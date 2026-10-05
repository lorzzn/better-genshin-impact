using BetterGenshinImpact.GameTask.Common;
using BetterGenshinImpact.Runtime;
using Vanara.PInvoke;

namespace BetterGenshinImpact.Core.Simulator;

/// <summary>Background input is transported to the same Target, never a local HWND.</summary>
public sealed class PostMessageSimulator
{
    public PostMessageSimulator LeftButtonClick(int x, int y) { GameSession.Current.Move(x, y); return LeftButtonClick(); }
    public PostMessageSimulator LeftButtonClickBackground(int x, int y) => LeftButtonClick(x, y);
    public PostMessageSimulator LeftButtonClick() { Simulation.SendInput.Mouse.LeftButtonClick(); return this; }
    public PostMessageSimulator LeftButtonClickBackground() => LeftButtonClick();
    public PostMessageSimulator LeftButtonDown() { Simulation.SendInput.Mouse.LeftButtonDown(); return this; }
    public PostMessageSimulator LeftButtonUp() { Simulation.SendInput.Mouse.LeftButtonUp(); return this; }
    public PostMessageSimulator RightButtonClick() { Simulation.SendInput.Mouse.RightButtonClick(); return this; }
    public PostMessageSimulator RightButtonDown() { Simulation.SendInput.Mouse.RightButtonDown(); return this; }
    public PostMessageSimulator RightButtonUp() { Simulation.SendInput.Mouse.RightButtonUp(); return this; }
    public PostMessageSimulator KeyPress(User32.VK key) { Simulation.SendInput.Keyboard.KeyPress(key); return this; }
    public PostMessageSimulator KeyPress(User32.VK key, int milliseconds)
    {
        KeyDown(key);
        try { TaskControl.Sleep(milliseconds); }
        finally { KeyUp(key); }
        return this;
    }
    public PostMessageSimulator LongKeyPress(User32.VK key) => KeyPress(key, 1000);
    public PostMessageSimulator KeyDown(User32.VK key) { Simulation.SendInput.Keyboard.KeyDown(key); return this; }
    public PostMessageSimulator KeyUp(User32.VK key) { Simulation.SendInput.Keyboard.KeyUp(key); return this; }
    public PostMessageSimulator KeyPressBackground(User32.VK key) => KeyPress(key);
    public PostMessageSimulator KeyDownBackground(User32.VK key) => KeyDown(key);
    public PostMessageSimulator KeyUpBackground(User32.VK key) => KeyUp(key);
    public PostMessageSimulator Sleep(int milliseconds) { TaskControl.Sleep(milliseconds); return this; }
}
