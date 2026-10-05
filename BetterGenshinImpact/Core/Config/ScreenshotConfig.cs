using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterGenshinImpact.Core.Config;

public partial class ScreenshotConfig : ObservableObject
{
    public static readonly OpenCvSharp.Rect UidCoverRightBottomRect = new(1920 - 1685, 1080 - 1053, 178, 22);
    /// <summary>
    ///     是否启用遮罩窗口
    /// </summary>
    [ObservableProperty]
    private bool _screenshotEnabled;

    /// <summary>
    ///     UID遮盖是否启用
    /// </summary>
    [ObservableProperty]
    private bool _screenshotUidCoverEnabled = true;

    /// <summary>
    ///     是否保存奖励识别调试截图
    /// </summary>
    [ObservableProperty]
    private bool _rewardRecognitionScreenshotEnabled;

}
