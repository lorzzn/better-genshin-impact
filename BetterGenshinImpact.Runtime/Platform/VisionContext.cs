using BetterGenshinImpact.Runtime;
using BetterGenshinImpact.View.Drawable;

namespace BetterGenshinImpact.View.Drawable;

public sealed class VisionContext
{
    private static readonly VisionContext instance = new();
    public static VisionContext Instance() => instance;
    public DrawContent DrawContent => GameSession.Current.Overlay;
}
