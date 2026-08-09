using System.Runtime.CompilerServices;
using Serilog;

namespace KncWX2Server.Core.Logging;

/// <summary>
/// Extension methods for structured logging.
/// </summary>
public static class LoggerExtensions
{
    /// <summary>
    /// Logs debug information with caller context.
    /// </summary>
    public static void LogDebugWithContext(
        this ILogger logger,
        string message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        logger.Debug("{Message} [{Member} at {File}:{Line}]", message, memberName, Path.GetFileName(filePath), lineNumber);
    }

    /// <summary>
    /// Logs information with context.
    /// </summary>
    public static void LogInfoWithContext(
        this ILogger logger,
        string message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        logger.Information("{Message} [{Member} at {File}:{Line}]", message, memberName, Path.GetFileName(filePath), lineNumber);
    }

    /// <summary>
    /// Logs a warning with context.
    /// </summary>
    public static void LogWarningWithContext(
        this ILogger logger,
        string message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        logger.Warning("{Message} [{Member} at {File}:{Line}]", message, memberName, Path.GetFileName(filePath), lineNumber);
    }

    /// <summary>
    /// Logs an error with context.
    /// </summary>
    public static void LogErrorWithContext(
        this ILogger logger,
        Exception? exception,
        string message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        logger.Error(exception, "{Message} [{Member} at {File}:{Line}]", message, memberName, Path.GetFileName(filePath), lineNumber);
    }
}

/// <summary>
/// Serilog logger configuration.
/// </summary>
public static class LoggerConfiguration
{
    /// <summary>
    /// Configures and returns a global logger instance.
    /// </summary>
    public static ILogger ConfigureLogger(string logDirectory = "logs")
    {
        Directory.CreateDirectory(logDirectory);

        return new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File(
                path: Path.Combine(logDirectory, "server-.log"),
                rollingInterval: RollingInterval.Day,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }
}
