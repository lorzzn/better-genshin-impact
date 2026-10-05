using BetterGenshinImpact.Runtime;
using Microsoft.Extensions.Logging;

namespace BetterGenshinImpact.GameTask;

public sealed class TaskTriggerDispatcher : IDisposable
{
    private readonly object gate = new();
    private readonly ScriptTriggerCollection collection = new();
    private readonly TriggerProcessor processor = new();
    private readonly GameSession session = GameSession.Current;
    private List<ITaskTrigger> triggers = [];
    private Task? loop;
    private Exception? failure;
    private bool disposed;
    public static TaskTriggerDispatcher Instance() => GameSession.Current.Triggers;

    public bool AddTrigger(string name, object? config)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            ThrowIfFailed();
            if (!collection.AddTrigger(name, config)) return false;
            triggers = collection.ConvertToTriggerList(allEnabled: true);
            loop ??= Task.Run(RunAsync);
            return true;
        }
    }

    public void ClearTriggers()
    {
        lock (gate) { collection.ClearTriggers(); triggers.Clear(); }
    }

    public void ThrowIfFailed()
    {
        if (failure is { } error) throw new InvalidOperationException("BetterGI 实时任务执行失败", error);
    }

    private async Task RunAsync()
    {
        var token = session.CancellationToken;
        var interval = Math.Max(1, session.Config.TriggerInterval);
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(interval));
        var frameIndex = 0;
        try
        {
            while (await timer.WaitForNextTickAsync(token))
            {
                lock (gate)
                {
                    if (disposed || triggers.Count == 0 || session.Runner.IsSuspend) continue;
                    var background = !session.Host.IsForeground;
                    if (background && !triggers.Any(t => t.IsEnabled && t.IsBackgroundRunning)) continue;
                    // CaptureContent owns this raw image and performs the same 1080p transform as desktop.
                    using var content = new CaptureContent(session.Host.Capture(token), frameIndex, interval);
                    processor.Process(content, triggers, background, trigger => trigger.OnCapture(content));
                    frameIndex = (frameIndex + 1) % Math.Max(1, CaptureContent.MaxFrameIndexSecond * 1000 / interval);
                }
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception error)
        {
            failure = error;
            RuntimeEnvironment.Logger.LogError(error, "BetterGI 实时任务已停止");
            session.Lifetime.Cancel();
        }
    }

    public void Dispose()
    {
        lock (gate) { if (disposed) return; disposed = true; }
        session.Lifetime.Cancel();
        loop?.GetAwaiter().GetResult();
        ClearTriggers();
        ThrowIfFailed();
    }
}
