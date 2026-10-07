using BetterGenshinImpact.Core.Config;
using BetterGenshinImpact.Core.Recognition;
using System;
using System.Diagnostics;
using BetterGenshinImpact.GameTask.Common;
using BetterGenshinImpact.GameTask.Common.BgiVision;
using BetterGenshinImpact.GameTask.Common.Element.Assets;
using BetterGenshinImpact.GameTask.Model.Area;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace BetterGenshinImpact.GameTask.GameLoading;

public partial class GameLoadingTrigger : ITaskTrigger
{
    public static bool GlobalEnabled = true;
    
    public string Name => "自动开门";

    public bool IsEnabled { get => _useGlobalEnabled ? GlobalEnabled : _enabled; set {} }

    public int Priority => 999;

    public bool IsExclusive => false;

    public bool IsBiliJudged = false;
    public bool IsBili = false;

    public bool IsBackgroundRunning => true;

    private readonly GenshinStartConfig _config = TaskContext.Instance().Config.GenshinStartConfig;
    private static ILogger<GameLoadingTrigger> _logger = App.GetLogger<GameLoadingTrigger>();


    private DateTime _prevExecuteTime = DateTime.MinValue;

    private DateTime _triggerStartTime = DateTime.Now;

    private bool biliLoginClicked = false;
    private DateTime _prevAgePromptOcrTime = DateTime.MinValue;
    private readonly TimeSpan? _activeFor = TimeSpan.FromMinutes(5);
    private readonly bool _useGlobalEnabled = true;
    private bool _enabled = true;

    public GameLoadingTrigger()
    {
    }

    /// <summary>
    /// Creates an invocation-local trigger. Null keeps human login waiting open;
    /// the desktop default constructor retains the original five-minute limit.
    /// </summary>
    public GameLoadingTrigger(TimeSpan? activeFor)
    {
        if (activeFor is { } duration && duration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(activeFor));
        _activeFor = activeFor;
        _useGlobalEnabled = false;
    }

    public void InnerSetEnabled(bool enabled)
    {
        if (_useGlobalEnabled) GlobalEnabled = enabled;
        else _enabled = enabled;
    }

    public void Init()
    {
        if (!_config.AutoEnterGameEnabled)
        {
            InnerSetEnabled(false);
        }

        InitializePlatform();
    }

    public void OnCapture(CaptureContent content)
    {
        // 2s 一次
        if ((DateTime.Now - _prevExecuteTime).TotalMilliseconds <= 2000)
        {
            return;
        }

        _prevExecuteTime = DateTime.Now;
        // 桌面默认 5min；宿主可显式使用无人工等待期限的独立实例。
        if (_activeFor is { } activeFor && DateTime.Now - _triggerStartTime >= activeFor)
        {
            InnerSetEnabled(false);
            return;
        }
        
        // 成功进入游戏判断    
        if (Bv.IsInMainUi(content.CaptureRectArea) || Bv.IsInAnyClosableUi(content.CaptureRectArea) || Bv.IsInDomain(content.CaptureRectArea))
        {
            // _logger.LogInformation("当前在游戏主界面");
            InnerSetEnabled(false);
            return;
        }

        if ((DateTime.Now - _prevAgePromptOcrTime).TotalMilliseconds >= 1000)
        {
            _prevAgePromptOcrTime = DateTime.Now;
            var loadingOcrRegions = content.CaptureRectArea.FindMulti(RecognitionObject.OcrThis);
            try
            {
                if (loadingOcrRegions.Any(region =>
                        region.Text.Contains("适龄") || region.Text.Contains("监护")))
                {
                    // 适龄提示窗口自动关闭
                    using var agePopup = content.CaptureRectArea.Find(ElementRecognition.Get("BtnWhiteConfirm", content.CaptureRectArea));
                    if (!agePopup.IsEmpty())
                    {
                        agePopup.Click();
                        _logger.LogInformation("检测到适龄提示，自动点击确认");
                    }
                }
            }
            finally
            {
                foreach (var region in loadingOcrRegions) region.Dispose();
            }
        }

        // B服判断
        if (!IsBiliJudged)
        {
            DetectChannel();
            IsBiliJudged = true;
        }

        // 官服流程：先识别并点击顶号或切号的后一次“进入游戏”弹窗按钮
        if (!IsBili)
        {
            using var extraEnterGameBtn = content.CaptureRectArea.Find(RecognitionAssets.Get("GameLoading", "ChooseEnterGame", content.CaptureRectArea));
            if (!extraEnterGameBtn.IsEmpty())
            {
                extraEnterGameBtn.Click();
                return;
            }
        }

        // 点击进入游戏按钮
        using var ra = content.CaptureRectArea.Find(RecognitionAssets.Get("GameLoading", "EnterGame", content.CaptureRectArea));

        if (!ra.IsEmpty())
        {
            TaskContext.Instance().PostMessageSimulator.LeftButtonClickBackground();
            biliLoginClicked = true;
            return;
        }

        // 只有在"进入游戏"按钮未出现时，才进行B服登录处理
        if (IsBili && !biliLoginClicked)
        {
            ContinueChannelLogin();
        }

        if (Bv.IsInBlessingOfTheWelkinMoon(content.CaptureRectArea))
        {
            GameCaptureRegion.GameRegion1080PPosMove(100, 100);
            TaskContext.Instance().PostMessageSimulator.LeftButtonClickBackground();
            Debug.WriteLine("[GameLoading] Click blessing of the welkin moon");
            // TaskControl.Logger.LogInformation("自动点击月卡");
            return;
        }

        // 原石
        using var ysRa = content.CaptureRectArea.Find(ElementRecognition.Get("Primogem", content.CaptureRectArea));
        if (!ysRa.IsEmpty())
        {
            GameCaptureRegion.GameRegion1080PPosMove(100, 100);
            TaskContext.Instance().PostMessageSimulator.LeftButtonClickBackground();
            Debug.WriteLine("[GameLoading] 跳过原石");
            return;
        }
    }

}
