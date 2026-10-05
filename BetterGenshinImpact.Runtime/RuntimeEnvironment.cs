using BetterGenshinImpact.Core.Recognition.OCR;
using BetterGenshinImpact.Core.Recognition.OCR.Paddle;
using BetterGenshinImpact.Core.Recognition.ONNX;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using BetterGenshinImpact.Helpers;
using OpenCvSharp;

namespace BetterGenshinImpact.Runtime;

/// <summary>Process-level model resources. Run one game invocation per process.</summary>
public static class RuntimeEnvironment
{
    private static readonly ResourceManagerStringLocalizerFactory localizers = new(Options.Create(new LocalizationOptions()), NullLoggerFactory.Instance);
    static RuntimeEnvironment() => ServerTimeHelper.Initialize(new ServerTimeProvider(TimeProvider.System));
    public static string AssetRoot { get; set; } = AppContext.BaseDirectory;
    public static ILogger Logger { get; set; } = NullLogger.Instance;
    public static BgiOnnxFactory OnnxFactory { get; } = new(NullLogger.Instance,
        new Core.Config.HardwareAccelerationConfig { EnableTensorRtCache = false, OptimizedModel = false });
    public static Action<Service.Notification.Model.BaseNotificationData>? Notification { get; set; }
    public static void ReportNotification(Service.Notification.Model.BaseNotificationData data)
    {
        if (Notification is { } notify) notify(data);
        else Logger.LogInformation("{Event}: {Result}: {Message}", data.Event, data.Result, data.Message);
    }
    public static Action<Point2f>? PositionChanged { get; set; }
    public static void ReportPosition(Point2f position) => PositionChanged?.Invoke(position);
    public static IStringLocalizer<T> Localizer<T>() => new StringLocalizer<T>(localizers);
    private static readonly Dictionary<PaddleOcrService.PaddleOcrModelType, PaddleOcrService> ocrServices = new();

    public static string ResolveResource(string relativePath)
    {
        relativePath = relativePath.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        var root = relativePath.StartsWith("GameTask" + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            ? Path.Combine(AppContext.BaseDirectory, "UpstreamAssets") : AssetRoot;
        return Path.GetFullPath(Path.Combine(root, relativePath));
    }

    internal static IOcrService Paddle => GetOcrService(PaddleOcrService.PaddleOcrModelType.V4);

    public static PaddleOcrService GetOcrService(PaddleOcrService.PaddleOcrModelType model)
    {
        if (!ocrServices.TryGetValue(model, out var service))
            ocrServices.Add(model, service = new PaddleOcrService(OnnxFactory, model));
        return service;
    }

    public static void DisposeModels()
    {
        foreach (var service in ocrServices.Values) service.Dispose();
        ocrServices.Clear();
    }
}
