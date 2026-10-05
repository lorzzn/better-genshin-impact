using BetterGenshinImpact.GameTask;
using BetterGenshinImpact.GameTask.AutoTrackPath;
using System.Threading.Tasks;
using BetterGenshinImpact.GameTask.Common.Job;
using Vanara.PInvoke;
using BetterGenshinImpact.GameTask.AutoFishing;
using BetterGenshinImpact.ViewModel.Pages;
using System;
using BetterGenshinImpact.GameTask.AutoPathing;
using BetterGenshinImpact.GameTask.Common.BgiVision;
using OpenCvSharp;
using static BetterGenshinImpact.GameTask.Common.TaskControl;
using BetterGenshinImpact.GameTask.Common.Map;
using BetterGenshinImpact.GameTask.Common.Map.Maps.Base;

using BetterGenshinImpact.GameTask.Common.Exceptions;
using BetterGenshinImpact.GameTask.Common.Map.Maps;
using BetterGenshinImpact.Helpers.Extensions;
using Microsoft.Extensions.Logging;

namespace BetterGenshinImpact.Core.Script.Dependence;

public partial class Genshin
{
    private readonly ILogger<Genshin> _logger = App.GetLogger<Genshin>();



    /// <summary>
    /// 切换队伍
    /// </summary>
    /// <param name="partyName">队伍界面自定义的队伍名称</param>
    /// <returns></returns>
    public async Task<bool> SwitchParty(string partyName)
    {
        try
        {
            return await new SwitchPartyTask().Start(partyName, CancellationContext.Instance.Cts.Token);
        }
        catch (PartySetupFailedException)
        {
            return false;//释放失败状态给调用方，否则失败后会退出任务。
        }
    }

    /// <summary>
    /// 按槽位重组当前队伍角色。
    /// </summary>
    /// <param name="slot1">1 号位角色名。</param>
    /// <param name="slot2">2 号位角色名。</param>
    /// <param name="slot3">3 号位角色名。</param>
    /// <param name="slot4">4 号位角色名。</param>
    /// <param name="usePhysicalSlots">是否将 slot1-slot4 解释为队伍物理槽位；false 时按当前玩家可控角色顺序解释。</param>
    /// <returns>完成保存并返回主界面返回 true；参数无效、目标角色未找到或流程失败返回 false。</returns>
    /// <remarks>
    /// 未传入的槽位默认跳过；空字符串表示跳过对应槽位。
    /// 物理槽位调用示例：<c>await genshin.SwitchCharacter("胡桃", "夜兰", "", "钟离");</c>
    /// 可控顺序调用示例：<c>await genshin.SwitchCharacter("胡桃", "夜兰", "", "", false);</c>
    /// 该方法表示重组队伍槽位角色，不是按数字键切换当前出战角色。
    /// </remarks>
    public async Task<bool> SwitchCharacter(
        string slot1 = "",
        string slot2 = "",
        string slot3 = "",
        string slot4 = "",
        bool usePhysicalSlots = true)
    {
        try
        {
            return await new SwitchCharacterStateMachineTask().Start(
                slot1,
                slot2,
                slot3,
                slot4,
                usePhysicalSlots,
                CancellationContext.Instance.Cts.Token);
        }
        catch (PartySetupFailedException ex)
        {
            _logger.LogError(ex, "切换角色失败：{Message}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 清除当前调度器的队伍缓存
    /// </summary>
    public void ClearPartyCache()
    {
        RunnerContext.Instance.ClearCombatScenes();
    }


    /// <summary>
    /// 自动点击空月祝福
    /// </summary>
    /// <returns></returns>
    public async Task BlessingOfTheWelkinMoon()
    {
        await new BlessingOfTheWelkinMoonTask().Start(CancellationContext.Instance.Cts.Token);
    }

    /// <summary>
    /// 持续对话并选择目标选项
    /// </summary>
    /// <param name="option">选项文本</param>
    /// <param name="skipTimes">跳过次数</param>
    /// <param name="isOrange">是否为橙色选项</param>
    /// <returns></returns>
    public async Task ChooseTalkOption(string option, int skipTimes = 10, bool isOrange = false)
    {
        await new ChooseTalkOptionTask().SingleSelectText(option, CancellationContext.Instance.Cts.Token, skipTimes, isOrange);
    }

    /// <summary>
    /// 一键领取纪行奖励
    /// </summary>
    /// <returns></returns>
    public async Task ClaimBattlePassRewards()
    {
        await new ClaimBattlePassRewardsTask().Start(CancellationContext.Instance.Cts.Token);
    }

    /// <summary>
    /// 领取长效历练点奖励
    /// </summary>
    /// <returns></returns>
    public async Task ClaimEncounterPointsRewards()
    {
        await new ClaimEncounterPointsRewardsTask().Start(CancellationContext.Instance.Cts.Token);
    }

    /// <summary>
    /// 前往冒险家协会领取奖励
    /// </summary>
    /// <param name="country">国家名称</param>
    /// <returns></returns>
    public async Task GoToAdventurersGuild(string country)
    {
        await new GoToAdventurersGuildTask().Start(country, CancellationContext.Instance.Cts.Token);
    }

    /// <summary>
    /// 前往合成台
    /// </summary>
    /// <param name="country">国家名称</param>
    /// <returns></returns>
    public async Task GoToCraftingBench(string country)
    {
        await new GoToCraftingBenchTask().GoToCraftingBench(country, CancellationContext.Instance.Cts.Token);
    }

    /// <summary>
    /// 前往合成台合成浓缩树脂
    /// </summary>
    /// <param name="country">国家名称</param>
    /// <returns></returns>
    public async Task GoCraftResin(string country)
    {
        await new GoToCraftingBenchTask().GoCraftResin(country, CancellationContext.Instance.Cts.Token);
    }

    /// <summary>
    /// 在当前已打开的合成界面中合成指定材料。
    /// </summary>
    /// <param name="materialName">目标成品材料名。</param>
    /// <param name="quantity">目标合成个数，必须大于 0。</param>
    /// <param name="materialType">材料筛选类型；为空时从物品模型 CSV 中读取。</param>
    /// <returns>合成执行结果。</returns>
    public async Task<CraftMaterialResult> CraftMaterial(string materialName, int quantity, string? materialType = null)
    {
        return await new CraftMaterialTask(materialName, quantity, materialType).Start(CancellationContext.Instance.Cts.Token);
    }



    /// <summary>
    /// 钓鱼
    /// </summary>
    /// <returns></returns>
    public async Task AutoFishing(int fishingTimePolicy = 0)
    {
        var taskSettingsPageViewModel = App.GetService<TaskSettingsPageViewModel>();
        if (taskSettingsPageViewModel == null)
        {
            throw new ArgumentNullException(nameof(taskSettingsPageViewModel), "内部视图模型对象为空");
        }

        var param = AutoFishingTaskParam.BuildFromConfig(TaskContext.Instance().Config.AutoFishingConfig, taskSettingsPageViewModel.SaveScreenshotOnKeyTick);
        param.FishingTimePolicy = (FishingTimePolicy)fishingTimePolicy;
        await new AutoFishingTask(param).Start(CancellationContext.Instance.Cts.Token);
    }

    /// <summary>
    /// 重新登录原神
    /// </summary>
    /// <returns></returns>
    public async Task Relogin()
    {
        await new ExitAndReloginJob().Start(CancellationContext.Instance.Cts.Token);
    }
    
    /// <summary>
    /// 进出千星奇域
    /// </summary>
    /// <returns></returns>
    public async Task WonderlandCycle()
    {
        await new EnterAndExitWonderlandJob().Start(CancellationContext.Instance.Cts.Token);
    }

    /// <summary>
    /// 调整时间
    /// </summary>
    /// <param name="hour">目标小时(0-24)</param>
    /// <param name="minute">目标分钟(0-59)</param>
    /// <param name="skip">是否跳过动画（默认为否）</param>
    /// <returns></returns>
    public async Task SetTime(int hour, int minute, bool skip = false)
    {
        if ( hour < 0 || hour > 24)
            throw new ArgumentException($"无效的小时值: {hour}，必须是 0-24 之间的整数字符", nameof(hour));
        if (minute < 0 || minute > 59)
            throw new ArgumentException($"无效的分钟值: {minute}，必须是 0-59 之间的整数字符", nameof(minute));
        await new SetTimeTask().Start(hour, minute, CancellationContext.Instance.Cts.Token, skip);
    }
    
    /// <summary>
    /// 调整时间
    /// </summary>
    /// <param name="hour">目标小时(0-24的字符类型)</param>
    /// <param name="minute">目标分钟(0-59的字符类型)</param>
    /// <param name="skip">是否跳过动画（默认为否）</param>
    /// <returns></returns>
    public async Task SetTime(string hour, string minute, bool skip = false)
    {
        if (!int.TryParse(hour, out var h) || h < 0 || h > 24)
            throw new ArgumentException($"无效的小时值: {hour}，必须是 0-24 之间的整数字符", nameof(hour));
        if (!int.TryParse(minute, out var m) || m < 0 || m > 59)
            throw new ArgumentException($"无效的分钟值: {minute}，必须是 0-59 之间的整数字符", nameof(minute));
        await new SetTimeTask().Start(h, m, CancellationContext.Instance.Cts.Token, skip);
    }

    // /// <summary>
    // /// 莉奈娅挖矿，调试使用，暂时注释
    // /// </summary>
    // /// <param name="mineCount">射箭次数，默认1</param>
    // /// <param name="scanRounds">大循环寻矿次数。不传则默认5；传单个数字时与射箭次数相同</param>
    // public async Task StartMining(int? mineCount = null, int? scanRounds = null)
    // {
    //     var actualMine = mineCount ?? 1;
    //     var actualScan = scanRounds ?? (mineCount ?? 5);
    //     if (actualScan < actualMine) actualScan = actualMine;
    //     await new LinneaMiningTask(actualScan, actualMine).Start(CancellationContext.Instance.Cts.Token);
    // }
}
