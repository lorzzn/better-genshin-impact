using BetterGenshinImpact.Core.Config;
using Microsoft.ClearScript;
using Microsoft.ClearScript.V8;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BetterGenshinImpact.GameTask.Common;
using Microsoft.ClearScript.JavaScript;
using Microsoft.Extensions.Logging;
using BetterGenshinImpact.Core.Script.Utils;

namespace BetterGenshinImpact.Core.Script.Project;

public partial class ScriptProject
{
    public string ProjectPath { get; set; }
    public string ManifestFile { get; set; }

    public Manifest Manifest { get; set; }

    public string FolderName { get; set; }

    public ScriptProject(DirectoryInfo directory)
    {
        FolderName = directory.Name;
        ProjectPath = directory.FullName;
        if (!Directory.Exists(ProjectPath))
        {
            throw new DirectoryNotFoundException("脚本文件夹不存在:" + ProjectPath);
        }

        ManifestFile = ScriptUtils.NormalizePath(ProjectPath, "manifest.json");
        if (!File.Exists(ManifestFile))
        {
            throw new FileNotFoundException("manifest.json文件不存在，请确认此脚本是JS脚本类型。" + ManifestFile);
        }

        Manifest = Manifest.FromJson(File.ReadAllText(ManifestFile));
        Manifest.Validate(ProjectPath);
    }

    private IScriptEngine BuildScriptEngine(IScriptHost host)
    {
        V8ScriptEngine engine = new V8ScriptEngine(V8ScriptEngineFlags.UseCaseInsensitiveMemberBinding | V8ScriptEngineFlags.EnableTaskPromiseConversion);

        // packages 依赖和资源重载
        var loader = new PackageDocumentLoader(ProjectPath);
        engine.DocumentSettings.Loader = loader;

        // 添加 packages 到搜索路径
        var libraries = new HashSet<string>(Manifest.Library ?? Array.Empty<string>())
        {
            ".",
            "./packages"
        };

        var libraryList = libraries.ToList();

        try
        {
            host.Configure(engine, ProjectPath, libraryList.ToArray());
            return engine;
        }
        catch
        {
            engine.Dispose();
            throw;
        }
    }

    public async Task<string?> ExecuteWithHostAsync(IScriptHost host, object? context = null,
        CancellationToken cancellationToken = default, bool serializeResult = true)
    {
        ArgumentNullException.ThrowIfNull(host);
        cancellationToken.ThrowIfCancellationRequested();
        // 加载代码
        var code = await LoadCode();
        using var engine = BuildScriptEngine(host);
        using var cancellation = cancellationToken.Register(engine.Interrupt);

        // 使用自定义加载器解析脚本文件
        var loader = (PackageDocumentLoader)engine.DocumentSettings.Loader;

        if (context != null)
        {
            // 写入配置的内容
            engine.AddHostObject("settings", context);
        }

        try
        {
            bool useModule = Manifest.Library.Length != 0 ||
                             code.Contains("import ", StringComparison.Ordinal) ||
                             code.Contains("export ", StringComparison.Ordinal);

            object? evaluation;
            if (useModule)
            {
                // 清除Document缓存
                DocumentLoader.Default.DiscardCachedDocuments();

                string mainScriptPath = ScriptUtils.NormalizePath(ProjectPath, Manifest.Main);
                string runtimeCode = loader.RewriteScriptCode(code, mainScriptPath);
                
                var documentInfo = new DocumentInfo(new Uri(mainScriptPath)) { Category = ModuleCategory.Standard };
                evaluation = engine.Evaluate(documentInfo, runtimeCode);
            }
            else
            {
                evaluation = engine.Evaluate(code);
            }
            if (evaluation is Task task)
            {
                await task.WaitAsync(cancellationToken);
                evaluation = task is Task<object> result ? result.Result : null;
            }
            // The desktop caller does not consume script return values. Preserve
            // that behavior for values which cannot be represented as JSON.
            if (!serializeResult) return null;
            // Materialize the result while V8 is alive. Returning a ScriptObject
            // would leave the caller holding an object from a disposed engine.
            var json = engine.Script.JSON.stringify(evaluation);
            return json is string text ? text : null;
        }
        catch (Exception) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(cancellationToken);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            throw;
        }
        finally
        {
            // Close host callbacks while V8 is still alive. Window transports
            // must not outlive the script that owns their pending promises.
            try { (host as IScriptHostLifetime)?.OnScriptEnding(); }
            finally
            {
                // 终止代码执行
                try { engine.Interrupt(); }
                catch (Exception e) { TaskControl.Logger.LogError(e, "中断脚本执行异常：" + e.Message); }
            }

        }
    }

    public async Task<string> LoadCode()
    {
        var code = await File.ReadAllTextAsync(ScriptUtils.NormalizePath(ProjectPath, Manifest.Main));
        if (string.IsNullOrEmpty(code))
        {
            throw new FileNotFoundException("main js is empty.");
        }

        return code;
    }
}
