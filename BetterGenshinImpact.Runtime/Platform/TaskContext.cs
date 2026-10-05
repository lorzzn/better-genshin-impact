using BetterGenshinImpact.Runtime;

namespace BetterGenshinImpact.GameTask;

// Compatibility facade for upstream callers. Ownership and OS operations live
// in GameSession/IGameHost; no window handle or Windows application is created.
public sealed class TaskContext
{
    public GameConfiguration Config => GameSession.Current.Config;
    private static readonly TaskContext instance = new();
    public static TaskContext Instance() => instance;
    public GameSystemInfo SystemInfo => GameSession.Current.SystemInfo;
    public bool IsInitialized => GameSession.IsBound;
    public float DpiScale => 1;
    public Core.Simulator.PostMessageSimulator PostMessageSimulator { get; } = new();
}
