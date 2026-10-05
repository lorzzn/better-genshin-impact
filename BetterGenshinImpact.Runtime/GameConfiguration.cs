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
    public HotKeyConfig HotKeyConfig { get; set; } = new();
    public HardwareAccelerationConfig HardwareAccelerationConfig { get; set; } = new();
    public PathingConditionConfig PathingConditionConfig { get; set; } = PathingConditionConfig.Default;
    public OtherConfig OtherConfig { get; set; } = new();
    public GameTask.AutoFight.AutoFightConfig AutoFightConfig { get; set; } = new();
    public GameTask.AutoSkip.AutoSkipConfig AutoSkipConfig { get; set; } = new();
    public GameTask.AutoPick.AutoPickConfig AutoPickConfig { get; set; } = new();
    public GameTask.AutoEat.AutoEatConfig AutoEatConfig { get; set; } = new();
    public GameTask.AutoFishing.AutoFishingConfig AutoFishingConfig { get; set; } = new();
    public GameTask.AutoWood.AutoWoodConfig AutoWoodConfig { get; set; } = new();
    public GameTask.AutoArtifactSalvage.AutoArtifactSalvageConfig AutoArtifactSalvageConfig { get; set; } = new();
    public GameTask.GetGridIcons.GetGridIconsConfig GetGridIconsConfig { get; set; } = new();
    public MacroConfig MacroConfig { get; set; } = new();
    public OneDragonFlowConfig OneDragonFlowConfig { get; set; } = new();
    public Service.Notification.NotificationConfig NotificationConfig { get; set; } = new();
    public ScreenshotConfig CommonConfig { get; set; } = new();
    public string SelectedOneDragonFlowConfigName { get; set; } = "默认配置";
    public GameTask.SkillCd.SkillCdConfig SkillCdConfig { get; set; } = new();
    public bool SaveScreenshotOnKeyTick { get; set; }
    public GameTask.AutoBoss.AutoBossConfig AutoBossConfig { get; set; } = new();
    public GameTask.AutoDomain.AutoDomainConfig AutoDomainConfig { get; set; } = new();
    public GameTask.AutoLeyLineOutcrop.AutoLeyLineOutcropConfig AutoLeyLineOutcropConfig { get; set; } = new();
    public GameTask.AutoStygianOnslaught.AutoStygianOnslaughtConfig AutoStygianOnslaughtConfig { get; set; } = new();
    public GameTask.AutoGeniusInvokation.AutoGeniusInvokationConfig AutoGeniusInvokationConfig { get; set; } = new();
    public int TriggerInterval { get; set; } = 50;
    public PathingPartyConfig? PathingPartyConfig { get; set; }
    public int AutoWoodRoundNum { get; set; }
    public int AutoWoodDailyMaxCount { get; set; } = 2000;
    public bool DetailedErrorLogs { get; set; }
    public GameTask.AutoCook.AutoCookConfig AutoCookConfig { get; set; } = new();
    public GameTask.AutoCombo.ComboBuild.AutoComboBuildConfig AutoComboBuildConfig { get; set; } = new();
    public bool IsHdrCapture { get; set; }
    public GameTask.AutoMusicGame.AutoMusicGameConfig AutoMusicGameConfig { get; set; } = new();
}
