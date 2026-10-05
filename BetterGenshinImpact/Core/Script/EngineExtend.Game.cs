using System;
using System.Threading.Tasks;
using BetterGenshinImpact.Core.Script.Dependence;
using BetterGenshinImpact.Core.Script.Dependence.Model;
using BetterGenshinImpact.GameTask.AutoBoss;
using BetterGenshinImpact.GameTask.AutoDomain;
using BetterGenshinImpact.GameTask.AutoFight;
using BetterGenshinImpact.GameTask.AutoFight.Model;
using BetterGenshinImpact.GameTask.AutoLeyLineOutcrop;
using BetterGenshinImpact.GameTask.AutoSkip;
using BetterGenshinImpact.GameTask.AutoStygianOnslaught;
using BetterGenshinImpact.GameTask.CharacterDevelopment;
using BetterGenshinImpact.GameTask.Common.Job;
using BetterGenshinImpact.GameTask.Model.GameUI;
using Microsoft.ClearScript;
#if BETTERGI_PORTABLE
using Pen = BetterGenshinImpact.Runtime.OverlayPen;
#else
using System.Drawing;
#endif

namespace BetterGenshinImpact.Core.Script;

public partial class EngineExtend
{
    public static void InitGameHost(IScriptEngine engine, string workDir, Dispatcher dispatcher, object? config = null, IScriptFileSystem? fileSystem = null)
    {
        // 添加我的自定义实例化对象
        engine.AddHostObject("pathingScript", new AutoPathingScript(workDir, config, fileSystem));
        engine.AddHostObject("characterDevelopmentTask", new CharacterDevelopmentTask());
        engine.AddHostObject("notification", new Notification());

        // 任务调度器
        engine.AddHostObject("dispatcher", dispatcher);
        engine.AddHostType("RealtimeTimer", typeof(RealtimeTimer));
        engine.AddHostType("SoloTask", typeof(SoloTask));
        engine.AddHostType("AutoSkipConfig", typeof(AutoSkipConfig));

        // PostMessage 作为类型实例化
        engine.AddHostType("PostMessage", typeof(Dependence.Simulator.PostMessage));

        // 直接添加方法
        engine.AddHostObject("getAvatars", (Func<string[]>)GlobalMethod.GetAvatars);

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

        // 添加C#的类型
        engine.AddHostType(typeof(Task));

    }
}
