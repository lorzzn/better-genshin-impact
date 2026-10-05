using BetterGenshinImpact.GameTask.Model;
using OpenCvSharp;

namespace BetterGenshinImpact.Runtime;

/// <summary>Shares each captured Target frame between parallel pixel readers.</summary>
internal sealed class TargetPixelSource : IGamePixelSource
{
    private readonly object sync = new();
    private readonly GameSession session = GameSession.Current;
    private readonly CancellationTokenSource stopping;
    private readonly Task reader;
    private Mat? frame;
    private Exception? failure;
    private bool disposed;

    public TargetPixelSource(CancellationToken cancellationToken)
    {
        stopping = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, session.CancellationToken);
        try { frame = Capture(); }
        catch { stopping.Dispose(); throw; }
        reader = Task.Run(ReadFrames);
    }

    private Mat Capture()
    {
        var captured = session.Host.Capture(stopping.Token);
        if (captured.Empty() || captured.Type() != MatType.CV_8UC3)
        {
            captured.Dispose();
            throw new InvalidDataException("Target 像素采样需要 BGR 画面");
        }
        if (captured.Width != session.SystemInfo.Width || captured.Height != session.SystemInfo.Height)
        {
            captured.Dispose();
            throw new InvalidOperationException("Target 画面尺寸在任务期间发生变化，请重新执行任务");
        }
        return captured;
    }

    private void ReadFrames()
    {
        try
        {
            while (!stopping.IsCancellationRequested)
            {
                var next = Capture();
                lock (sync) { frame?.Dispose(); frame = next; }
            }
        }
        catch (OperationCanceledException) when (stopping.IsCancellationRequested) { }
        catch (Exception error) { lock (sync) failure = error; }
    }

    public Vec3b GetPixel(int x, int y)
    {
        stopping.Token.ThrowIfCancellationRequested();
        lock (sync)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (failure != null) throw new IOException("Target 像素画面读取失败", failure);
            if (frame == null || x < 0 || y < 0 || x >= frame.Width || y >= frame.Height)
            {
                // Fail every parallel reader instead of leaving sibling key loops running.
                failure = new ArgumentOutOfRangeException(nameof(x), "采样点超出 Target 画面");
                throw failure;
            }
            return frame.At<Vec3b>(y, x);
        }
    }

    public void Dispose()
    {
        lock (sync)
        {
            if (disposed) return;
            disposed = true;
        }
        stopping.Cancel();
        try { reader.GetAwaiter().GetResult(); }
        finally
        {
            lock (sync) { frame?.Dispose(); frame = null; }
            stopping.Dispose();
        }
    }
}
