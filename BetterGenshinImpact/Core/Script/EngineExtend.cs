using System;
using System.Collections.Generic;
using System.Drawing;
using BetterGenshinImpact.Core.Script.Dependence;
using BetterGenshinImpact.Core.Script.Dependence.Model;
using Microsoft.ClearScript;
using System.Threading;
using System.Threading.Tasks;
using BetterGenshinImpact.Core.BgiVision;
using OpenCvSharp;
using BetterGenshinImpact.Core.Recognition;
using BetterGenshinImpact.GameTask.Model.Area;
using BetterGenshinImpact.Core.Script.Utils;
using BetterGenshinImpact.GameTask.Common.Job;
using BetterGenshinImpact.GameTask.AutoDomain;
using BetterGenshinImpact.GameTask.AutoBoss;
using BetterGenshinImpact.GameTask.AutoFight;
using BetterGenshinImpact.GameTask.AutoFight.Model;
using BetterGenshinImpact.GameTask.AutoLeyLineOutcrop;
using BetterGenshinImpact.GameTask.AutoSkip;
using BetterGenshinImpact.GameTask.AutoStygianOnslaught;
using BetterGenshinImpact.GameTask.CharacterDevelopment;
using BetterGenshinImpact.GameTask.Model.GameUI;
using Region = BetterGenshinImpact.GameTask.Model.Area.Region;

namespace BetterGenshinImpact.Core.Script;

public partial class EngineExtend
{
    /// <summary>
    /// ！！！ 注意：这个方法会添加一些全局方法和对象，不要随便添加，以免安全风险！！！
    /// </summary>
    /// <param name="engine"></param>
    /// <param name="workDir"></param>
    /// <param name="searchPaths"></param>
    public static void InitHost(IScriptEngine engine, string workDir, string[]? searchPaths = null, object? config = null)
    {
        InitCoreHost(engine, workDir, searchPaths);

        // engine.AddHostObject("xHost", new ExtendedHostFunctions());  // 有越权的安全风险

        InitGameHost(engine, workDir, new Dispatcher(config), config);
        engine.AddHostObject("http", new Http());
        engine.AddHostType("KeyMouseHook", typeof(KeyMouseHook));

        // HTML 遮罩
        engine.AddHostObject("htmlMask", new HtmlMask(workDir));


    }

    public static void AddAllGlobalMethod(IScriptEngine engine)
    {
        // // 获取GlobalMethod类的所有静态方法
        // var methods = typeof(GlobalMethod).GetMethods(BindingFlags.Static | BindingFlags.Public);
        //
        // foreach (var method in methods)
        // {
        //     // 使用方法名首字母小写作为HostObject的名称
        //     var methodName = char.ToLowerInvariant(method.Name[0]) + method.Name[1..];
        //     engine.AddHostObject(methodName, method);
        // }

        AddCoreGlobalMethods(engine);
        engine.AddHostObject("getAvatars", (Func<string[]>)GlobalMethod.GetAvatars);
    }
}
