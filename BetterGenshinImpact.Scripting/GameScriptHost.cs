using BetterGenshinImpact.Core.Script;
using BetterGenshinImpact.Runtime;
using Microsoft.ClearScript;

namespace BetterGenshinImpact.Scripting;

/// <summary>Registers the shared official host objects against the caller's game session.</summary>
public sealed class GameScriptHost : IScriptHost
{
    public void Configure(IScriptEngine engine, string projectPath, string[] searchPaths)
    {
        _ = GameSession.Current;
        EngineExtend.InitCoreHost(engine, projectPath, searchPaths);
    }
}
