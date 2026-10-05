using System.Collections.ObjectModel;
using System.Linq;
using BetterGenshinImpact.GameTask.AutoFight.Model;

namespace BetterGenshinImpact.Core.Script.Dependence;

public partial class GlobalMethod
{
    public static string[] GetAvatars()
    {
        var combatScenes = new CombatScenes().InitializeTeam(CaptureGameRegion());
        ReadOnlyCollection<Avatar> avatars = combatScenes.GetAvatars();
        return avatars.Count > 0
            ? avatars.Select(avatar => avatar.Name).ToArray()
            : [];
    }
}
