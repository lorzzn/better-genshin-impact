using Microsoft.ClearScript;

namespace BetterGenshinImpact.Core.Script;

/// <summary>
/// Registers the objects available to a script. The desktop and an embedding
/// application provide their own dependency boundary; execution stays shared.
/// </summary>
public interface IScriptHost
{
    void Configure(IScriptEngine engine, string projectPath, string[] searchPaths);
}

public interface IScriptHostLifetime
{
    void OnScriptEnding();
    void OnScriptEnded() { }
}
