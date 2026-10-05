using System.Threading;
using System.Threading.Tasks;
using BetterGenshinImpact.Core.Simulator;
using BetterGenshinImpact.Core.Simulator.Extensions;

namespace BetterGenshinImpact.GameTask.Common;

public partial class TaskControl
{
    /// <summary>
    /// 模拟长按指定动作。使用 try/finally 块确保在任务被取消或发生异常时，按键也能安全释放，防止卡键。
    /// </summary>
    /// <param name="action">需要模拟的游戏动作（如元素战技、普通攻击等）</param>
    /// <param name="holdMs">长按持续的时间（毫秒）</param>
    /// <param name="ct">用于监控任务取消的取消令牌</param>
    public static async Task SimulateHoldActionAsync(GIActions action, int holdMs, CancellationToken ct)
    {
        try
        {
            Simulation.SendInput.SimulateAction(action, KeyType.KeyDown);
            await Delay(holdMs, ct);
        }
        finally
        {
            Simulation.SendInput.SimulateAction(action, KeyType.KeyUp);        
        }
    }

    /// <summary>
    /// 模拟长按元素战技（如万叶长E）。包含释放前摇、长按以及释放后的缓冲延时。
    /// </summary>
    /// <param name="holdMs">元素战技按住的时间（毫秒）</param>
    /// <param name="ct">用于监控任务取消的取消令牌</param>
    /// <param name="releaseLeftMouseBefore">是否在按下元素战技前先松开鼠标左键，避免输入冲突，默认 true</param>
    /// <param name="releaseLeftMouseDelayMs">松开鼠标左键后的缓冲时间（毫秒），默认 10ms</param>
    /// <param name="postKeyUpDelayMs">元素战技释放后的缓冲时间（毫秒），默认 50ms</param>
    public static async Task SimulateHoldElementalSkillAsync(
        int holdMs,
        CancellationToken ct,
        bool releaseLeftMouseBefore = true,
        int releaseLeftMouseDelayMs = 10,
        int postKeyUpDelayMs = 50)
    {
        if (releaseLeftMouseBefore)
        {
            Simulation.SendInput.Mouse.LeftButtonUp();
            await Delay(releaseLeftMouseDelayMs, ct);
        }

        await SimulateHoldActionAsync(GIActions.ElementalSkill, holdMs, ct);   
        await Delay(postKeyUpDelayMs, ct);
    }

    /// <summary>
    /// 模拟鼠标左键连续点击循环（如万叶长E后的下落攻击）。双层 try/finally 设计以确保无论在循环的哪个阶段发生取消或异常，鼠标左键都会被强制释放。
    /// </summary>
    /// <param name="repeatCount">需要循环点击的次数</param>
    /// <param name="ct">用于监控任务取消的取消令牌</param>
    /// <param name="preUpDelayMs">每次点击前，预先抬起左键后的缓冲延时（毫秒），默认 10ms</param>
    /// <param name="downHoldMs">鼠标左键按下的保持时间（毫秒），默认 35ms</param>
    /// <param name="postUpDelayMs">每次点击完成后的等待时间（毫秒），默认 50ms</param>
    public static async Task SimulateMouseLeftClickLoopAsync(
        int repeatCount,
        CancellationToken ct,
        int preUpDelayMs = 10,
        int downHoldMs = 35,
        int postUpDelayMs = 50)
    {
        try
        {
            for (var i = 0; i < repeatCount; i++)
            {
                Simulation.SendInput.Mouse.LeftButtonUp();
                await Delay(preUpDelayMs, ct);
                Simulation.SendInput.Mouse.LeftButtonDown();
                try
                {
                    await Delay(downHoldMs, ct);
                }
                finally
                {
                    Simulation.SendInput.Mouse.LeftButtonUp();
                }

                await Delay(postUpDelayMs, ct);
            }
        }
        finally
        {
            Simulation.SendInput.Mouse.LeftButtonUp();
        }
    }

}
