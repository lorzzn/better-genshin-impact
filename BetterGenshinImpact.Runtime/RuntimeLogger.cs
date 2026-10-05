using Microsoft.Extensions.Logging;

namespace BetterGenshinImpact.Runtime;

internal sealed class RuntimeLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => RuntimeEnvironment.Logger.BeginScope(state);
    public bool IsEnabled(LogLevel level) => RuntimeEnvironment.Logger.IsEnabled(level);
    public void Log<TState>(LogLevel level, EventId id, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
        RuntimeEnvironment.Logger.Log(level, id, state, exception, formatter);
}

public static class RuntimeUi
{
    public static void Error(string message, string? title = null) => RuntimeEnvironment.Logger.LogError("{Title}: {Message}", title, message);
    public static void Warning(string message) => RuntimeEnvironment.Logger.LogWarning("{Message}", message);
}
