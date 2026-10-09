using System;

namespace BetterGenshinImpact.Core.Script;

/// <summary>Input observed on the bound game surface. Coordinates are capture pixels.</summary>
public interface IScriptInputSource : IDisposable
{
    event EventHandler<ScriptKeyEvent>? KeyDown;
    event EventHandler<ScriptKeyEvent>? KeyUp;
    event EventHandler<ScriptMouseEvent>? MouseDownExt;
    event EventHandler<ScriptMouseEvent>? MouseUpExt;
    event EventHandler<ScriptMouseEvent>? MouseMoveExt;
    event EventHandler<ScriptMouseEvent>? MouseWheelExt;
    void Start();
}

/// <summary>
/// Optional for sources that observe input on another computer. Each hook reports
/// which input kinds currently have script callbacks, so the source starts only
/// those; (hook, false, false) withdraws that hook's demand.
/// </summary>
public interface IScriptInputDemand
{
    void Demand(object hook, bool keyboard, bool mouse);
}

public sealed class ScriptKeyEvent(string keyCode, string keyData) : EventArgs
{
    public string KeyCode { get; } = keyCode;
    public string KeyData { get; } = keyData;
}

public sealed class ScriptMouseEvent(string button, int x, int y, int delta = 0) : EventArgs
{
    public string Button { get; } = button;
    public int X { get; } = x;
    public int Y { get; } = y;
    public int Delta { get; } = delta;
}
