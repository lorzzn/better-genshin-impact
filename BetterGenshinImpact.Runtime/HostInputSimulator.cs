using BetterGenshinImpact.GameTask.Common;

namespace BetterGenshinImpact.Runtime;

/// <summary>Input transport for the shared task sources, exclusively to their bound host.</summary>
public sealed class HostInputSimulator
{
    public HostKeyboard Keyboard { get; } = new();
    public HostMouse Mouse { get; } = new();
}

public sealed class HostKeyboard
{
    public void KeyDown(int key) => GameSession.Current.Key(key, true);
    public void KeyUp(int key) => GameSession.Current.Key(key, false);
    public void KeyPress(int key)
    {
        KeyDown(key);
        try { TaskControl.Sleep(50); }
        finally { KeyUp(key); }
    }
}

public sealed class HostMouse
{
    public HostMouse MoveMouseBy(int dx, int dy) { GameSession.Current.MoveBy(dx, dy); return this; }
    // InputSimulator coordinates are in 0..65535 over the target surface.
    public HostMouse MoveMouseTo(double x, double y)
    {
        var session = GameSession.Current;
        session.Move(x * session.SystemInfo.Width / 65535d, y * session.SystemInfo.Height / 65535d);
        return this;
    }
    public void VerticalScroll(int notches) => GameSession.Current.Scroll(notches);
    public void LeftButtonDown() => GameSession.Current.Button(0, true);
    public void LeftButtonUp() => GameSession.Current.Button(0, false);
    public void RightButtonDown() => GameSession.Current.Button(1, true);
    public void RightButtonUp() => GameSession.Current.Button(1, false);
    public void MiddleButtonDown() => GameSession.Current.Button(2, true);
    public void MiddleButtonUp() => GameSession.Current.Button(2, false);
    public void XButtonDown(int button) => GameSession.Current.Button(button == 1 ? 3 : 4, true);
    public void XButtonUp(int button) => GameSession.Current.Button(button == 1 ? 3 : 4, false);
    public void LeftButtonClick() => Click(0);
    public void RightButtonClick() => Click(1);
    public void MiddleButtonClick() => Click(2);
    public void XButtonClick(int button) => Click(button == 1 ? 3 : 4);
    private static void Click(int button)
    {
        var session = GameSession.Current;
        session.Button(button, true);
        try { TaskControl.Sleep(50); }
        finally { session.Button(button, false); }
    }
}
