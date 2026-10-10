using System.Windows.Media;
using BetterGenshinImpact.ViewModel.Pages;
using BetterGenshinImpact.ViewModel.Pages.OneDragon;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterGenshinImpact.Model;

public partial class OneDragonTaskItem
{
    [ObservableProperty] private Brush _statusColor = Brushes.Gray;

    [ObservableProperty] private OneDragonBaseViewModel? _viewModel;

    private static partial bool GetFightStrategy(string strategyName, out string path) =>
        App.GetService<TaskSettingsPageViewModel>()!.GetFightStrategy(strategyName, out path);
}
