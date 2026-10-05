using BetterGenshinImpact.GameTask.Model.Area;
using BetterGenshinImpact.Runtime;
using Microsoft.Extensions.Logging;

namespace BetterGenshinImpact.GameTask.Common;

public partial class TaskControl
{
    public static readonly SemaphoreSlim TaskSemaphore = new(1, 1);
    public static ILogger Logger => RuntimeEnvironment.Logger;
    public static ImageRegion CaptureToRectArea(bool forceNew = false) => GameSession.Current.Capture();
}
