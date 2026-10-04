using OpenCvSharp;
using System;
using System.Collections.Generic;
namespace BetterGenshinImpact.Core.Recognition.OpenCv;
public static partial class CommonExtension
{
    public static Point GetCenterPoint(this Rect rectangle)
    {
        if (rectangle == default) throw new ArgumentException("rectangle is empty");

        return new Point(rectangle.X + rectangle.Width / 2, rectangle.Y + rectangle.Height / 2);
    }

    public static Rect Multiply(this Rect rect, double assetScale)
    {
        if (rect == default) throw new ArgumentException("rect is empty");

        return new Rect((int)(rect.X * assetScale), (int)(rect.Y * assetScale), (int)(rect.Width * assetScale), (int)(rect.Height * assetScale));
    }

    public static Point2d ToPoint2d(this Point2f p)
    {
        return new Point2d(p.X, p.Y);
    }

    public static List<Point2d> ToPoint2d(this List<Point2f> list)
    {
        return list.ConvertAll(ToPoint2d);
    }

    /// <summary>
    /// 将矩形钳位到指定尺寸范围内（交集语义），防止 OpenCV ROI 越界
    /// </summary>
    public static Rect ClampTo(this Rect rect, int maxWidth, int maxHeight)
    {
        int x1 = Math.Clamp(rect.X, 0, maxWidth);
        int y1 = Math.Clamp(rect.Y, 0, maxHeight);
        int x2 = Math.Clamp(rect.X + rect.Width, 0, maxWidth);
        int y2 = Math.Clamp(rect.Y + rect.Height, 0, maxHeight);
        return new Rect(x1, y1, x2 - x1, y2 - y1);
    }

    /// <summary>
    /// 将矩形钳位到 Mat 范围内（交集语义），防止 OpenCV ROI 越界
    /// </summary>
    public static Rect ClampTo(this Rect rect, Mat mat)
    {
        return rect.ClampTo(mat.Cols, mat.Rows);
    }
}
