using System;
using System.Threading;
using System.Threading.Tasks;

namespace BetterGenshinImpact.Core.Script;

public sealed record ScriptPhase(string Id, string? ParentId, string Operation, string Status, string? Message);

/// <summary>Optional progress observation at official script entry boundaries.</summary>
public static class ScriptOperation
{
    private static readonly AsyncLocal<Action<ScriptPhase>?> observer = new();
    private static readonly AsyncLocal<string?> current = new();

    public static IDisposable Observe(Action<ScriptPhase> callback)
    {
        var previous = observer.Value;
        observer.Value = callback;
        return new Observation(() => observer.Value = previous);
    }

    public static async Task RunAsync(string name, Func<Task> run)
        => await RunAsync(name, async () => { await run(); return 0; });

    public static async Task<T> RunAsync<T>(string name, Func<Task<T>> run)
    {
        var callback = observer.Value;
        if (callback is null) return await run();
        var parent = current.Value;
        var id = Guid.NewGuid().ToString();
        current.Value = id;
        try
        {
            callback(new(id, parent, name, "started", null));
            var result = await run();
            callback(new(id, parent, name, "completed", null));
            return result;
        }
        catch (Exception error)
        {
            callback(new(id, parent, name, "failed", error.Message));
            throw;
        }
        finally { current.Value = parent; }
    }

    public static T Run<T>(string name, Func<T> run)
    {
        var callback = observer.Value;
        if (callback is null) return run();
        var parent = current.Value;
        var id = Guid.NewGuid().ToString();
        current.Value = id;
        try
        {
            callback(new(id, parent, name, "started", null));
            var result = run();
            callback(new(id, parent, name, "completed", null));
            return result;
        }
        catch (Exception error)
        {
            callback(new(id, parent, name, "failed", error.Message));
            throw;
        }
        finally { current.Value = parent; }
    }

    private sealed class Observation(Action restore) : IDisposable
    {
        public void Dispose() => restore();
    }
}
