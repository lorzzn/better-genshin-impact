using BetterGenshinImpact.GameTask.Model.Area;
using BetterGenshinImpact.View.Drawable;
using OpenCvSharp;

namespace BetterGenshinImpact.Runtime;

/// <summary>Scoped game context used by the original task and region classes.</summary>
public sealed class GameSession : IDisposable
{
    private static readonly AsyncLocal<GameSession?> current = new();
    private readonly GameSession? previous;
    private Mat? initialFrame;
    private readonly HashSet<int> pressedKeys = [];
    private readonly HashSet<int> pressedButtons = [];
    private bool disposed;
    public static GameSession Current => current.Value ?? throw new InvalidOperationException("No game host is bound to this invocation");
    public IGameHost Host { get; }
    public CancellationToken CancellationToken { get; }
    public GameSystemInfo SystemInfo { get; private set; }
    public DrawContent Overlay { get; } = new();
    public GameConfiguration Config { get; }
    /// <summary>Optional binding to the official path executor when that component is installed.</summary>
    public Func<GameTask.AutoPathing.Model.Waypoint, string, string, CancellationToken, Task>? Pathing { get; init; }
    public Point PointerPosition { get; private set; }

    public GameSession(IGameHost host, CancellationToken cancellationToken = default, GameConfiguration? configuration = null)
    {
        ArgumentNullException.ThrowIfNull(host);
        Host = host;
        CancellationToken = cancellationToken;
        Config = configuration ?? new();
        initialFrame = host.Capture(cancellationToken);
        if (initialFrame.Empty()) { initialFrame.Dispose(); throw new InvalidDataException("Game host returned an empty frame"); }
        SystemInfo = new(initialFrame.Width, initialFrame.Height);
        previous = current.Value;
        current.Value = this;
    }

    public ImageRegion Capture()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        CancellationToken.ThrowIfCancellationRequested();
        var frame = initialFrame ?? Host.Capture(CancellationToken);
        initialFrame = null;
        if (frame.Empty()) { frame.Dispose(); throw new InvalidDataException("Game host returned an empty frame"); }
        if (frame.Width != SystemInfo.Width || frame.Height != SystemInfo.Height)
        {
            frame.Dispose();
            throw new InvalidOperationException("Game surface dimensions changed during the task; start a new invocation");
        }
        return new DesktopRegion(frame.Width, frame.Height).Derive(frame, 0, 0).DeriveTo1080P();
    }

    public void Move(double x, double y)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        CancellationToken.ThrowIfCancellationRequested();
        Host.MovePointer(x, y);
        PointerPosition = new Point((int)Math.Round(x), (int)Math.Round(y));
    }

    public void MoveBy(int dx, int dy)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        CancellationToken.ThrowIfCancellationRequested();
        Host.MovePointerBy(dx, dy);
        PointerPosition += new Point(dx, dy);
    }

    public void Key(int key, bool down)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (down) CancellationToken.ThrowIfCancellationRequested();
        // The host may apply the input and then lose its acknowledgement.
        // Keep it pending until a successful release has been acknowledged.
        if (down) pressedKeys.Add(key);
        Host.SetKey(key, down);
        if (!down) pressedKeys.Remove(key);
    }

    public void Button(int button, bool down)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (down) CancellationToken.ThrowIfCancellationRequested();
        if (down) pressedButtons.Add(button);
        Host.SetPointerButton(button, down);
        if (!down) pressedButtons.Remove(button);
    }

    public void ReleaseInput()
    {
        List<Exception> errors = [];
        foreach (var key in pressedKeys.ToArray())
            try { Key(key, false); } catch (Exception e) { errors.Add(e); }
        foreach (var button in pressedButtons.ToArray())
            try { Button(button, false); } catch (Exception e) { errors.Add(e); }
        if (errors.Count > 0) throw new AggregateException("Game host could not release input", errors);
    }

    public void Scroll(int notches)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        CancellationToken.ThrowIfCancellationRequested();
        Host.Scroll(notches);
    }

    public void RequirePathing()
    {
        if (Pathing is null) throw new NotSupportedException("The official path executor is not bound to this game session");
    }

    public Task MoveToWaypoint(GameTask.AutoPathing.Model.Waypoint waypoint, string map, string method, CancellationToken token)
    {
        RequirePathing();
        return Pathing!(waypoint, map, method, token);
    }

    public void Dispose()
    {
        if (disposed) return;
        try { initialFrame?.Dispose(); ReleaseInput(); }
        finally { disposed = true; current.Value = previous; }
    }
}

public sealed record GameSystemInfo(int Width, int Height)
{
    public Rect CaptureAreaRect => new(0, 0, Width, Height);
    public Rect ScaleMax1080PCaptureRect => Width <= 1920 ? CaptureAreaRect : new(0, 0, 1920, (int)Math.Round(Height * 1920d / Width));
    public double ScaleTo1080PRatio => Width / 1920d;
    public double AssetScale => Math.Min(1, ScaleTo1080PRatio);
    public double ZoomOutMax1080PRatio => AssetScale;
}
