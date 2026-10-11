namespace BetterGenshinImpact.GameTask;

/// <summary>
/// 任务过程中的状态（上次传送的地图、战斗计数、导航的上一位置等）。
/// 嵌入式运行库中一个进程可同时运行多个游戏的任务（平台节点的会话位），
/// 状态按 GameSession 分开；桌面版一个进程只有一个游戏，进程内共用一份。
/// </summary>
public static class TaskLocal
{
#if BETTERGI_PORTABLE
    public static T Get<T>() where T : class, new() =>
        Runtime.GameSession.IsBound ? Runtime.GameSession.Current.Local<T>() : Shared<T>.Value;
#else
    public static T Get<T>() where T : class, new() => Shared<T>.Value;
#endif

    private static class Shared<T> where T : class, new()
    {
        internal static readonly T Value = new();
    }
}
