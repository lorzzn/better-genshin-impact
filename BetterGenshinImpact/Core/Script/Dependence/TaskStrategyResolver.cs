using System;
using System.IO;
using BetterGenshinImpact.Core.Config;
using BetterGenshinImpact.GameTask.AutoFight;

namespace BetterGenshinImpact.Core.Script.Dependence;

public static class TaskStrategyResolver
{
    public static bool GetTcgStrategy(string strategyName, out string content, Action<string> warning, Action<string> error)
    {
        content = string.Empty;
        if (string.IsNullOrEmpty(strategyName))
        {
            warning("请先选择策略");
            return true;
        }

        var path = Global.Absolute(@"User\AutoGeniusInvokation\" + strategyName + ".txt");

        if (!File.Exists(path))
        {
            error("策略文件不存在");
            return true;
        }

        content = File.ReadAllText(path);
        return false;
    }

    public static bool GetFightStrategy(string strategyName, out string path, Action<string> warning, Action<string> error)
    {
        if (string.IsNullOrEmpty(strategyName))
        {
            warning("请先在下拉列表配置中选择战斗策略！");
            path = string.Empty;
            return true;
        }

        if ("根据队伍自动选择".Equals(strategyName))
        {
            path = Global.Absolute(@"User\AutoFight\");
        }
        else if (AutoFightParam.ComboStrategyName.Equals(strategyName))
        {
            // 固定策略：不对应策略文件，跳过存在性检查，由 ComboCombatTaskFactory 路由
            path = strategyName;
            return false;
        }
        else
        {
            (path, _) = AutoFightParam.ResolveStrategyPath(strategyName);
        }

        if (!File.Exists(path) && !Directory.Exists(path))
        {
            error("当前选择的自动战斗策略文件不存在");
            return true;
        }

        return false;
    }

}
