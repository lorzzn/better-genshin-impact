using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using BetterGenshinImpact.Core.BgiVision;
using BetterGenshinImpact.Core.Recognition;
using BetterGenshinImpact.Core.Script.Dependence;
using BetterGenshinImpact.Core.Script.Utils;
using BetterGenshinImpact.GameTask.Model.Area;
using Microsoft.ClearScript;
using OpenCvSharp;
using Region = BetterGenshinImpact.GameTask.Model.Area.Region;

namespace BetterGenshinImpact.Core.Script;

public partial class EngineExtend
{
    public static void InitCoreHost(IScriptEngine engine, string workDir, string[]? searchPaths = null, IScriptFileSystem? fileSystem = null)
    {
        GlobalMethod.SetGameMetrics(1920, 1080);
        engine.AddHostObject("keyMouseScript", new KeyMouseScript(workDir, fileSystem));
        engine.AddHostObject("genshin", new Dependence.Genshin());
        engine.AddHostObject("log", new Log());
        engine.AddHostObject("file", new LimitedFile(workDir, fileSystem)); // 限制文件访问
        engine.AddHostType("CancellationTokenSource", typeof(CancellationTokenSource));
        engine.AddHostType("CancellationToken", typeof(CancellationToken));
        engine.AddHostType("Mat", typeof(Mat));
        engine.AddHostType("Point2f", typeof(Point2f)); // 添加Point2f类型暴露
        engine.AddHostType("RecognitionObject", typeof(RecognitionObject));
        engine.AddHostType("DesktopRegion", typeof(DesktopRegion));
        engine.AddHostType("GameCaptureRegion", typeof(GameCaptureRegion));
        engine.AddHostType("ImageRegion", typeof(ImageRegion));
        engine.AddHostType("Region", typeof(Region));
        engine.AddHostType("Color", typeof(Color));
        engine.AddHostObject("OpenCvSharp", new HostTypeCollection("OpenCvSharp"));
        engine.AddHostType("ServerTime", typeof(ServerTime));
        engine.AddHostType("BvPage", typeof(BvPage));
        engine.AddHostType("BvLocator", typeof(BvLocator));
        engine.AddHostType("BvImage", typeof(BvImage));
        engine.AddHostObject("host", new CustomHostFunctions());
        AddCoreGlobalMethods(engine);
        ConfigureModuleSearch(engine, workDir, searchPaths);
    }

    // Configuration hosts need the same module rules without game/input APIs.
    public static void ConfigureModuleSearch(IScriptEngine engine, string workDir, string[]? searchPaths)
    {
        // 导入 JavaScript 模块
        // https://microsoft.github.io/ClearScript/2023/01/24/module-interop.html
        // https://github.com/microsoft/ClearScript/blob/master/ClearScriptTest/V8ModuleTest.cs
        engine.DocumentSettings.AccessFlags = DocumentAccessFlags.AllowCategoryMismatch;
        if (searchPaths != null)
        {
            var normalizedPaths = new List<string>();
            foreach (var path in searchPaths)
            {
                try
                {
                    var normalizedPath = ScriptUtils.NormalizePath(workDir, path);
                    normalizedPaths.Add(normalizedPath);
                }
                catch (Exception ex)
                {
                    throw new Exception($"从 library 字段读取路径 '{path}' 失败: {ex.Message}", ex);
                }
            }

            if (normalizedPaths.Count > 0)
            {
                engine.DocumentSettings.SearchPath = string.Join(';', normalizedPaths);
            }
        }
    }

    public static void AddCoreGlobalMethods(IScriptEngine engine)
    {
#pragma warning disable CS8974 // Converting method group to non-delegate type
        engine.AddHostObject("sleep", GlobalMethod.Sleep);
        engine.AddHostObject("getVersion", GlobalMethod.GetVersion);
        engine.AddHostObject("keyDown", GlobalMethod.KeyDown);
        engine.AddHostObject("keyUp", GlobalMethod.KeyUp);
        engine.AddHostObject("keyPress", GlobalMethod.KeyPress);
        engine.AddHostObject("setGameMetrics", GlobalMethod.SetGameMetrics);
        engine.AddHostObject("getGameMetrics", GlobalMethod.GetGameMetrics);
        engine.AddHostObject("moveMouseBy", GlobalMethod.MoveMouseBy);
        engine.AddHostObject("moveMouseTo", GlobalMethod.MoveMouseTo);
        engine.AddHostObject("click", GlobalMethod.Click);
        engine.AddHostObject("leftButtonClick", GlobalMethod.LeftButtonClick);
        engine.AddHostObject("leftButtonDown", GlobalMethod.LeftButtonDown);
        engine.AddHostObject("leftButtonUp", GlobalMethod.LeftButtonUp);
        engine.AddHostObject("rightButtonClick", GlobalMethod.RightButtonClick);
        engine.AddHostObject("rightButtonDown", GlobalMethod.RightButtonDown);
        engine.AddHostObject("rightButtonUp", GlobalMethod.RightButtonUp);
        engine.AddHostObject("middleButtonClick", GlobalMethod.MiddleButtonClick);
        engine.AddHostObject("middleButtonDown", GlobalMethod.MiddleButtonDown);
        engine.AddHostObject("middleButtonUp", GlobalMethod.MiddleButtonUp);
        engine.AddHostObject("verticalScroll", GlobalMethod.VerticalScroll);
        engine.AddHostObject("captureGameRegion", GlobalMethod.CaptureGameRegion);
        engine.AddHostObject("inputText", GlobalMethod.InputText);
#pragma warning restore CS8974
    }
}
