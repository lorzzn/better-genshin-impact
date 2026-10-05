using BetterGenshinImpact.Core.Simulator;
using Fischless.WindowsInput;
using OpenCvSharp;

namespace BetterGenshinImpact.Helpers.Extensions;

public static class ClickExtension
{
    private static System.Drawing.Size InputSize =>
#if BETTERGI_PORTABLE
        new(Runtime.GameSession.Current.SystemInfo.Width, Runtime.GameSession.Current.SystemInfo.Height);
#else
        PrimaryScreen.WorkingArea;
#endif

    public static void Click(this Point point)
    {
        Simulation.SendInput.Mouse.MoveMouseTo(point.X * 65535 * 1d / InputSize.Width,
            point.Y * 65535 * 1d / InputSize.Height).LeftButtonDown().Sleep(50).LeftButtonUp();
    }

    // public static void ClickCenter(this Rect rect, bool isRand = false)
    // {
    //     Simulation.SendInputEx.Mouse.MoveMouseTo((rect.X + (isRand ? Rd.Next(rect.Width) : rect.Width * 1d / 2)) * 65535 / InputSize.Width,
    //         (rect.Y + (isRand ? Rd.Next(rect.Height) : rect.Height * 1d / 2)) * 65535 / InputSize.Height).LeftButtonDown().Sleep(50).LeftButtonUp();
    // }

    public static IMouseSimulator Click(double x, double y)
    {
        return Simulation.SendInput.Mouse.MoveMouseTo(x * 65535 * 1d / InputSize.Width,
            y * 65535 * 1d / InputSize.Height).LeftButtonDown().Sleep(50).LeftButtonUp();
    }

    public static IMouseSimulator Move(double x, double y)
    {
        return Simulation.SendInput.Mouse.MoveMouseTo(x * 65535 * 1d / InputSize.Width,
            y * 65535 * 1d / InputSize.Height);
    }

    public static IMouseSimulator Move(Point p)
    {
        return Move(p.X, p.Y);
    }
}
