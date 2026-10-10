using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BetterGenshinImpact.Core.Config;
using BetterGenshinImpact.Core.Script;
using BetterGenshinImpact.Core.Script.Dependence;
using BetterGenshinImpact.Core.Script.Group;
using BetterGenshinImpact.Core.Script.Project;
using BetterGenshinImpact.GameTask;
using BetterGenshinImpact.GameTask.AutoGeniusInvokation.Exception;
using BetterGenshinImpact.GameTask.AutoPathing.Model;
using BetterGenshinImpact.GameTask.Common;
using BetterGenshinImpact.GameTask.Common.BgiVision;
using BetterGenshinImpact.GameTask.Common.Job;
using BetterGenshinImpact.GameTask.FarmingPlan;
using BetterGenshinImpact.GameTask.LogParse;
using BetterGenshinImpact.GameTask.TaskProgress;
using BetterGenshinImpact.Service.Interface;
using BetterGenshinImpact.Service.Notification;
using BetterGenshinImpact.Service.Notification.Model.Enum;
using BetterGenshinImpact.ViewModel.Pages;
using Microsoft.Extensions.Logging;

namespace BetterGenshinImpact.Service;

public partial class ScriptService : IScriptService
{
    private readonly ILogger<ScriptService> _logger = App.GetLogger<ScriptService>();
    private readonly ScriptGroupRunner _runner;

    public ScriptService()
    {
        _runner = new ScriptGroupRunner(new DesktopRunnerHost(_logger));
    }

    public bool ShouldSkipTask(ScriptGroupProject project) => _runner.ShouldSkipTask(project);

    public Task RunMulti(IEnumerable<ScriptGroupProject> projectList, string? groupName = null, TaskProgress? taskProgress = null)
        => _runner.RunMulti(projectList, groupName, taskProgress);

    private sealed class DesktopRunnerHost(ILogger logger) : IScriptGroupRunnerHost
    {
        public Task StartGameTask() => ScriptService.StartGameTask();

        public IEnumerable<ScriptGroup> GetScriptGroups() => App.GetService<ScriptControlViewModel>()!.ScriptGroups;

        public Task RunThreadAsync(Func<Task> action) => new TaskRunner().RunThreadAsync(action);

        public void OnProjectStarting(ScriptGroupProject project) => TaskContext.Instance().CurrentScriptProject = project;

        public void SaveTaskProgress(TaskProgress taskProgress) => TaskProgressManager.SaveTaskProgress(taskProgress);

        public void OnProjectEnded(TaskProgress taskProgress)
        {
            //异常达到一次次数，重启bgi
            var autoconfig = TaskContext.Instance().Config.OtherConfig.AutoRestartConfig;
            if (autoconfig.Enabled && taskProgress.ConsecutiveFailureCount >= autoconfig.FailureCount)
            {
                logger.LogInformation("调度器任务出现未预期的异常，自动重启bgi");
                Notify.Event(NotificationEvent.GroupEnd).Error("调度器任务出现未预期的异常，自动重启bgi");
                if (autoconfig.RestartGameTogether
                    && TaskContext.Instance().Config.GenshinStartConfig.LinkedStartEnabled
                    && TaskContext.Instance().Config.GenshinStartConfig.AutoEnterGameEnabled)
                {
                    SystemControl.CloseGame();
                    Thread.Sleep(2000);
                }

                SystemControl.RestartApplication(["--TaskProgress", taskProgress.Name]);
            }
        }
    }

    private async Task<List<string>> ReadCodeList(List<ScriptProject> list)
    {
        var codeList = new List<string>();
        foreach (var project in list)
        {
            var code = await project.LoadCode();
            codeList.Add(code);
        }

        return codeList;
    }

    private bool HasTimerOperation(IEnumerable<string> codeList)
    {
        return codeList.Any(code => DispatcherAddTimerRegex().IsMatch(code));
    }

    [GeneratedRegex(@"^(?!\s*\/\/)\s*dispatcher\.\s*addTimer", RegexOptions.Multiline)]
    private static partial Regex DispatcherAddTimerRegex();


    public static async Task StartGameTask(bool waitForMainUi = true)
    {
        // 没启动时候，启动截图器
        var homePageViewModel = App.GetService<HomePageViewModel>();
        if (!homePageViewModel!.TaskDispatcherEnabled)
        {
            await homePageViewModel.OnStartTriggerAsync();

            if (waitForMainUi)
            {
                await Task.Run(async () =>
                {
                    await Task.Delay(200);
                    var first = true;
                    var sw = Stopwatch.StartNew();
                    var loseFocusCount = 0;
                    while (true)
                    {
                        if (CancellationContext.Instance.IsCancellationRequested)
                        {
                            TaskControl.Logger.LogInformation("检测到停止指令，退出启动等待");
                            return;
                        }

                        if (!homePageViewModel.TaskDispatcherEnabled || !TaskContext.Instance().IsInitialized)
                        {
                            await Task.Delay(500);
                            continue;
                        }

                        using var content = TaskControl.CaptureToRectArea();
                        if (Bv.IsInMainUi(content) || Bv.IsInAnyClosableUi(content) || Bv.IsInDomain(content))
                        {
                            return;
                        }

                        if (first)
                        {
                            first = false;
                            TaskControl.Logger.LogInformation("当前不在游戏主界面，等待进入主界面后执行任务...");
                            TaskControl.Logger.LogInformation("如果你已经在游戏内的其他界面，请自行退出当前界面（ESC），或是30秒后将程序将自动尝试到入主界面，使当前任务能够继续运行！");
                        }

                        await Task.Delay(500);
                        if (sw.Elapsed.TotalSeconds >= 30)
                        {
                            //防止自启动游戏后因为一些原因失焦，导致一直卡住
                            if (!SystemControl.IsGenshinImpactActiveByProcess())
                            {
                                loseFocusCount++;
                                if (loseFocusCount>50 && loseFocusCount<100)
                                {
                                    SystemControl.MinimizeAndActivateWindow(TaskContext.Instance().GameHandle);
                                }
                                SystemControl.ActivateWindow();
                            }

                            //自启动游戏，如果鼠标在游戏外面，将无法自动开门，这里尝试移动到游戏界面
                            if (sw.Elapsed.TotalSeconds < 200)
                            {
                                GlobalMethod.MoveMouseTo(300, 300);
                            }

                        }
                    }
                });
            }
        }

        // 等待命令行启动时并行执行的自动更新完成（如果有）
        var pendingUpdate = ScriptRepoUpdater.Instance.CommandLineAutoUpdateTask;
        if (pendingUpdate != null)
        {
            await pendingUpdate;
            ScriptRepoUpdater.Instance.CommandLineAutoUpdateTask = null;
        }
    }
}
