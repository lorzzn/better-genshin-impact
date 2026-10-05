using System.Dynamic;
using System.Text.Json;
using BetterGenshinImpact.Core.Script;
using BetterGenshinImpact.Core.Script.Project;
using BetterGenshinImpact.Core.Script.Utils;
using Microsoft.ClearScript;
using Xunit;

namespace BetterGenshinImpact.ScriptingTest;

public sealed class ScriptProjectTests
{
    [Theory]
    [InlineData("({answer:42})")]
    [InlineData("(async()=>{ await probe.pause(); return {answer:42}; })()")]
    public async Task ResultsAreMaterializedBeforeEngineDisposal(string code)
    {
        using var files = new ScriptFiles(code);
        var result = await files.Project().ExecuteWithHostAsync(new ProbeHost());
        using var json = JsonDocument.Parse(result!);
        Assert.Equal(42, json.RootElement.GetProperty("answer").GetInt32());
    }

    [Fact]
    public async Task OfficialEngineAwaitsPlainAsyncScriptAndUsesCaseInsensitiveHostBinding()
    {
        using var files = new ScriptFiles("(async () => { await probe.pause(); probe.RECORD(settings.answer); })()");
        dynamic settings = new ExpandoObject();
        settings.answer = "finished";
        var host = new ProbeHost();
        await files.Project().ExecuteWithHostAsync(host, (object)settings);
        Assert.Equal("finished", Assert.Single(host.Records));
        Assert.True(host.Paused);
    }

    [Fact]
    public async Task OfficialLoaderExecutesModulesAndResourceImports()
    {
        using var files = new ScriptFiles("import { answer } from './module.js'; await probe.pause(); probe.record(answer);");
        files.Write("module.js", "import data from './data.json'; export const answer = JSON.parse(data).value;");
        files.Write("data.json", "{\"value\":\"module value\"}");
        var host = new ProbeHost();
        await files.Project().ExecuteWithHostAsync(host);
        Assert.Equal("module value", Assert.Single(host.Records));
    }

    [Fact]
    public async Task ModuleWithAsyncEntryWaitsForCommissionStyleCompletion()
    {
        using var files = new ScriptFiles("import { answer } from './module.js'; (async () => { await probe.pause(); probe.record(answer); })();");
        files.Write("module.js", "export const answer = 'commission-style entry completed';");
        var host = new ProbeHost();
        await files.Project().ExecuteWithHostAsync(host);
        Assert.Equal("commission-style entry completed", Assert.Single(host.Records));
    }

    [Fact]
    public async Task AsyncScriptFailurePropagates()
    {
        using var files = new ScriptFiles("(async () => { await probe.pause(); throw new Error('task failed'); })()");
        var error = await Assert.ThrowsAnyAsync<Exception>(() => files.Project().ExecuteWithHostAsync(new ProbeHost()));
        Assert.Contains("task failed", error.Message);
    }

    [Theory]
    [InlineData("while (true) {}")]
    [InlineData("new Promise(() => {})")]
    public async Task CancellationStopsCpuLoopAndUnresolvedPromise(string code)
    {
        using var files = new ScriptFiles(code);
        using var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var run = Task.Run(() => files.Project().ExecuteWithHostAsync(new ProbeHost(), cancellationToken: cancel.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task ModuleCannotReadOutsideItsPackage()
    {
        using var files = new ScriptFiles("import { value } from '../outside.js'; probe.record(value);");
        File.WriteAllText(Path.Combine(files.Root, "outside.js"), "export const value = 'outside';");
        var host = new ProbeHost();
        await Assert.ThrowsAnyAsync<Exception>(() => files.Project().ExecuteWithHostAsync(host));
        Assert.Empty(host.Records);
    }

    [Fact]
    public void MainCannotReadOutsideItsPackage()
    {
        using var files = new ScriptFiles("probe.record('unused');");
        File.WriteAllText(Path.Combine(files.Root, "outside.js"), "probe.record('outside');");
        files.WriteManifest("../outside.js");
        Assert.ThrowsAny<Exception>(() => files.Project());
    }

    [Fact]
    public void CommonPathPrefixIsNotAChildDirectory()
    {
        var root = Path.Combine(Path.GetTempPath(), "script");
        Assert.Throws<ArgumentException>(() => ScriptUtils.NormalizePath(root, "../script-private/secret.js"));
    }

    public sealed class ProbeHost : IScriptHost
    {
        public List<string> Records { get; } = [];
        public bool Paused { get; private set; }
        public void Record(string text) => Records.Add(text);
        public async Task Pause() { await Task.Delay(30); Paused = true; }
        public void Configure(IScriptEngine engine, string projectPath, string[] searchPaths)
        {
            engine.AddHostObject("probe", this);
            engine.AddHostObject("file", new TestFiles(projectPath));
            engine.DocumentSettings.AccessFlags = DocumentAccessFlags.AllowCategoryMismatch;
            engine.DocumentSettings.SearchPath = string.Join(';', searchPaths.Select(p => ScriptUtils.NormalizePath(projectPath, p)));
        }
    }

    public sealed class TestFiles(string root)
    {
        public string ReadTextSync(string path) => File.ReadAllText(ScriptUtils.NormalizePath(root, path));
    }

    private sealed class ScriptFiles : IDisposable
    {
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "bgi-script-test-" + Guid.NewGuid().ToString("N"));
        private string Package => Path.Combine(Root, "package");
        public ScriptFiles(string code)
        {
            Directory.CreateDirectory(Package);
            Write("main.js", code);
            WriteManifest("main.js");
        }
        public void Write(string path, string content) => File.WriteAllText(Path.Combine(Package, path), content);
        public void WriteManifest(string main) => Write("manifest.json", JsonSerializer.Serialize(new { name = "test", version = "1.0.0", main }));
        public ScriptProject Project() => new(new DirectoryInfo(Package));
        public void Dispose() => Directory.Delete(Root, recursive: true);
    }
}
