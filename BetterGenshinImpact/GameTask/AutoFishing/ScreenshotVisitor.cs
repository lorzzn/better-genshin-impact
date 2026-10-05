using System;
using System.IO;
using System.Threading.Tasks;
using CsTrees;
using CsTrees.Visitors;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using BetterGenshinImpact.GameTask.Model.Area;
using BetterGenshinImpact.Core.Config;

namespace BetterGenshinImpact.GameTask.AutoFishing;

/// <summary>
/// CsTrees Visitor：在行为结束时自动截图。
/// 通过 Blackboard 获取当前帧，在行为的终态（Success/Failure）时保存截图。
/// </summary>
public class ScreenshotVisitor : VisitorBase
{
    private readonly ILogger _logger;

    public ScreenshotVisitor(ILogger logger) : base(full: false)
    {
        _logger = logger;
    }

    public override void Run(Behaviour behaviour)
    {
        if (behaviour.Status == Status.Running)
            return;

        if (behaviour is IScreenshotBehaviour screenshotBehaviour)
        {
            var currentFrame = screenshotBehaviour.Screenshot.Get();
            if (currentFrame == null)
                return;

            var fileName = $"{DateTime.Now:yyyyMMddHHmmssfff}_{behaviour.GetType().Name}_{behaviour.Status}.png";
            _logger.LogInformation("保存截图: {Name}", fileName);

            SaveScreenshot(currentFrame, fileName);
        }
    }

    public static void SaveScreenshot(ImageRegion imageRegion, string name)
    {
        var path = Global.Absolute($@"log\screenshot\");
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        if (String.IsNullOrWhiteSpace(name))
        {
            name = $@"{DateTime.Now:yyyyMMddHHmmssffff}.png";
        }
        var savePath = Global.Absolute($@"log\screenshot\{name}");

        var snapshot = imageRegion.SrcMat.Clone();
        var coverUid = TaskContext.Instance().Config.CommonConfig.ScreenshotUidCoverEnabled;
        var assetScale = TaskContext.Instance().SystemInfo.ScaleTo1080PRatio;
        _ = Task.Run(() =>
        {
            using (snapshot)
            {
                try
                {
                    if (coverUid)
                    {
                        var rect = new Rect((int)(snapshot.Width - ScreenshotConfig.UidCoverRightBottomRect.X * assetScale),
                            (int)(snapshot.Height - ScreenshotConfig.UidCoverRightBottomRect.Y * assetScale),
                            (int)(ScreenshotConfig.UidCoverRightBottomRect.Width * assetScale),
                            (int)(ScreenshotConfig.UidCoverRightBottomRect.Height * assetScale));
                        snapshot.Rectangle(rect, Scalar.White, -1);
                    }
                    Cv2.ImWrite(savePath, snapshot);
                }
                catch (Exception e)
                {
                    System.Diagnostics.Trace.TraceError("保存截图失败: {0}", e.Message);
                }
            }
        });
    }
}
