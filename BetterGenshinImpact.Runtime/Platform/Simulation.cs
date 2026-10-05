using BetterGenshinImpact.Core.Config;
using BetterGenshinImpact.Runtime;

namespace BetterGenshinImpact.Core.Simulator;

public static class Simulation
{
    public static HostInputSimulator SendInput { get; } = new();
    public static void KeyPress(KeyId key) => SendInput.Keyboard.KeyPress((int)key);
    public static bool IsKeyDown(Vanara.PInvoke.User32.VK key) => GameSession.Current.IsKeyDown((int)key);
    public static bool IsKeyDown(KeyId key) => GameSession.Current.IsKeyDown((int)key);
    public static void ReleaseAllKey() => GameSession.Current.ReleaseInput();
}
