using BetterGenshinImpact.GameTask.Model.Area;
using BetterGenshinImpact.Runtime;
using Microsoft.Extensions.Logging;

namespace BetterGenshinImpact.GameTask.Common;

public static class TaskControl
{
    public static ILogger Logger => RuntimeEnvironment.Logger;
    public static ImageRegion CaptureToRectArea() => GameSession.Current.Capture();
    public static Task Delay(int milliseconds, CancellationToken token = default) => Task.Delay(milliseconds, token);
    public static void Sleep(int milliseconds) => Task.Delay(milliseconds, GameSession.Current.CancellationToken).GetAwaiter().GetResult();
}
