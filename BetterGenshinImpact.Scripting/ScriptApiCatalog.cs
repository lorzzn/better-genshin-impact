using System.Reflection;
using BetterGenshinImpact.Core.BgiVision;
using BetterGenshinImpact.Core.Script.Dependence;

namespace BetterGenshinImpact.Scripting;

/// <summary>Compiled host members, for editors; this is not a game acceptance result.</summary>
public static class ScriptApiCatalog
{
    public static string[] Members()
    {
        var objects = new Dictionary<string, Type>
        {
            ["file"] = typeof(LimitedFile), ["genshin"] = typeof(Genshin),
            ["dispatcher"] = typeof(Dispatcher), ["pathingScript"] = typeof(AutoPathingScript),
            ["keyMouseScript"] = typeof(KeyMouseScript), ["strategyFile"] = typeof(StrategyFile),
            ["notification"] = typeof(Notification), ["log"] = typeof(Log),
            ["characterDevelopmentTask"] = typeof(BetterGenshinImpact.GameTask.CharacterDevelopment.CharacterDevelopmentTask),
            ["BvPage"] = typeof(BvPage), ["BvLocator"] = typeof(BvLocator), ["BvImage"] = typeof(BvImage),
        };
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (name, type) in objects)
        {
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            {
                if (method.IsSpecialName || method.DeclaringType == typeof(object)) continue;
                var camel = char.ToLowerInvariant(method.Name[0]) + method.Name[1..];
                result.Add(name + "." + camel);
                result.Add(name + "." + method.Name);
            }
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            {
                result.Add(name + "." + property.Name);
                result.Add(name + "." + char.ToLowerInvariant(property.Name[0]) + property.Name[1..]);
            }
        }
        return result.Order(StringComparer.Ordinal).ToArray();
    }
}
