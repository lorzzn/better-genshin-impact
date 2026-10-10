using System.Threading.Tasks;
using BetterGenshinImpact.Core.Config;
using BetterGenshinImpact.GameTask.Shell;

namespace BetterGenshinImpact.Core.Script.Group;

public partial class ScriptGroupProject
{
    private async Task RunShell()
    {
        ShellConfig? shellConfig = null;
        if (GroupInfo?.Config.EnableShellConfig ?? false)
        {
            shellConfig = GroupInfo?.Config.ShellConfig;
        }

        var task = new ShellTask(ShellTaskParam.BuildFromConfig(Name, shellConfig ?? new ShellConfig()));
        await task.Start(CancellationContext.Instance.Cts.Token);
    }
}
