using OpenCvSharp;

namespace BetterGenshinImpact.GameTask.Model;

/// <summary>Geometry needed by recognition, independent of an operating-system window.</summary>
public interface IRecognitionSurface
{
    double AssetScale { get; }
    double ZoomOutMax1080PRatio { get; }
    double ScaleTo1080PRatio { get; }
    Rect ScaleMax1080PCaptureRect { get; }
}
