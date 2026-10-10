using BetterGenshinImpact.Core.Script.Dependence;
using BetterGenshinImpact.Runtime;

namespace BetterGenshinImpact.Model;

public partial class OneDragonTaskItem
{
    private static partial bool GetFightStrategy(string strategyName, out string path) =>
        TaskStrategyResolver.GetFightStrategy(strategyName, out path, RuntimeUi.Warning, message => RuntimeUi.Error(message));
}
