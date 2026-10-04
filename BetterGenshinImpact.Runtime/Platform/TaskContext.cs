using BetterGenshinImpact.Runtime;

namespace BetterGenshinImpact.GameTask;

// Compatibility facade for upstream callers. Ownership and OS operations live
// in GameSession/IGameHost; no window handle or Windows application is created.
public sealed class TaskContext
{
    private static readonly TaskContext instance = new();
    public static TaskContext Instance() => instance;
    public GameSystemInfo SystemInfo => GameSession.Current.SystemInfo;
    public double DpiScale => 1;
}
