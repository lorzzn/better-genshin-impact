namespace BetterGenshinImpact.Core.Script.Dependence;

/// <summary>Task preferences previously supplied only by the desktop settings page.</summary>
public interface IScriptTaskDefaults
{
    int AutoWoodRoundNum { get; }
    int AutoWoodDailyMaxCount { get; }
    // Matches the original desktop helpers: true means missing/invalid configuration.
    bool GetTcgStrategy(out string content);
    bool GetFightStrategy(out string path);
    bool GetFightStrategy(string name, out string path);
}
