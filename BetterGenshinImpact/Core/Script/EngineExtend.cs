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

        // 添加我的自定义实例化对象
        engine.AddHostObject("pathingScript", new AutoPathingScript(workDir, config));
        engine.AddHostObject("characterDevelopmentTask", new CharacterDevelopmentTask());
        engine.AddHostObject("http", new Http()); // 限制文件访问
        engine.AddHostObject("notification", new Notification());
        
        // 任务调度器
        engine.AddHostObject("dispatcher", new Dispatcher(config));
        engine.AddHostType("RealtimeTimer", typeof(RealtimeTimer));
        engine.AddHostType("SoloTask", typeof(SoloTask));
        engine.AddHostType("AutoSkipConfig", typeof(AutoSkipConfig));
        
        // 添加取消令牌相关类型

        // PostMessage 作为类型实例化
        engine.AddHostType("PostMessage", typeof(Dependence.Simulator.PostMessage));

        // 直接添加方法
        engine.AddHostObject("getAvatars", GlobalMethod.GetAvatars);

        // 识图模块相关
        
        engine.AddHostType("Pen", typeof(Pen));
        
        engine.AddHostType("CombatScenes", typeof(CombatScenes));
        engine.AddHostType("Avatar", typeof(Avatar));
        
        

        
        engine.AddHostType("AutoDomainParam", typeof(AutoDomainParam));  
        engine.AddHostType("AutoBossParam", typeof(AutoBossParam));
        engine.AddHostType("CountInventoryItemParam", typeof(CountInventoryItemParam));
        engine.AddHostType("GridScreenName", typeof(GridScreenName));
        engine.AddHostType("ItemIconRecognitionMode", typeof(ItemIconRecognitionMode));
        engine.AddHostType("AutoFightParam", typeof(AutoFightParam)); 
        engine.AddHostType("AutoLeyLineOutcropParam", typeof(AutoLeyLineOutcropParam));
        engine.AddHostType("AutoStygianOnslaughtParam", typeof(AutoStygianOnslaughtParam));
        engine.AddHostObject("strategyFile", new StrategyFile());
        //鼠标回调
        engine.AddHostType("KeyMouseHook", typeof(KeyMouseHook)); 
        // 添加C#的类型
        engine.AddHostType(typeof(Task));
        
        // 新的BvPage类


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
        engine.AddHostObject("getAvatars", GlobalMethod.GetAvatars); // Converting method group to non-delegate type
    }
}
