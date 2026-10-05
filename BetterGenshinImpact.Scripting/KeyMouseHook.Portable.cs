using BetterGenshinImpact.Scripting;

namespace BetterGenshinImpact.Core.Script.Dependence;

public partial class KeyMouseHook
{
    public KeyMouseHook() : this(ScriptInputScope.Current.Source)
    {
        ScriptInputScope.Current.Register(this);
    }
}
