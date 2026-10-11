using BetterGenshinImpact.GameTask.Model.Area;
using BetterGenshinImpact.Runtime;
using Microsoft.Extensions.Logging;

namespace BetterGenshinImpact.GameTask.Common;

public partial class TaskControl
{
    private static readonly SemaphoreSlim unbound = new(1, 1);
    // Tasks of one game run one at a time; other games' tasks are independent.
    public static SemaphoreSlim TaskSemaphore => GameSession.IsBound ? GameSession.Current.TaskSemaphore : unbound;
    public static ILogger Logger => RuntimeEnvironment.Logger;
    public static ImageRegion CaptureToRectArea(bool forceNew = false) => GameSession.Current.Capture();
}
