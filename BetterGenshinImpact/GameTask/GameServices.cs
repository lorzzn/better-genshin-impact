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
