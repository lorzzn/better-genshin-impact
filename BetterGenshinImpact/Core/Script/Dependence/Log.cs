using Microsoft.Extensions.Logging;

namespace BetterGenshinImpact.Core.Script.Dependence;

public class Log
{
#if BETTERGI_PORTABLE
    private ILogger _logger => Runtime.RuntimeEnvironment.Logger;
#else
    private readonly ILogger<Log> _logger = App.GetLogger<Log>();
#endif

    public void Debug(string? message, params object?[] args)
    {
        _logger.LogDebug(message, args);
    }

    public void Info(string? message, params object?[] args)
    {
        _logger.LogInformation(message, args);
    }

    public void Warn(string? message, params object?[] args)
    {
        _logger.LogWarning(message, args);
    }

    public void Error(string? message, params object?[] args)
    {
        _logger.LogError(message, args);
    }
}
