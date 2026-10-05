using BetterGenshinImpact.Core.Recognition.ONNX;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Localization;
#if !BETTERGI_PORTABLE
using Microsoft.Extensions.DependencyInjection;
#endif

namespace BetterGenshinImpact.GameTask;

/// <summary>Services used by game execution, independent of the desktop application.</summary>
public static class GameServices
{
    public static void SetClipboardText(string? text)
    {
#if BETTERGI_PORTABLE
        Runtime.GameSession.Current.CancellationToken.ThrowIfCancellationRequested();
        Runtime.GameSession.Current.Host.SetClipboardText(text);
#else
        Helpers.UIDispatcherHelper.Invoke(() =>
        {
            if (text == null) System.Windows.Clipboard.Clear();
            else System.Windows.Clipboard.SetDataObject(text);
        });
#endif
    }

    public static Model.IGamePixelSource CreatePixelSource(System.Threading.CancellationToken cancellationToken) =>
#if BETTERGI_PORTABLE
        new Runtime.TargetPixelSource(cancellationToken);
#else
        new Model.GamePixelSource(TaskContext.Instance().GameHandle);
#endif

    public static BgiOnnxFactory OnnxFactory =>
#if BETTERGI_PORTABLE
        Runtime.RuntimeEnvironment.OnnxFactory;
#else
        App.ServiceProvider.GetRequiredService<BgiOnnxFactory>();
#endif

    public static ILogger<T> GetLogger<T>() =>
#if BETTERGI_PORTABLE
        new Runtime.RuntimeLogger<T>();
#else
        App.GetLogger<T>();
#endif

    public static IStringLocalizer<T> Localizer<T>() =>
#if BETTERGI_PORTABLE
        Runtime.RuntimeEnvironment.Localizer<T>();
#else
        App.GetService<IStringLocalizer<T>>()!;
#endif
}
