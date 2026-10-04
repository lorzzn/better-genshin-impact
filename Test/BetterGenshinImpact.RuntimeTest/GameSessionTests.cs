using BetterGenshinImpact.Runtime;
using OpenCvSharp;
using Xunit;

namespace BetterGenshinImpact.RuntimeTest;

public sealed class GameSessionTests
{
    [Theory]
    [InlineData(3840, 2160, 1920, 1080, 500, 300)]
    [InlineData(1280, 720, 1280, 720, 250, 150)]
    public void OfficialRegionConvertsCropClickToTargetCoordinates(int width, int height, int normalizedWidth, int normalizedHeight, int x, int y)
    {
        var host = new TestHost { Width = width, Height = height };
        using var session = new GameSession(host);
        using var frame = session.Capture();
        Assert.Equal(normalizedWidth, frame.Width);
        Assert.Equal(normalizedHeight, frame.Height);
        using var crop = frame.DeriveCrop(new Rect(200, 100, 100, 100));
        crop.Click();
        Assert.Equal(new Point(x, y), host.Pointer);
        Assert.Empty(host.ButtonsDown);
    }

    [Fact]
    public void CancellationReleasesAllKeysAndButtons()
    {
        using var cancel = new CancellationTokenSource();
        var host = new TestHost();
        var session = new GameSession(host, cancel.Token);
        session.Key(87, true);
        session.Button(0, true);
        cancel.Cancel();
        Assert.Throws<OperationCanceledException>(() => session.Move(1, 2));
        session.Dispose();
        Assert.Empty(host.KeysDown);
        Assert.Empty(host.ButtonsDown);
    }

    [Fact]
    public void LostAcknowledgementStillReleasesPossiblyPressedKey()
    {
        var host = new TestHost { FailKeyDown = true };
        var session = new GameSession(host);
        Assert.Throws<IOException>(() => session.Key(87, true));
        session.Dispose();
        Assert.Empty(host.KeysDown);
    }

    [Fact]
    public void OneFailedReleaseDoesNotPreventOtherReleases()
    {
        var host = new TestHost { FailReleaseKey = 87 };
        var session = new GameSession(host);
        session.Key(87, true);
        session.Key(65, true);
        session.Button(0, true);
        Assert.Throws<AggregateException>(() => session.Dispose());
        Assert.Equal(new[] { 87 }, host.KeysDown);
        Assert.Empty(host.ButtonsDown);
        Assert.Throws<InvalidOperationException>(() => GameSession.Current);
    }

    [Fact]
    public void RepeatedDisposeDoesNotReplaceAnotherCurrentSession()
    {
        var disposed = new GameSession(new TestHost());
        disposed.Dispose();
        using var active = new GameSession(new TestHost());
        disposed.Dispose();
        Assert.Same(active, GameSession.Current);
    }

    [Fact]
    public void DisposedSessionRejectsFurtherInput()
    {
        var host = new TestHost();
        var session = new GameSession(host);
        session.Dispose();
        Assert.Throws<ObjectDisposedException>(() => session.Key(87, true));
        Assert.Empty(host.KeysDown);
    }

    [Fact]
    public void DimensionChangeRejectsFrameBeforeAnyInput()
    {
        var host = new TestHost();
        using var session = new GameSession(host);
        using var first = session.Capture();
        host.Width = 1280;
        Assert.Throws<InvalidOperationException>(() => session.Capture());
        Assert.Empty(host.KeysDown);
        Assert.Empty(host.ButtonsDown);
    }

    private sealed class TestHost : IGameHost
    {
        public int Width { get; set; } = 1920;
        public int Height { get; set; } = 1080;
        public Point Pointer;
        public HashSet<int> KeysDown { get; } = [];
        public HashSet<int> ButtonsDown { get; } = [];
        public bool FailKeyDown;
        public int FailReleaseKey;
        public Mat Capture(CancellationToken cancellationToken) => new(Height, Width, MatType.CV_8UC3, Scalar.Black);
        public void MovePointer(double x, double y) => Pointer = new Point((int)x, (int)y);
        public void MovePointerBy(int dx, int dy) => Pointer += new Point(dx, dy);
        public void Scroll(int notches) { }
        public void SetPointerButton(int button, bool down)
        {
            if (down) ButtonsDown.Add(button); else ButtonsDown.Remove(button);
        }
        public void SetKey(int virtualKey, bool down)
        {
            if (down)
            {
                KeysDown.Add(virtualKey);
                if (FailKeyDown) throw new IOException("Lost input acknowledgement");
            }
            else
            {
                if (virtualKey == FailReleaseKey) throw new IOException("Release failed");
                KeysDown.Remove(virtualKey);
            }
        }
    }
}
