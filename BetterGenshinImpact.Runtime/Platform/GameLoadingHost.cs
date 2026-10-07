using BetterGenshinImpact.Runtime;

namespace BetterGenshinImpact.GameTask.GameLoading;

public partial class GameLoadingTrigger
{
    private void InitializePlatform()
    {
        // An embedded game session must never inspect the server's registry or
        // start a desktop launcher. This setting belongs to the Windows host.
        if (_config.RecordGameTimeEnabled)
            throw new NotSupportedException("嵌入运行库不提供 Starward 游戏时长记录");
    }

    private void DetectChannel()
    {
        IsBili = GameSession.Current.Host.GameChannel switch
        {
            "official" => false,
            "bilibili" => true,
            _ => throw new NotSupportedException("Target 未提供可用的游戏登录渠道")
        };
    }

    private void ContinueChannelLogin()
    {
        var session = GameSession.Current;
        biliLoginClicked = session.Host.ContinueChannelLogin(session.CancellationToken);
    }
}
