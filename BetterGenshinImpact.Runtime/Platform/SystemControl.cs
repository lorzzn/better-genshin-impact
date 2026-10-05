using BetterGenshinImpact.Runtime;

namespace BetterGenshinImpact.GameTask;

/// <summary>Focus operations on the bound Target, never the embedding machine.</summary>
public static class SystemControl
{
    public static bool IsGenshinImpactActive() => GameSession.Current.Host.IsForeground;
    public static bool IsGenshinImpactActiveByProcess() => IsGenshinImpactActive();
    public static string GetActiveByProcess() => GameSession.Current.Host.ActiveSurfaceName;
    public static void ActivateWindow() => GameSession.Current.Host.Activate();
}
