using BetterGenshinImpact.Core.Config;
using BetterGenshinImpact.GameTask.AutoTrackPath;
using BetterGenshinImpact.GameTask.QuickTeleport;

namespace BetterGenshinImpact.Runtime;

/// <summary>Task configuration supplied by the embedding host; no UI or scheduler.</summary>
public sealed class GameConfiguration
{
    public TpConfig TpConfig { get; set; } = new();
    public QuickTeleportConfig QuickTeleportConfig { get; set; } = new();
    public KeyBindingsConfig KeyBindingsConfig { get; set; } = new();
    public RuntimePathingConfiguration PathingConditionConfig { get; set; } = new();
    public RuntimeGeneralConfiguration OtherConfig { get; set; } = new();
    public bool IsHdrCapture { get; set; }
}

public sealed class RuntimePathingConfiguration
{
    public string MapMatchingMethod { get; set; } = "SIFT";
}

public sealed class RuntimeGeneralConfiguration
{
    public string GameCultureInfoName { get; set; } = "zh-Hans";
    public TimeSpan ServerTimeZoneOffset { get; set; } = TimeSpan.FromHours(8);
}
