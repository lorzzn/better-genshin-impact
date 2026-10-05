using System;
using System.Collections.Generic;
using System.Linq;
using BetterGenshinImpact.GameTask.Common.BgiVision;

namespace BetterGenshinImpact.GameTask;

/// <summary>Shared official exclusive-trigger and game-UI dispatch policy.</summary>
public sealed class TriggerProcessor
{
    private GameUiCategory PrevGameUiCategory = GameUiCategory.Unknown;
    private DateTime PrevGameUiChangeTime = DateTime.Now;
    public void Process(CaptureContent content, IReadOnlyList<ITaskTrigger> triggers, bool backgroundOnly, Action<ITaskTrigger> invoke)
    {
                    var needRunTriggers = new List<ITaskTrigger>(); // 最终要执行的触发器列表
                    var exclusiveTrigger = triggers.FirstOrDefault(t => t is { IsEnabled: true, IsExclusive: true });
                    if (exclusiveTrigger != null)
                    {
                        needRunTriggers.Add(exclusiveTrigger);
                    }
                    else
                    {
                        var runningTriggers = triggers.Where(t => t.IsEnabled);
                        if (backgroundOnly)
                        {
                            runningTriggers = runningTriggers.Where(t => t.IsBackgroundRunning);
                        }

                        needRunTriggers.AddRange(runningTriggers);
                    }

                    if (needRunTriggers.Count > 0)
                    {
                        // 判断当前UI
                        content.CurrentGameUiCategory = Bv.WhichGameUiForTriggers(content.CaptureRectArea);
                        
                        if (content.CurrentGameUiCategory != PrevGameUiCategory)
                        {
                            PrevGameUiChangeTime = DateTime.Now;
                        }

                        foreach (var trigger in needRunTriggers)
                        {
                            if ((PrevGameUiCategory != content.CurrentGameUiCategory || (DateTime.Now - PrevGameUiChangeTime).TotalSeconds <= 30) // UI变化了后的30s内则所有触发器执行一遍
                                || trigger.SupportedGameUiCategory == content.CurrentGameUiCategory)
                            {
                                // 触发器耗时只累计触发器执行本体，便于和截图耗时、总处理耗时拆开观察。
                                invoke(trigger);
                            }
                        }

                        PrevGameUiCategory = content.CurrentGameUiCategory;
                    }
    }
}
