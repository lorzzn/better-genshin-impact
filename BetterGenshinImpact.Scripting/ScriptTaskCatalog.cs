namespace BetterGenshinImpact.Scripting;

// Metadata for embedding clients. Execution still uses Dispatcher.RunTask.
public sealed record ScriptTaskDescriptor(string Name, string Label, object Parameters);

public static class ScriptTaskCatalog
{
    public static IReadOnlyList<ScriptTaskDescriptor> Tasks { get; } =
    [
        new("AutoGeniusInvokation", "自动七圣召唤", new { strategy = "" }),
        new("AutoWood", "自动伐木", new { }),
        new("AutoFight", "自动战斗", new { }),
        new("OneKeyFight", "角色战斗宏", new { }),
        new("AutoArtifactSalvage", "自动分解圣遗物", new { }),
        new("AutoDomain", "自动秘境", new { }),
        new("AutoBoss", "自动首领讨伐", new { }),
        new("AutoLeyLineOutcrop", "自动地脉花", new { }),
        new("AutoStygianOnslaught", "自动幽境危战", new { }),
        new("AutoFishing", "自动钓鱼", new { }),
        new("AutoCook", "自动料理", new { }),
        new("AutoEat", "自动吃料理", new { }),
        new("CountInventoryItem", "背包材料计数", new { gridScreenName = "CharacterDevelopmentItems", stopByItemSort = false, itemNames = Array.Empty<string>() }),
        new("AutoMusicGame", "自动音游", new { }),
        new("AutoAlbum", "自动音游专辑", new { }),
        new("PlayMusic", "乐谱自动演奏", new { files = Array.Empty<string>(), speed = 1.0, customBpm = (double?)null, startSeconds = 0, playbackMode = "Sequential", autoSwitchInstrument = false, outputProfileName = "", transpose = 0, disabledTrackIndexes = Array.Empty<int>() }),
        new("AutoOpenChest", "识别并开启宝箱", new { }),
        new("QuickBuy", "快速购买", new { }),
        new("OneKeyClaimReward", "快捷领取当前页面奖励", new { }),
        new("QuickEnhanceArtifact", "快捷强化圣遗物", new { }),
        new("QuickSereniteaPot", "快速进出尘歌壶", new { }),
        new("UseRedemptionCode", "使用兑换码", new { codes = Array.Empty<string>() }),
        new("ReturnMainUi", "返回游戏主界面", new { }),
        new("ClaimBattlePassRewards", "领取纪行奖励", new { }),
        new("ClaimMailRewards", "领取邮件奖励", new { }),
        new("ClaimEncounterPointsRewards", "领取历练点奖励", new { }),
        new("BlessingOfTheWelkinMoon", "领取空月祝福", new { }),
        new("GoToAdventurersGuild", "前往冒险家协会", new { country = "蒙德" }),
        new("GoCraftResin", "合成浓缩树脂", new { country = "蒙德" }),
        new("GoToCraftingBench", "前往合成台", new { country = "蒙德" }),
        new("SwitchParty", "切换队伍", new { partyName = "" }),
        new("SetTime", "调整游戏时间", new { hour = 12, minute = 0, skip = false }),
    ];

    public static void Require(string name)
    {
        if (!Tasks.Any(task => task.Name == name)) throw new ArgumentException($"未知官方任务：{name}");
    }
}
