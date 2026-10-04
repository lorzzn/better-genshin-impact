using BetterGenshinImpact.Runtime;
using BetterGenshinImpact.View.Drawable;

namespace BetterGenshinImpact.GameTask;

public sealed class VisionContext
{
    private static readonly VisionContext instance = new();
    public static VisionContext Instance() => instance;
    public DrawContent DrawContent => GameSession.Current.Overlay;
}
