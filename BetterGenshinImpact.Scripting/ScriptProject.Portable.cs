using BetterGenshinImpact.Core.Config;

namespace BetterGenshinImpact.Core.Script.Project;

public partial class ScriptProject
{
    /// <summary>
    /// Script host for upstream callers such as scheduler items. The embedding
    /// application supplies it, as the desktop build supplies its own host.
    /// </summary>
    public static Func<PathingPartyConfig?, IScriptHost>? HostFactory { get; set; }

    public Task ExecuteAsync(dynamic? context = null, PathingPartyConfig? partyConfig = null)
    {
        var factory = HostFactory ?? throw new InvalidOperationException("嵌入程序未提供 JS 脚本宿主");
        return ExecuteWithHostAsync(factory(partyConfig), (object?)context, CancellationContext.Instance.Token, serializeResult: false);
    }
}
