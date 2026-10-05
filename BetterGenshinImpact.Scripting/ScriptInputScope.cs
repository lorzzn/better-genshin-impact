using BetterGenshinImpact.Core.Script;
using BetterGenshinImpact.Core.Script.Dependence;

namespace BetterGenshinImpact.Scripting;

/// <summary>Owns all hooks constructed by one script, including forgotten Dispose calls.</summary>
public sealed class ScriptInputScope : IDisposable
{
    private static readonly AsyncLocal<ScriptInputScope?> current = new();
    private readonly ScriptInputScope? previous = current.Value;
    private readonly object sync = new();
    private readonly List<KeyMouseHook> hooks = [];
    private bool stopping;
    internal static ScriptInputScope Current => current.Value ?? throw new InvalidOperationException("脚本 Target 输入未绑定");
    internal IScriptInputSource Source { get; }

    public ScriptInputScope(IScriptInputSource source)
    {
        Source = source;
        current.Value = this;
    }

    internal void Register(KeyMouseHook hook)
    {
        lock (sync)
        {
            if (stopping) { hook.Dispose(); throw new ObjectDisposedException(nameof(ScriptInputScope)); }
            hooks.Add(hook);
        }
    }

    public void Dispose()
    {
        lock (sync)
        {
            if (stopping) return;
            stopping = true;
            foreach (var hook in hooks) hook.Dispose();
        }
        Source.Dispose();
        current.Value = previous;
    }

    // Call after interrupting V8, before disposing it. A callback may have been
    // waiting for the engine when the main script ended.
    public void DrainCallbacks()
    {
        Task[] pending;
        lock (sync) pending = hooks.Select(hook => hook.Completion).ToArray();
        Task.WhenAll(pending).GetAwaiter().GetResult();
    }
}
