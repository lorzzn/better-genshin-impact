using BetterGenshinImpact.GameTask.Common;
using Microsoft.Extensions.Logging;

namespace BetterGenshinImpact.GameTask.LogParse;

public partial class TravelsDiaryDetailManager
{
    private static partial void ShowInformation(string message) => TaskControl.Logger.LogInformation(message);

    private static partial void ShowWarning(string message) => TaskControl.Logger.LogWarning(message);
}
