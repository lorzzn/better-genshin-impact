using System;
using OpenCvSharp;
using Vanara.PInvoke;

namespace BetterGenshinImpact.GameTask.Model;

internal sealed class GamePixelSource(IntPtr window) : IGamePixelSource
{
    public Vec3b GetPixel(int x, int y)
    {
        var dc = User32.GetDC(window);
        try
        {
            var color = Gdi32.GetPixel(dc, x, y);
            return new(color.B, color.G, color.R);
        }
        finally { User32.ReleaseDC(window, dc); }
    }

    public void Dispose() { }
}
