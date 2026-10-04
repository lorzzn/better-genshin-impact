using BetterGenshinImpact.Core.Recognition.OCR;
using BetterGenshinImpact.Core.Recognition.OCR.Paddle;
using BetterGenshinImpact.Core.Recognition.ONNX;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BetterGenshinImpact.Runtime;

/// <summary>Process-level model resources. Run one game invocation per process.</summary>
public static class RuntimeEnvironment
{
    public static string AssetRoot { get; set; } = AppContext.BaseDirectory;
    public static ILogger Logger { get; set; } = NullLogger.Instance;
    private static PaddleOcrService? paddle;

    public static string ResolveResource(string relativePath)
    {
        relativePath = relativePath.Replace('\\', Path.DirectorySeparatorChar);
        var root = relativePath.StartsWith("GameTask" + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            ? Path.Combine(AppContext.BaseDirectory, "UpstreamAssets") : AssetRoot;
        return Path.GetFullPath(Path.Combine(root, relativePath));
    }

    internal static IOcrService Paddle => paddle ??= new PaddleOcrService(new BgiOnnxFactory(), PaddleOcrService.PaddleOcrModelType.V4);

    public static void DisposeModels()
    {
        paddle?.Dispose();
        paddle = null;
    }
}
