using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using BetterGenshinImpact.Core.Script.Dependence.Model.TimerConfig;
using BetterGenshinImpact.GameTask.AutoSkip;

namespace BetterGenshinImpact.GameTask;

/// <summary>The original named trigger registry, shared by desktop and embedded sessions.</summary>
public sealed class ScriptTriggerCollection
{
    public ConcurrentDictionary<string, ITaskTrigger>? TriggerDictionary { get; set; }
    public List<ITaskTrigger> ConvertToTriggerList(bool allEnabled = false)
    {
        if (TriggerDictionary is null)
        {
            return [];
        }

        var loadedTriggers = TriggerDictionary.Values.ToList();

        loadedTriggers.ForEach(i => i.Init());
        if (allEnabled)
        {
            loadedTriggers.ForEach(i => i.IsEnabled = true);
        }

        loadedTriggers = [.. loadedTriggers.OrderByDescending(i => i.Priority)];
        return loadedTriggers;
    }

    public void ClearTriggers()
    {
        TriggerDictionary?.Clear();
    }

    /// <summary>
    /// 通过名称添加触发器
    /// </summary>
    /// <param name="name"></param>
    /// <param name="externalConfig"></param>
    public bool AddTrigger(string name, object? externalConfig)
    {
        TriggerDictionary ??= new ConcurrentDictionary<string, ITaskTrigger>();

        ITaskTrigger? trigger = null;
        string? triggerName = null;
        switch (name)
        {
            case "AutoPick":
                triggerName = "AutoPick";
                trigger = new AutoPick.AutoPickTrigger(externalConfig as AutoPickExternalConfig);
                break;
            case "AutoSkip":
                triggerName = "AutoSkip";
                trigger = externalConfig is null ? new AutoSkip.AutoSkipTrigger() : new AutoSkip.AutoSkipTrigger(externalConfig as AutoSkipConfig);
                break;
            case "AutoEat":
                triggerName = "AutoEat";
                trigger = new AutoEat.AutoEatTrigger();
                break;
        }

        if (triggerName == null || trigger == null)
        {
            return false;
        }
        TriggerDictionary[triggerName] = trigger;
        return true;
    }

}
