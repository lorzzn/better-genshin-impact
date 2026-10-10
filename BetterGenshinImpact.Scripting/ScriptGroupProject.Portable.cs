namespace BetterGenshinImpact.Core.Script.Group;

public partial class ScriptGroupProject
{
    // Shell items run commands on the executing machine. Embedding hosts run
    // scripts on a shared server, so they never execute this item type.
    private Task RunShell() => throw new NotSupportedException("Shell 条目不在此运行环境中执行");
}
