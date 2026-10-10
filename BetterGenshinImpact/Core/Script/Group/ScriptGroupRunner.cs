using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BetterGenshinImpact.Core.Config;
using BetterGenshinImpact.Core.Script.Project;
using BetterGenshinImpact.GameTask;
using BetterGenshinImpact.GameTask.AutoPathing.Model;
using BetterGenshinImpact.GameTask.Common;
using BetterGenshinImpact.GameTask.AutoGeniusInvokation.Exception;
using BetterGenshinImpact.GameTask.Common.Job;
using BetterGenshinImpact.GameTask.FarmingPlan;
using BetterGenshinImpact.GameTask.LogParse;
using BetterGenshinImpact.Service.Notification;
using BetterGenshinImpact.Service.Notification.Model.Enum;
using Microsoft.Extensions.Logging;

namespace BetterGenshinImpact.Core.Script.Group;

/// <summary>
/// 运行配置组时由宿主提供的操作：桌面程序启动截图器、读取调度器页面的配置组、
/// 保存任务进度并在连续失败时重启；嵌入程序按自己的会话提供。
/// </summary>
public interface IScriptGroupRunnerHost
{
    Task StartGameTask();

    /// <summary>优先执行配置组按名称从这里查找。</summary>
    IEnumerable<ScriptGroup> GetScriptGroups();

    Task RunThreadAsync(Func<Task> action);

    void OnProjectStarting(ScriptGroupProject project);

    void SaveTaskProgress(GameTask.TaskProgress.TaskProgress taskProgress);

    /// <summary>一个条目结束且记录了任务进度后调用，桌面程序据此判断是否自动重启。</summary>
    void OnProjectEnded(GameTask.TaskProgress.TaskProgress taskProgress);
}

/// <summary>
/// 调度器配置组的执行流程，自 ScriptService 移出，桌面程序与嵌入程序共用。
/// </summary>
public class ScriptGroupRunner(IScriptGroupRunnerHost host)
{
    private readonly ILogger _logger = TaskControl.Logger;
    private readonly BlessingOfTheWelkinMoonTask _blessingOfTheWelkinMoonTask = new();

    /// <summary>当前正在执行的条目，供脚本通知等按条目权限判断。</summary>
    public static ScriptGroupProject? CurrentProject { get; private set; }

    private static bool IsCurrentHourEqual(string input)
    {
        // 尝试将输入字符串转换为整数
        if (int.TryParse(input, out int hour))
        {
            // 验证小时是否在合法范围内（0-23）
            if (hour is >= 0 and <= 23)
            {
                // 获取当前小时数
                int currentHour = DateTime.Now.Hour;
                // 判断是否相等
                return currentHour == hour;
            }
        }

        // 如果输入非数字或不合法，返回 false
        return false;
    }

    public bool ShouldSkipTask(ScriptGroupProject project)
    {

        if (project.GroupInfo is { Config.PathingConfig.Enabled: true } )
        {
            if (IsCurrentHourEqual(project.GroupInfo.Config.PathingConfig.SkipDuring))
            {
                _logger.LogInformation($"{project.Name}任务已到禁止执行时段，将跳过！");
                return true;
            }

            var tcc = project.GroupInfo.Config.PathingConfig.TaskCycleConfig;
            if (tcc.Enable)
            {
                int index = tcc.GetExecutionOrder();
                if (index == -1)
                {
                    _logger.LogInformation($"{project.Name}周期配置参数错误，配置将不生效，任务正常执行！");
                }
                else if (index != tcc.Index)
                {
                    _logger.LogInformation($"{project.Name}任务已经不在执行周期（当前值${index}!=配置值${tcc.Index}），将跳过此任务！");
                    return true;
                }

            }

        }

        if (TaskContext.Instance().Config.OtherConfig.FarmingPlanConfig.Enabled)
        {
            try
            {
                var task = PathingTask.BuildFromFilePath(Path.Combine(Global.Absolute(@"User\AutoPathing"), project.FolderName, project.Name));
                if (task is null)
                {
                    return true;
                }
                string message;
                if (FarmingStatsRecorder.IsDailyFarmingLimitReached(task.FarmingInfo,out message))
                {
                    _logger.LogInformation($"{project.Name}:{message},跳过此任务！");
                    return true;
                }
            }
            catch (Exception e)
            {
                TaskControl.Logger.LogError($"锄地规划统计异常：{e.Message}");
            }


        }
        string skipMessage;
        if (ExecutionRecordStorage.IsSkipTask(project,out skipMessage))
        {
            TaskControl.Logger.LogInformation($"{project.Name}:{skipMessage},跳过此任务！");
            return true;
        }
        return false; // 不跳过
    }




    //优先执行的配置组，统计每个project执行次数
    private readonly Dictionary<string, int> _projectExecutionCount = new();

    public async Task RunMulti(IEnumerable<ScriptGroupProject> projectList, string? groupName = null, GameTask.TaskProgress.TaskProgress? taskProgress = null)
    {
        groupName ??= "默认";

        // 启动等待之前先进行取消操作的初始化，便于在任务开始前终止任务.
        CancellationContext.Instance.Set();

        var list = ReloadScriptProjects(projectList);

        //恢复临时的跳过标志
        foreach (var scriptGroupProject in projectList)
        {
            scriptGroupProject.SkipFlag = false;
        }

        // 没启动时候，启动截图器
        await host.StartGameTask();
        if (CancellationContext.Instance.IsCancellationRequested)
        {
            _logger.LogInformation("配置组 {Name} 在启动阶段被取消", groupName);
            return;
        }


        if (!string.IsNullOrEmpty(groupName)&&!RunnerContext.Instance.IsPreExecution)
        {
            _logger.LogInformation("配置组 {Name} 加载完成，共{Cnt}个脚本，开始执行", groupName, list.Count);
        }

        bool fisrt = true;


        //非优先执行配置下，清空执行计数
        if (!RunnerContext.Instance.IsPreExecution)
        {
            _projectExecutionCount.Clear();
        }


        await host.RunThreadAsync(async () =>
            {
                var stopwatch = new Stopwatch();
                int projectIndex = -1;
                for (int x = 0; x < list.Count; x++)
                {
                    var project = list[x];
                    //正常情况下，只有一个真正执行的project，存在其他优先执行配置组情况下，会有多个任务。
                    List<ScriptGroupProject> exeProjects = [project];
                    RunnerContext.Instance.IsPreExecution = false;
                    //优先执行配置组逻辑
                    if (!RunnerContext.Instance.IsPreExecution &&
                        (project.GroupInfo?.Config.PathingConfig.Enabled ?? false) &&
                        project.GroupInfo.Config.PathingConfig.PreExecutionPriorityConfig.Enabled)
                    {
                        var preConfig = project.GroupInfo.Config.PathingConfig.PreExecutionPriorityConfig;
                        var groupNames = preConfig.GroupNames;

                        if (!string.IsNullOrWhiteSpace(groupNames))
                        {
                            // 解析组名集合
                            var groupNameSet = groupNames
                                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(name => name.Trim())
                                .Where(name => !string.IsNullOrWhiteSpace(name));

                            // 获取匹配的脚本组
                            var scriptGroups = host.GetScriptGroups()
                                .Where(g => groupNameSet.Contains(g.Name, StringComparer.OrdinalIgnoreCase))
                                .ToList();

                            // 收集需要执行的项目
                            var preExecutionProjects = new List<ScriptGroupProject>();
                            foreach (var group in scriptGroups)
                            {
                                var skipConfig = group.Config.PathingConfig.TaskCompletionSkipRuleConfig;
                                var records = ExecutionRecordStorage.GetRecentExecutionRecordsByConfig(skipConfig);

                                foreach (var p in group.Projects)
                                {
                                    // 检查是否应该跳过任务
                                    if (ExecutionRecordStorage.IsSkipTask(p, out _, records))
                                        continue;

                                    // 生成项目唯一标识
                                    var projectKey = $"{p.Name}|{p.FolderName}|{p.GroupInfo?.Name}";

                                    // 检查执行次数
                                    if (!_projectExecutionCount.TryGetValue(projectKey, out var count))
                                    {
                                        count = 0;
                                    }

                                    // 检查是否超过最大重试次数
                                    if (count > preConfig.MaxRetryCount)
                                        continue;

                                    //增加执行计数
                                    //_projectExecutionCount[projectKey] = count + 1;
                                    preExecutionProjects.Add(p);
                                }
                            }

                            // 存在优先执行的项目，则优先执行
                            if (preExecutionProjects.Count > 0)
                            {

                                _logger.LogInformation($"存在{preExecutionProjects.Count}个需优先执行的任务！");
                                // 设置执行状态，进入优先执行任务
                                RunnerContext.Instance.IsPreExecution = true;
                                //重新构造需要执行的配置组
                                exeProjects = preExecutionProjects.Concat(new[] { project }).ToList();
                            }
                        }
                    }

                    if (!RunnerContext.Instance.IsPreExecution)
                    {
                        projectIndex++;
                    }


                    for (int y = 0; y < exeProjects.Count; y++)
                    {
                        var exeProject = exeProjects[y];
                        //最后一个执行的project，恢复正常执行状态
                        if (y == exeProjects.Count - 1)
                        {
                            RunnerContext.Instance.IsPreExecution = false;
                        }
                        if (!RunnerContext.Instance.IsPreExecution && taskProgress != null && taskProgress.Next != null)
                        {
                            if (taskProgress.Next.Index > projectIndex)
                            {
                                continue;
                            }

                            taskProgress.Next = null;
                        }

                        if (exeProject is { SkipFlag: true })
                        {
                            continue;
                        }

                        if (ShouldSkipTask(exeProject))
                        {
                            continue;
                        }

                        //月卡检测
                        await _blessingOfTheWelkinMoonTask.Start(CancellationContext.Instance.Cts.Token);
                        if (exeProject.Status != "Enabled")
                        {
                            _logger.LogInformation("脚本 {Name} 状态为禁用，跳过执行", exeProject.Name);
                            continue;
                        }

                        if (CancellationContext.Instance.Cts.IsCancellationRequested)
                        {
                            break;
                        }

                        if (fisrt )
                        {
                            fisrt = false;
                            Notify.Event(NotificationEvent.GroupStart).Success($"配置组{groupName}启动");
                        }

                        if (!RunnerContext.Instance.IsPreExecution &&taskProgress != null)
                        {
                            taskProgress.CurrentScriptGroupProjectInfo = new GameTask.TaskProgress.TaskProgress.ScriptGroupProjectInfo
                            {
                                Name = exeProject.Name,
                                FolderName = exeProject.FolderName, Index = projectIndex,
                                GroupName = taskProgress?.CurrentScriptGroupName ?? ""
                            };
                            host.SaveTaskProgress(taskProgress!);
                        }

                        //优先执行的任务，需要计数
                        if (RunnerContext.Instance.IsPreExecution)
                        {
                            // 生成项目唯一标识
                            var projectKey = $"{exeProject.Name}|{exeProject.FolderName}|{exeProject.GroupInfo?.Name}";
                            // 检查执行次数
                            if (!_projectExecutionCount.TryGetValue(projectKey, out var preExecutionCount))
                            {
                                preExecutionCount = 0;
                            }

                            _projectExecutionCount[projectKey] = preExecutionCount + 1;
                        }


                        for (var i = 0; i < exeProject.RunNum; i++)
                        {
                            try
                            {
                                TaskTriggerDispatcher.Instance().ClearTriggers();


                                _logger.LogInformation("------------------------------");

                                stopwatch.Reset();
                                stopwatch.Start();

                                await ExecuteProject(exeProject);

                                //多次执行时及时中断
                                if (exeProject.RunNum > 1 && ShouldSkipTask(exeProject))
                                {
                                    continue;
                                }
                            }
                            catch (NormalEndException)
                            {
                                throw;
                            }
                            catch (OperationCanceledException e)
                            {
                                _logger.LogInformation("取消执行配置组: {Msg}", e.Message);
                                throw;
                            }
                            catch (Exception e)
                            {
                                _logger.LogDebug(e, "执行脚本时发生异常");
                                _logger.LogError("执行脚本时发生异常: {Msg}", e.Message);
                                if (!RunnerContext.Instance.IsPreExecution && taskProgress != null && taskProgress.CurrentScriptGroupProjectInfo != null)
                                {
                                    taskProgress.CurrentScriptGroupProjectInfo.Status = 2;
                                }
                            }
                            finally
                            {
                                stopwatch.Stop();
                                var elapsedTime = TimeSpan.FromMilliseconds(stopwatch.ElapsedMilliseconds);
                                _logger.LogInformation("→ 脚本执行结束: {Name}, 耗时: {Minutes}分{Seconds:0.000}秒", exeProject.Name,
                                    elapsedTime.Hours * 60 + elapsedTime.Minutes, elapsedTime.TotalSeconds % 60);
                                _logger.LogInformation("------------------------------");
                            }

                            await Task.Delay(1000);
                        }

                        if (!RunnerContext.Instance.IsPreExecution && taskProgress != null)
                        {
                            if (taskProgress.CurrentScriptGroupProjectInfo != null)
                            {
                                taskProgress.CurrentScriptGroupProjectInfo.TaskEnd = true;
                                taskProgress.CurrentScriptGroupProjectInfo.EndTime = DateTime.Now;
                                if (taskProgress.CurrentScriptGroupProjectInfo.Status == 1)
                                {
                                    taskProgress.ConsecutiveFailureCount = 0;
                                    taskProgress.LastSuccessScriptGroupProjectInfo =
                                        taskProgress.CurrentScriptGroupProjectInfo;
                                    taskProgress.LastScriptGroupName = taskProgress.CurrentScriptGroupName;
                                }

                                //累计连续失败次数
                                if (taskProgress.CurrentScriptGroupProjectInfo.Status == 2)
                                {
                                    taskProgress.ConsecutiveFailureCount++;
                                }

                                taskProgress?.History?.Add(taskProgress.CurrentScriptGroupProjectInfo);
                                host.SaveTaskProgress(taskProgress!);
                            }

                            host.OnProjectEnded(taskProgress!);
                        }
                    }
                }
            });


        if (!string.IsNullOrEmpty(groupName)&&!RunnerContext.Instance.IsPreExecution)
        {
            _logger.LogInformation("配置组 {Name} 执行结束", groupName);
        }

        if (!fisrt&&!RunnerContext.Instance.IsPreExecution)
        {
            if (CancellationContext.Instance.IsManualStop is false)
            {
                Notify.Event(NotificationEvent.GroupEnd).Success($"配置组{groupName}结束");
            }
        }

        if (taskProgress != null)
        {
            taskProgress.Next = null;
        }

    }

    private List<ScriptGroupProject> ReloadScriptProjects(IEnumerable<ScriptGroupProject> projectList)
    {
        var list = new List<ScriptGroupProject>();
        foreach (var project in projectList)
        {
            if (project.Type == "Javascript")
            {
                var newProject = new ScriptGroupProject(new ScriptProject(project.FolderName));
                CopyProjectProperties(project, newProject);
                list.Add(newProject);
            }
            else if (project.Type == "KeyMouse")
            {
                var newProject = ScriptGroupProject.BuildKeyMouseProject(project.Name);
                CopyProjectProperties(project, newProject);
                list.Add(newProject);
            }
            else if (project.Type == "Pathing")
            {
                var newProject = ScriptGroupProject.BuildPathingProject(project.Name, project.FolderName);
                CopyProjectProperties(project, newProject);
                list.Add(newProject);
            }
            else if (project.Type == "Shell")
            {
                var newProject = ScriptGroupProject.BuildShellProject(project.Name);
                CopyProjectProperties(project, newProject);
                list.Add(newProject);
            }
        }

        return list;
    }

    private void CopyProjectProperties(ScriptGroupProject source, ScriptGroupProject target)
    {
        target.Status = source.Status;
        target.Schedule = source.Schedule;
        target.RunNum = source.RunNum;
        target.JsScriptSettingsObject = source.JsScriptSettingsObject;
        target.GroupInfo = source.GroupInfo;
        target.AllowJsNotification = source.AllowJsNotification;
        target.AllowJsHTTPHash = source.AllowJsHTTPHash;
        target.SkipFlag = source.SkipFlag;
    }

    private async Task ExecuteProject(ScriptGroupProject project)
    {
        CurrentProject = project;
        host.OnProjectStarting(project);
        try
        {
            if (project.Type == "Javascript")
            {
                if (project.Project == null)
                {
                    throw new Exception("Project 为空");
                }

                _logger.LogInformation("→ 开始执行JS脚本: {Name}", project.Name);
                if (RunnerContext.Instance.IsPreExecution) _logger.LogInformation("此任务为优先执行任务！");
                var hasSettingsBeforeRun = project.JsScriptSettingsObject != null;
                try
                {
                    await project.Run();
                }
                finally
                {
                    SaveScriptGroupAfterJsRun(project, hasSettingsBeforeRun);
                }
            }
            else if (project.Type == "KeyMouse")
            {
                _logger.LogInformation("→ 开始执行键鼠脚本: {Name}", project.Name);
                if (RunnerContext.Instance.IsPreExecution) _logger.LogInformation("此任务为优先执行任务！");
                await project.Run();
            }
            else if (project.Type == "Pathing")
            {
                _logger.LogInformation("→ 开始执行地图追踪任务: {Name}", project.Name);
                if (RunnerContext.Instance.IsPreExecution) _logger.LogInformation("此任务为优先执行任务！");
                await project.Run();
            }
            else if (project.Type == "Shell")
            {
                _logger.LogInformation("→ 开始执行shell: {Name}", project.Name);
                if (RunnerContext.Instance.IsPreExecution) _logger.LogInformation("此任务为优先执行任务！");
                await project.Run();
            }
        }
        finally
        {
            CurrentProject = null;
        }
    }

    private void SaveScriptGroupAfterJsRun(ScriptGroupProject project, bool hasSettingsBeforeRun)
    {
        if (!hasSettingsBeforeRun)
        {
            project.JsScriptSettingsObject = null;
            return;
        }

        var scriptGroup = project.GroupInfo!;
        try
        {
            var scriptGroupPath = Global.Absolute(@"User\ScriptGroup");
            if (!Directory.Exists(scriptGroupPath))
            {
                Directory.CreateDirectory(scriptGroupPath);
            }

            var file = Path.Combine(scriptGroupPath, $"{scriptGroup.Name}.json");
            scriptGroup.WriteToFileAtomically(file);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存JS脚本配置组失败: {GroupName}", scriptGroup.Name);
        }
    }
}
