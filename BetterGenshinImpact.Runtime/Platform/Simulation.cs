using BetterGenshinImpact.Core.Config;
using BetterGenshinImpact.Runtime;

namespace BetterGenshinImpact.Core.Simulator;

public static class Simulation
{
    public static HostInputSimulator SendInput { get; } = new();
    public static void KeyPress(KeyId key) => SendInput.Keyboard.KeyPress((int)key);
    public static void ReleaseAllKey() => GameSession.Current.ReleaseInput();
}
