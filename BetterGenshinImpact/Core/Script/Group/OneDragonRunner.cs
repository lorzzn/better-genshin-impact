using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BetterGenshinImpact.Core.Config;
using BetterGenshinImpact.GameTask.Common;
using BetterGenshinImpact.GameTask.Common.Job;
using BetterGenshinImpact.Model;
using BetterGenshinImpact.Service.Notification;
using BetterGenshinImpact.Service.Notification.Model.Enum;
using Microsoft.Extensions.Logging;

namespace BetterGenshinImpact.Core.Script.Group;

/// <summary>
/// 运行一条龙时由宿主提供的操作：启动截图器、按名称运行配置组、保存一条龙配置，
/// 以及界面提示与结束后的操作（关闭游戏、关机等）。
/// </summary>
public interface IOneDragonRunnerHost
{
    Task StartGameTask();

    Task RunThreadAsync(Func<Task> action);

    /// <summary>运行指定名称的配置组。</summary>
    Task RunScriptGroup(string name);

    void SaveConfig(OneDragonFlowConfig config);

    /// <summary>从 NextTaskId 开始执行后，配置中的标记已清除。</summary>
    void OnNextTaskConsumed(OneDragonFlowConfig config);

    void Warning(string message);

    void Error(string message);

    void OnCompleted(OneDragonFlowConfig config);
}

/// <summary>
/// 一条龙执行流程，自 OneDragonFlowViewModel 移出，桌面程序与嵌入程序共用。
/// </summary>
public class OneDragonRunner(IOneDragonRunnerHost host)
{
    private readonly ILogger _logger = TaskControl.Logger;

    /// <summary>一条龙内置任务；其余任务名为配置组名称。</summary>
    public static readonly string[] BuiltInTaskNames =
    [
        "领取邮件",
        "合成树脂",
        "自动秘境",
        "自动首领讨伐",
        "自动幽境危战",
        "自动地脉花",
        "领取每日奖励",
        "领取尘歌壶奖励",
    ];

    /// <summary>按配置中的顺序和启用状态还原任务列表。</summary>
    public static List<OneDragonTaskItem> LoadTaskList(OneDragonFlowConfig config)
    {
        var list = new List<OneDragonTaskItem>();
        if (config.TaskEnabledList == null)
        {
            return list;
        }

        // 旧格式兼容：TaskDefinitions 为空时，TaskEnabledList 键为任务名
        bool isOldFormat = config.TaskDefinitions == null || config.TaskDefinitions.Count == 0;

        // 使用 TaskOrder 恢复顺序；若无则回退到 TaskEnabledList 的键顺序
        var orderedKeys = config.TaskOrder?.Count > 0
            ? config.TaskOrder
            : config.TaskEnabledList.Keys.ToList();

        foreach (var key in orderedKeys)
        {
            if (!config.TaskEnabledList.TryGetValue(key, out var enabled))
            {
                continue;
            }

            OneDragonTaskItem taskItem;
            if (isOldFormat)
            {
                taskItem = new OneDragonTaskItem(key) { IsEnabled = enabled };
            }
            else
            {
                if (!config.TaskDefinitions!.TryGetValue(key, out var name))
                {
                    continue;
                }
                taskItem = new OneDragonTaskItem(name, key) { IsEnabled = enabled };
            }
            taskItem.IsNextTask = key == config.NextTaskId;
            list.Add(taskItem);
        }

        return list;
    }

    public async Task RunAsync(OneDragonFlowConfig config, List<OneDragonTaskItem> taskList)
    {
        // 启动等待之前先进行取消操作的初始化，便于在任务开始前终止任务.
        CancellationContext.Instance.Set();

        var taskListCopy = new List<OneDragonTaskItem>(taskList);//避免执行过程中修改TaskList

        // 如果设置了 NextTaskId，从指定任务开始执行
        if (!string.IsNullOrEmpty(config.NextTaskId))
        {
            var taskIndex = taskListCopy.FindIndex(t => t.Id == config.NextTaskId);
            if (taskIndex >= 0)
            {
                _logger.LogInformation("一条龙：任务将从 {Name} 开始执行", taskListCopy[taskIndex].Name);
                taskListCopy = taskListCopy.Skip(taskIndex).ToList();
            }
            else
            {
                _logger.LogWarning("一条龙：未找到标记的任务，将从头开始执行");
            }
            config.NextTaskId = string.Empty;
            host.OnNextTaskConsumed(config);
        }

        foreach (var task in taskListCopy)
        {
            task.InitAction(config);
        }

        int finishOneTaskcount = 1;
        int finishTaskcount = 1;
        int enabledTaskCountall = taskListCopy.Count(t => t.IsEnabled);
        _logger.LogInformation($"启用任务总数量: {enabledTaskCountall}");

        if (taskListCopy.Count(t => t.IsEnabled) == 0)
        {
            host.Warning("请先选择任务");
            _logger.LogInformation("没有配置,退出执行!");
            return;
        }

        int enabledoneTaskCount = taskListCopy.Count(t => t.IsEnabled);
        _logger.LogInformation($"启用一条龙任务的数量: {enabledoneTaskCount}");

        await host.StartGameTask();
        if (CancellationContext.Instance.IsCancellationRequested)
        {
            _logger.LogInformation("一条龙在启动阶段被取消");
            return;
        }

        host.SaveConfig(config);
        int enabledTaskCount = taskListCopy.Count(t =>
            t.IsEnabled && !BuiltInTaskNames.Contains(t.Name));
        _logger.LogInformation($"启用配置组任务的数量: {enabledTaskCount}");

        if (enabledoneTaskCount <= 0)
        {
            _logger.LogInformation("没有一条龙任务!");
        }

        Notify.Event(NotificationEvent.DragonStart).Success("一条龙启动");
        foreach (var task in taskListCopy)
        {
            if (task is { IsEnabled: true, Action: not null })
            {
                if (BuiltInTaskNames.Contains(task.Name))
                {
                    _logger.LogInformation($"一条龙任务执行: {finishOneTaskcount++}/{enabledoneTaskCount}");
                    await host.RunThreadAsync(async () =>
                    {
                        await task.Action();
                        await Task.Delay(1000);
                    });
                }
                else
                {
                    try
                    {
                        if (enabledTaskCount <= 0)
                        {
                            _logger.LogInformation("没有配置组任务,退出执行!");
                            return;
                        }

                        Notify.Event(NotificationEvent.DragonStart).Success("配置组任务启动");

                        if (config.TaskEnabledList[task.Id])
                        {
                            _logger.LogInformation($"配置组任务执行: {finishTaskcount++}/{enabledTaskCount}");
                            await Task.Delay(500);
                            await host.RunScriptGroup(task.Name);
                            await Task.Delay(1000);
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogDebug(e, "执行配置组任务时失败");
                        host.Error("执行配置组任务时失败");
                    }
                }
                // 如果任务已经被取消，中断所有任务
                if (CancellationContext.Instance.Cts.IsCancellationRequested)
                {
                    _logger.LogInformation("任务被取消，退出执行");
                    if (CancellationContext.Instance.IsManualStop is false)
                    {
                        Notify.Event(NotificationEvent.DragonEnd).Success("一条龙和配置组任务结束");
                    }
                    return; // 后续的检查任务也不执行
                }
            }
        }

        // 检查和最终结束的任务
        await host.RunThreadAsync(async () =>
        {
            await new CheckRewardsTask().Start(CancellationContext.Instance.Cts.Token);
            await Task.Delay(500);
            if (CancellationContext.Instance.IsManualStop is false)
            {
                Notify.Event(NotificationEvent.DragonEnd).Success("一条龙和配置组任务结束");
            }
            _logger.LogInformation("一条龙和配置组任务结束");

            // 执行完成后操作
            host.OnCompleted(config);
        });
    }
}
