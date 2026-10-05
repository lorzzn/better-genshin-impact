using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media;

namespace BetterGenshinImpact.GameTask.Music.Model;

public partial class PerformanceScore
{
    [ObservableProperty]
    private ImageSource? _artwork;
}
