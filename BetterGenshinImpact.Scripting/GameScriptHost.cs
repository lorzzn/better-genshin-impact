using BetterGenshinImpact.Core.Script;
using BetterGenshinImpact.Core.Script.Dependence;
using BetterGenshinImpact.Runtime;
using Microsoft.ClearScript;

namespace BetterGenshinImpact.Scripting;

/// <summary>Registers the official task and script objects on the bound game session.</summary>
public sealed class GameScriptHost(IScriptFileSystem? fileSystem = null, IScriptTaskHost? externalTasks = null) : IScriptHost
{
    public void Configure(IScriptEngine engine, string projectPath, string[] searchPaths)
    {
        var config = GameSession.Current.Config;
        EngineExtend.InitCoreHost(engine, projectPath, searchPaths, fileSystem);
        var dispatcher = CreateDispatcher(externalTasks);
        EngineExtend.InitGameHost(engine, projectPath, dispatcher, config.PathingPartyConfig, fileSystem);
    }

    public static Dispatcher CreateDispatcher(IScriptTaskHost? externalTasks = null)
    {
        var config = GameSession.Current.Config;
        return new Dispatcher(config.PathingPartyConfig, new ConfiguredTaskDefaults(config), externalTasks);
    }

    private sealed class ConfiguredTaskDefaults(GameConfiguration config) : IScriptTaskDefaults
    {
        public int AutoWoodRoundNum => config.AutoWoodRoundNum;
        public int AutoWoodDailyMaxCount => config.AutoWoodDailyMaxCount;
        public bool GetTcgStrategy(out string content) => TaskStrategyResolver.GetTcgStrategy(
            config.AutoGeniusInvokationConfig.StrategyName, out content, RuntimeUi.Warning, message => RuntimeUi.Error(message));
        public bool GetFightStrategy(out string path) => GetFightStrategy(config.AutoFightConfig.StrategyName, out path);
        public bool GetFightStrategy(string name, out string path) => TaskStrategyResolver.GetFightStrategy(
            name, out path, RuntimeUi.Warning, message => RuntimeUi.Error(message));
    }
}
