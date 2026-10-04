using System.Globalization;
using BetterGenshinImpact.Core.Config;
using BetterGenshinImpact.Core.Recognition;
using BetterGenshinImpact.Core.Simulator;
using BetterGenshinImpact.Core.Simulator.Extensions;
using BetterGenshinImpact.GameTask.AutoTrackPath;
using BetterGenshinImpact.GameTask.Common.BgiVision;
using BetterGenshinImpact.GameTask.Common.Job;
using BetterGenshinImpact.Helpers;
using BetterGenshinImpact.Runtime;
using OpenCvSharp;
using Xunit;
using RuntimeEnvironment = BetterGenshinImpact.Runtime.RuntimeEnvironment;

namespace BetterGenshinImpact.RuntimeTest;

public sealed class OfficialTaskTests
{
    [Fact]
    public void OfficialTaskLocalizationUsesItsOriginalResources()
    {
        Assert.Equal("Mondstadt", RuntimeEnvironment.Localizer<TpTask>().WithCultureGet(new CultureInfo("en"), "蒙德"));
    }

    [Fact]
    public void OfficialKeyBindingChangeReachesTarget()
    {
        var config = new GameConfiguration();
        config.KeyBindingsConfig.OpenMap = KeyId.N;
        var host = new TaskHost();
        using var session = new GameSession(host, configuration: config);
        Simulation.SendInput.SimulateAction(GIActions.OpenMap);
        Assert.Equal(new[] { ((int)KeyId.N, true), ((int)KeyId.N, false) }, host.Keys);
    }

    [Fact]
    public async Task OfficialReturnMainUiClosesMenuAndRecognizesMainUi()
    {
        var host = new TaskHost();
        using var session = new GameSession(host);
        using (var menu = session.Capture()) Assert.False(Bv.IsInMainUi(menu));
        await new ReturnMainUiTask().Start(CancellationToken.None);
        using var result = session.Capture();
        Assert.True(Bv.IsInMainUi(result));
        Assert.Equal(new[] { ((int)KeyId.Escape, true), ((int)KeyId.Escape, false) }, host.Keys);
    }

    [Fact]
    public void OriginalRecognitionConfigEvaluatesCropExpressions()
    {
        using var session = new GameSession(new TaskHost());
        var ro = RecognitionAssets.Get("Common/Element", "PaimonMenu");
        Assert.Equal(new Rect(0, 0, 480, 270), ro.RegionOfInterest);
        Assert.False(ro.TemplateImageMat!.Empty());
    }

    [Fact]
    public void GameAssetsResolveBothDirectorySeparators()
    {
        Assert.Equal(RuntimeEnvironment.ResolveResource(@"GameTask\Common\Element\Assets\Recognition.json"),
            RuntimeEnvironment.ResolveResource("GameTask/Common/Element/Assets/Recognition.json"));
    }

    [Fact]
    public void OfficialImageReaderSupportsUnicodePathsAndReleasesFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"地图-{Guid.NewGuid():N}.png");
        try
        {
            using (var source = new Mat(12, 16, MatType.CV_8UC3, new Scalar(10, 20, 30)))
                File.WriteAllBytes(path, source.ToBytes(".png"));
            using var decoded = Bv.ImRead(path);
            Assert.Equal(new Size(16, 12), decoded.Size());
            Assert.Equal(new Vec3b(10, 20, 30), decoded.At<Vec3b>(0, 0));
            using var exclusive = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        finally { File.Delete(path); }
    }

    private sealed class TaskHost : IGameHost
    {
        private bool mainUi;
        public List<(int, bool)> Keys { get; } = [];
        public Mat Capture(CancellationToken token)
        {
            var frame = new Mat(1080, 1920, MatType.CV_8UC3, new Scalar(90, 70, 40));
            if (mainUi)
            {
                using var template = Cv2.ImRead(Path.Combine(AppContext.BaseDirectory, "UpstreamAssets/GameTask/Common/Element/Assets/1920x1080/paimon_menu.png"));
                using var roi = new Mat(frame, new Rect(20, 20, template.Width, template.Height));
                template.CopyTo(roi);
            }
            return frame;
        }
        public void SetKey(int key, bool down) { Keys.Add((key, down)); if (key == (int)KeyId.Escape && !down) mainUi = true; }
        public void SetPointerButton(int button, bool down) { }
        public void MovePointer(double x, double y) { }
        public void MovePointerBy(int dx, int dy) { }
        public void Scroll(int notches) { }
    }
}
