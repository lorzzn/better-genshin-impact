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
    // The embedding host owns task state. Shared libraries contain only inputs
    // (subscribed routes, strategies and macros), never per-Target progress.
    public static string StateRoot { get; set; } = Path.Combine(Path.GetTempPath(), "bettergi-runtime", Environment.ProcessId.ToString());
    public static string? LibraryRoot { get; set; }
    public static ILogger Logger { get; set; } = NullLogger.Instance;
    private static readonly Lazy<BgiOnnxFactory> onnxFactory = new(() => new BgiOnnxFactory(Logger,
        GameSession.IsBound ? GameSession.Current.Config.HardwareAccelerationConfig : new Core.Config.HardwareAccelerationConfig()));
    public static BgiOnnxFactory OnnxFactory => onnxFactory.Value;
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
    private static readonly object modelSync = new();

    public static string ResolveResource(string relativePath)
    {
        relativePath = relativePath.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        var segments = relativePath.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        var first = segments.Length > 0 ? segments[0] : "";
        var root = first switch
        {
            "GameTask" => Path.Combine(AppContext.BaseDirectory, "UpstreamAssets"),
            "User" when segments.Length > 1 && segments[1] is "AutoFight" or "AutoGeniusInvokation" or "AutoPathing" or "KeyMouseScript" => LibraryRoot ?? AssetRoot,
            "User" or "log" or "Cache" => StateRoot,
            _ => AssetRoot,
        };
        return Path.GetFullPath(Path.Combine(root, relativePath));
    }

    internal static IOcrService Paddle => GetOcrService(PaddleOcrService.PaddleOcrModelType.V4);

    public static PaddleOcrService GetOcrService(PaddleOcrService.PaddleOcrModelType model)
    {
        lock (modelSync)
        {
            if (!ocrServices.TryGetValue(model, out var service))
                ocrServices.Add(model, service = new PaddleOcrService(OnnxFactory, model));
            return service;
        }
    }

    public static void DisposeModels()
    {
        lock (modelSync)
        {
            foreach (var service in ocrServices.Values) service.Dispose();
            ocrServices.Clear();
        }
    }
}
