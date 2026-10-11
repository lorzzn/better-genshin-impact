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

/// <summary>
/// Process-level model resources, shared by every game invocation of the process.
/// Several invocations may run at once (one per game); the state root, logger,
/// notification and position callbacks belong to the invocation (async flow)
/// that sets them.
/// </summary>
public static class RuntimeEnvironment
{
    private static readonly ResourceManagerStringLocalizerFactory localizers = new(Options.Create(new LocalizationOptions()), NullLoggerFactory.Instance);
    static RuntimeEnvironment() => ServerTimeHelper.Initialize(new ServerTimeProvider(TimeProvider.System));
    public static string AssetRoot { get; set; } = AppContext.BaseDirectory;
    // The embedding host owns task state. The shared library holds what the
    // administrator configures (scripts, routes, strategies, macros, scheduler
    // groups, one-dragon flows); run records and caches stay in StateRoot.
    private static readonly AsyncLocal<string?> stateRoot = new();
    private static readonly string defaultStateRoot = Path.Combine(Path.GetTempPath(), "bettergi-runtime", Environment.ProcessId.ToString());
    public static string StateRoot { get => stateRoot.Value ?? defaultStateRoot; set => stateRoot.Value = value; }
    public static string? LibraryRoot { get; set; }
    private static readonly AsyncLocal<ILogger?> logger = new();
    public static ILogger Logger { get => logger.Value ?? NullLogger.Instance; set => logger.Value = value; }
    private static readonly Lazy<BgiOnnxFactory> onnxFactory = new(() => new BgiOnnxFactory(Logger,
        GameSession.IsBound ? GameSession.Current.Config.HardwareAccelerationConfig : new Core.Config.HardwareAccelerationConfig()));
    public static BgiOnnxFactory OnnxFactory => onnxFactory.Value;
    private static readonly AsyncLocal<Action<Service.Notification.Model.BaseNotificationData>?> notification = new();
    public static Action<Service.Notification.Model.BaseNotificationData>? Notification { get => notification.Value; set => notification.Value = value; }
    public static void ReportNotification(Service.Notification.Model.BaseNotificationData data)
    {
        if (Notification is { } notify) notify(data);
        else Logger.LogInformation("{Event}: {Result}: {Message}", data.Event, data.Result, data.Message);
    }
    private static readonly AsyncLocal<Action<Point2f>?> positionChanged = new();
    public static Action<Point2f>? PositionChanged { get => positionChanged.Value; set => positionChanged.Value = value; }
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
            "User" when segments.Length > 1 && segments[1] is "AutoFight" or "AutoGeniusInvokation" or "AutoPathing" or "KeyMouseScript"
                or "JsScript" or "ScriptGroup" or "OneDragon" => LibraryRoot ?? AssetRoot,
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
