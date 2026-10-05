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
    private readonly object inputSync = new();
    internal CancellationTokenSource Lifetime { get; }
    public GameTask.RunnerContext Runner { get; } = new();
    public GameTask.TaskTriggerDispatcher Triggers { get; }
    private readonly HashSet<int> pressedKeys = [];
    private readonly HashSet<int> pressedButtons = [];
    private bool disposed;
    public static bool IsBound => current.Value != null;
    public static GameSession Current => current.Value ?? throw new InvalidOperationException("No game host is bound to this invocation");
    public IGameHost Host { get; }
    public CancellationToken CancellationToken { get; }
    public GameSystemInfo SystemInfo { get; private set; }
    public DrawContent Overlay { get; } = new();
    public GameConfiguration Config { get; }
    public Point PointerPosition { get; private set; }

    public GameSession(IGameHost host, CancellationToken cancellationToken = default, GameConfiguration? configuration = null)
    {
        ArgumentNullException.ThrowIfNull(host);
        Host = host;
        Lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        CancellationToken = Lifetime.Token;
        Config = configuration ?? new();
        try
        {
            initialFrame = host.Capture(CancellationToken);
            if (initialFrame.Empty()) throw new InvalidDataException("Game host returned an empty frame");
        }
        catch { initialFrame?.Dispose(); Lifetime.Dispose(); throw; }
        SystemInfo = new(initialFrame.Width, initialFrame.Height);
        previous = current.Value;
        current.Value = this;
        Triggers = new();
    }

    public ImageRegion Capture()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        CancellationToken.ThrowIfCancellationRequested();
        var frame = Interlocked.Exchange(ref initialFrame, null) ?? Host.Capture(CancellationToken);
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
        lock (inputSync)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            CancellationToken.ThrowIfCancellationRequested();
            Host.MovePointer(x, y);
            PointerPosition = new Point((int)Math.Round(x), (int)Math.Round(y));
        }
    }

    public void MoveBy(int dx, int dy)
    {
        lock (inputSync)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            CancellationToken.ThrowIfCancellationRequested();
            Host.MovePointerBy(dx, dy);
            PointerPosition += new Point(dx, dy);
        }
    }

    public void InputText(string text)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        CancellationToken.ThrowIfCancellationRequested();
        Host.InputText(text);
    }

    public void Key(int key, bool down)
    {
        lock (inputSync)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (down) CancellationToken.ThrowIfCancellationRequested();
            // The host may apply the input and then lose its acknowledgement.
            // Keep it pending until a successful release has been acknowledged.
            if (down) pressedKeys.Add(key);
            Host.SetKey(key, down);
            if (!down) pressedKeys.Remove(key);
        }
    }

    public void Button(int button, bool down)
    {
        lock (inputSync)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (down) CancellationToken.ThrowIfCancellationRequested();
            if (down) pressedButtons.Add(button);
            Host.SetPointerButton(button, down);
            if (!down) pressedButtons.Remove(button);
        }
    }

    public bool IsKeyDown(int key) { lock (inputSync) return pressedKeys.Contains(key); }

    public void ReleaseInput()
    {
        lock (inputSync)
        {
            List<Exception> errors = [];
            foreach (var key in pressedKeys.ToArray())
                try { Key(key, false); } catch (Exception e) { errors.Add(e); }
            foreach (var button in pressedButtons.ToArray())
                try { Button(button, false); } catch (Exception e) { errors.Add(e); }
            if (errors.Count > 0) throw new AggregateException("Game host could not release input", errors);
        }
    }

    public void Scroll(int notches)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        CancellationToken.ThrowIfCancellationRequested();
        Host.Scroll(notches);
    }

    public void Dispose()
    {
        if (disposed) return;
        try
        {
            Lifetime.Cancel();
            Triggers.Dispose();
        }
        finally
        {
            try { Interlocked.Exchange(ref initialFrame, null)?.Dispose(); ReleaseInput(); }
            finally { Runner.Reset(); disposed = true; current.Value = previous; Lifetime.Dispose(); }
        }
    }
}

public sealed record GameSystemInfo(int Width, int Height) : GameTask.Model.IRecognitionSurface
{
    public DesktopRegion DesktopRectArea => new(Width, Height);
    public Rect CaptureAreaRect => new(0, 0, Width, Height);
    public Rect ScaleMax1080PCaptureRect => Width <= 1920 ? CaptureAreaRect : new(0, 0, 1920, (int)Math.Round(Height * 1920d / Width));
    public double ScaleTo1080PRatio => Width / 1920d;
    public double AssetScale => Math.Min(1, ScaleTo1080PRatio);
    public double ZoomOutMax1080PRatio => AssetScale;
}
