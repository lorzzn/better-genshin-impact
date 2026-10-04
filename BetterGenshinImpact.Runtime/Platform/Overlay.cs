using System.Collections.Concurrent;
using System.Drawing;
using OpenCvSharp;

namespace BetterGenshinImpact.Runtime
{
    // Portable drawing metadata; never creates GDI handles or a WPF window.
    public sealed record OverlayPen(Color Color, float Width = 2);
}

namespace BetterGenshinImpact.View.Drawable
{
    using BetterGenshinImpact.Runtime;
    public sealed record RectDrawable(Rect2d Rect, OverlayPen? Pen = null, string? Name = null);
    public static class RectDrawableExtension
    {
        public static RectDrawable ToRectDrawable(this OpenCvSharp.Rect rect, OverlayPen? pen = null, string? name = null) =>
            new(new Rect2d(rect.X, rect.Y, rect.Width, rect.Height), pen, name);
        public static RectDrawable ToRectDrawable(this OpenCvSharp.Rect rect, int offsetX, int offsetY, OverlayPen? pen = null, string? name = null) =>
            new(new Rect2d(rect.X + offsetX, rect.Y + offsetY, rect.Width, rect.Height), pen, name);
    }
    public sealed class LineDrawable(double x1, double y1, double x2, double y2)
    {
        public Point2d P1 { get; } = new(x1, y1);
        public Point2d P2 { get; } = new(x2, y2);
        public OverlayPen Pen { get; set; } = new(Color.Red);
    }

    // Retains diagnostic overlay data in the invocation. A headless Target has
    // no desktop mask window; this is UI metadata, never a recognition result.
    public sealed class DrawContent
    {
        public ConcurrentDictionary<string, List<RectDrawable>> RectList { get; } = new();
        public ConcurrentDictionary<string, List<LineDrawable>> LineList { get; } = new();
        public void PutRect(string key, RectDrawable value) => RectList[key] = [value];
        public void PutLine(string key, LineDrawable value) => LineList[key] = [value];
        public void RemoveRect(string key) => RectList.TryRemove(key, out _);
        public void RemoveLine(string key) => LineList.TryRemove(key, out _);
        public void PutOrRemoveRectList(string key, List<RectDrawable>? values)
        {
            if (values is null) RemoveRect(key);
            else RectList[key] = values;
        }
        public void ClearAll() { RectList.Clear(); LineList.Clear(); }
    }
}

namespace BetterGenshinImpact.Helpers.Extensions
{
    public static class TargetColorExtension
    {
        public static Scalar ToScalar(this Color color) => new(color.B, color.G, color.R, color.A);
    }
}
