using System;
using BetterGenshinImpact.ViewModel.Pages;

namespace BetterGenshinImpact.Core.Script.Dependence;

public partial class Dispatcher
{
    public Dispatcher(object? config) : this(config, new DesktopDefaults()) { }
    private sealed class DesktopDefaults : IScriptTaskDefaults
    {
        private TaskSettingsPageViewModel ViewModel => App.GetService<TaskSettingsPageViewModel>()
            ?? throw new InvalidOperationException("内部视图模型对象为空");
        public int AutoWoodRoundNum => ViewModel.AutoWoodRoundNum;
        public int AutoWoodDailyMaxCount => ViewModel.AutoWoodDailyMaxCount;
        public bool GetTcgStrategy(out string content) => ViewModel.GetTcgStrategy(out content);
        public bool GetFightStrategy(out string path) => ViewModel.GetFightStrategy(out path);
        public bool GetFightStrategy(string name, out string path) => ViewModel.GetFightStrategy(name, out path);
    }
}
