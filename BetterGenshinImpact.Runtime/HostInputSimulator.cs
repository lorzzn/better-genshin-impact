using BetterGenshinImpact.GameTask.Common;
using Fischless.WindowsInput;
using Vanara.PInvoke;

namespace BetterGenshinImpact.Runtime;

/// <summary>Implements the official input contracts over the invocation's Target.</summary>
public sealed class HostInputSimulator : InputSimulator
{
    public HostInputSimulator() : base(new HostKeyboard(), new HostMouse(), new HostInputState()) { }
    public new HostKeyboard Keyboard => (HostKeyboard)base.Keyboard;
    public new HostMouse Mouse => (HostMouse)base.Mouse;
}

public sealed class HostKeyboard : IKeyboardSimulator
{
    public IMouseSimulator Mouse => Core.Simulator.Simulation.SendInput.Mouse;
    public IKeyboardSimulator KeyDown(User32.VK key) => KeyDown((int)key);
    public IKeyboardSimulator KeyDown(bool? extended, User32.VK key) => KeyDown(key);
    public IKeyboardSimulator KeyUp(User32.VK key) => KeyUp((int)key);
    public IKeyboardSimulator KeyUp(bool? extended, User32.VK key) => KeyUp(key);
    public IKeyboardSimulator KeyPress(User32.VK key) => KeyPress((int)key);
    public IKeyboardSimulator KeyPress(bool? extended, User32.VK key) => KeyPress(key);
    public IKeyboardSimulator KeyDown(int key) { GameSession.Current.Key(key, true); return this; }
    public IKeyboardSimulator KeyUp(int key) { GameSession.Current.Key(key, false); return this; }
    public IKeyboardSimulator KeyPress(int key)
    {
        KeyDown(key);
        try { TaskControl.Sleep(50); }
        finally { KeyUp(key); }
        return this;
    }
    public IKeyboardSimulator KeyPress(params User32.VK[] keys) { foreach (var key in keys) KeyPress(key); return this; }
    public IKeyboardSimulator KeyPress(bool? extended, params User32.VK[] keys) => KeyPress(keys);
    public IKeyboardSimulator ModifiedKeyStroke(IEnumerable<User32.VK> modifiers, IEnumerable<User32.VK> keys)
    {
        var held = new Stack<User32.VK>();
        try
        {
            foreach (var modifier in modifiers) { held.Push(modifier); KeyDown(modifier); }
            foreach (var key in keys) KeyPress(key);
        }
        finally { foreach (var modifier in held) KeyUp(modifier); }
        return this;
    }
    public IKeyboardSimulator ModifiedKeyStroke(IEnumerable<User32.VK> modifiers, User32.VK key) => ModifiedKeyStroke(modifiers, [key]);
    public IKeyboardSimulator ModifiedKeyStroke(User32.VK modifier, IEnumerable<User32.VK> keys) => ModifiedKeyStroke([modifier], keys);
    public IKeyboardSimulator ModifiedKeyStroke(User32.VK modifier, User32.VK key) => ModifiedKeyStroke([modifier], [key]);
    public IKeyboardSimulator TextEntry(string text) { GameSession.Current.InputText(text); return this; }
    public IKeyboardSimulator TextEntry(char value) => TextEntry(value.ToString());
    public IKeyboardSimulator Sleep(int milliseconds) { TaskControl.Sleep(milliseconds); return this; }
    public IKeyboardSimulator Sleep(TimeSpan timeout) => Sleep(checked((int)timeout.TotalMilliseconds));
}

public sealed class HostMouse : IMouseSimulator
{
    public IKeyboardSimulator Keyboard => Core.Simulator.Simulation.SendInput.Keyboard;
    public IMouseSimulator MoveMouseBy(int dx, int dy) { GameSession.Current.MoveBy(dx, dy); return this; }
    // InputSimulator coordinates are in 0..65535 over the bound Target surface.
    public IMouseSimulator MoveMouseTo(double x, double y)
    {
        var session = GameSession.Current;
        session.Move(x * session.SystemInfo.Width / 65535d, y * session.SystemInfo.Height / 65535d);
        return this;
    }
    public IMouseSimulator MoveMouseToPositionOnVirtualDesktop(double x, double y) => MoveMouseTo(x, y);
    public IMouseSimulator VerticalScroll(int notches) { GameSession.Current.Scroll(notches); return this; }
    public IMouseSimulator HorizontalScroll(int notches) { GameSession.Current.Host.ScrollHorizontal(notches); return this; }
    public IMouseSimulator Sleep(int milliseconds) { TaskControl.Sleep(milliseconds); return this; }
    public IMouseSimulator Sleep(TimeSpan timeout) => Sleep(checked((int)timeout.TotalMilliseconds));
    public IMouseSimulator LeftButtonDown() => Button(0, true);
    public IMouseSimulator LeftButtonUp() => Button(0, false);
    public IMouseSimulator RightButtonDown() => Button(1, true);
    public IMouseSimulator RightButtonUp() => Button(1, false);
    public IMouseSimulator MiddleButtonDown() => Button(2, true);
    public IMouseSimulator MiddleButtonUp() => Button(2, false);
    public IMouseSimulator XButtonDown(int button) => Button(XButton(button), true);
    public IMouseSimulator XButtonUp(int button) => Button(XButton(button), false);
    public IMouseSimulator LeftButtonClick() => Click(0);
    public IMouseSimulator RightButtonClick() => Click(1);
    public IMouseSimulator MiddleButtonClick() => Click(2);
    public IMouseSimulator XButtonClick(int button) => Click(XButton(button));
    public IMouseSimulator LeftButtonDoubleClick() => DoubleClick(0);
    public IMouseSimulator RightButtonDoubleClick() => DoubleClick(1);
    public IMouseSimulator MiddleButtonDoubleClick() => DoubleClick(2);
    public IMouseSimulator XButtonDoubleClick(int button) => DoubleClick(XButton(button));
    private static int XButton(int button) => button is 1 or 2 ? button + 2 : throw new ArgumentOutOfRangeException(nameof(button));
    private IMouseSimulator Button(int button, bool down) { GameSession.Current.Button(button, down); return this; }
    private IMouseSimulator Click(int button)
    {
        Button(button, true);
        try { TaskControl.Sleep(50); }
        finally { Button(button, false); }
        return this;
    }
    private IMouseSimulator DoubleClick(int button) { Click(button); TaskControl.Sleep(50); return Click(button); }
}

internal sealed class HostInputState : IInputDeviceStateAdaptor
{
    public bool IsKeyDown(User32.VK key) => GameSession.Current.IsKeyDown((int)key);
    public bool IsKeyUp(User32.VK key) => !IsKeyDown(key);
    public bool IsHardwareKeyDown(User32.VK key) => GameSession.Current.Host.IsHardwareKeyDown((int)key);
    public bool IsHardwareKeyUp(User32.VK key) => !IsHardwareKeyDown(key);
    public bool IsTogglingKeyInEffect(User32.VK key) => GameSession.Current.Host.IsTogglingKeyInEffect((int)key);
}
