using System;
using BetterGenshinImpact.Core.Monitor;
using BetterGenshinImpact.GameTask;
using Gma.System.MouseKeyHook;
using Microsoft.Extensions.Logging;

namespace BetterGenshinImpact.Core.Script.Dependence;

public partial class KeyMouseHook
{
    public KeyMouseHook() : this(new DesktopInput(), ownsSource: true) { }

    private sealed class DesktopInput : IScriptInputSource
    {
        private IKeyboardMouseEvents? events;
        public event EventHandler<ScriptKeyEvent>? KeyDown;
        public event EventHandler<ScriptKeyEvent>? KeyUp;
        public event EventHandler<ScriptMouseEvent>? MouseDownExt;
        public event EventHandler<ScriptMouseEvent>? MouseUpExt;
        public event EventHandler<ScriptMouseEvent>? MouseMoveExt;
        public event EventHandler<ScriptMouseEvent>? MouseWheelExt;

        public void Start()
        {
            if (events != null) return;
            events = MouseKeyMonitor.GlobalHook;
            events.KeyDown += Down;
            events.KeyUp += Up;
            events.MouseDownExt += MouseDown;
            events.MouseUpExt += MouseUp;
            events.MouseMoveExt += Move;
            events.MouseWheelExt += Wheel;
        }

        private void Down(object? sender, System.Windows.Forms.KeyEventArgs args) =>
            KeyDown?.Invoke(this, new(args.KeyCode.ToString(), args.KeyData.ToString()));
        private void Up(object? sender, System.Windows.Forms.KeyEventArgs args) =>
            KeyUp?.Invoke(this, new(args.KeyCode.ToString(), args.KeyData.ToString()));
        private void MouseDown(object? sender, MouseEventExtArgs args) => MouseDownExt?.Invoke(this, Local(args));
        private void MouseUp(object? sender, MouseEventExtArgs args) => MouseUpExt?.Invoke(this, Local(args));
        private void Move(object? sender, MouseEventExtArgs args) => MouseMoveExt?.Invoke(this, Local(args));
        private void Wheel(object? sender, MouseEventExtArgs args) => MouseWheelExt?.Invoke(this, Local(args));

        private static ScriptMouseEvent Local(MouseEventExtArgs args)
        {
            try
            {
                var context = TaskContext.Instance();
                if (!context.IsInitialized || context.GameHandle == IntPtr.Zero)
                    throw new InvalidOperationException("游戏窗口尚未初始化");
                var captureRect = SystemControl.GetCaptureRect(context.GameHandle);
                if (captureRect.Width <= 0 || captureRect.Height <= 0)
                    throw new InvalidOperationException("游戏窗口捕获区域无效");
                return new(args.Button.ToString(), args.X - captureRect.Left, args.Y - captureRect.Top, args.Delta);
            }
            catch (Exception error)
            {
                GameServices.GetLogger<KeyMouseHook>().LogError(error, "转换鼠标坐标时发生错误");
                return new(args.Button.ToString(), -1, -1, args.Delta);
            }
        }

        public void Dispose()
        {
            if (events == null) return;
            events.KeyDown -= Down;
            events.KeyUp -= Up;
            events.MouseDownExt -= MouseDown;
            events.MouseUpExt -= MouseUp;
            events.MouseMoveExt -= Move;
            events.MouseWheelExt -= Wheel;
            events = null;
        }
    }
}
