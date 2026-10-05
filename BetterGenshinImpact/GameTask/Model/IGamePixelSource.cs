using System;
using OpenCvSharp;

namespace BetterGenshinImpact.GameTask.Model;

/// <summary>Pixel sampling on the same capture surface used for task input.</summary>
public interface IGamePixelSource : IDisposable
{
    Vec3b GetPixel(int x, int y);
}
