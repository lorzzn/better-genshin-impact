using BetterGenshinImpact.Core.Recorder;
using System.Threading.Tasks;

namespace BetterGenshinImpact.Core.Script.Dependence;

public class KeyMouseScript(string rootPath, IScriptFileSystem? fileSystem = null)
{
    public async Task Run(string json)
    {
        await ScriptOperation.RunAsync("game.playMacro", () => KeyMouseMacroPlayer.PlayMacro(json, CancellationContext.Instance.Token, false));
    }

    public async Task RunFile(string path)
    {
        var json = await new LimitedFile(rootPath, fileSystem).ReadText(path);
        await Run(json);
    }
}
