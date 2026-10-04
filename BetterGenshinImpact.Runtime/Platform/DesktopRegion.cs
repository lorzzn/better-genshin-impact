using BetterGenshinImpact.GameTask.Model.Area.Converter;
using BetterGenshinImpact.Runtime;
using OpenCvSharp;

namespace BetterGenshinImpact.GameTask.Model.Area;

public sealed class DesktopRegion(int width, int height) : Region(0, 0, width, height)
{
    public void DesktopRegionClick(int x, int y, int w, int h) => DesktopRegionClick(x + w / 2d, y + h / 2d);
    public void DesktopRegionMove(int x, int y, int w, int h) => DesktopRegionMove(x + w / 2d, y + h / 2d);
    public static void DesktopRegionClick(double x, double y)
    {
        var session = GameSession.Current;
        session.Move(x, y);
        session.Button(0, true);
        try { Common.TaskControl.Sleep(50); }
        finally { session.Button(0, false); }
        Common.TaskControl.Sleep(50);
    }
    public static void DesktopRegionMove(double x, double y) => GameSession.Current.Move(x, y);
    public static void DesktopRegionMoveBy(double dx, double dy) => GameSession.Current.MoveBy((int)dx, (int)dy);
    public GameCaptureRegion Derive(Mat frame, int x, int y) => new(frame, x, y, this, new TranslationConverter(x, y));
}
