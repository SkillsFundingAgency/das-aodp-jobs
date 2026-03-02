namespace SFA.DAS.AODP.Jobs.LoggerMessages;

/// <summary>
/// Defines static logger messages using source generators to generate the logging messages at compile time.
/// </summary>
public static partial class FunctionLoggerMessages
{
    [LoggerMessage(LogLevel.Information, message: "[{FunctionName}] -> Job currently running")]
    public static partial void JobRunning(this ILogger logger, string functionName);

    [LoggerMessage(LogLevel.Information, message: "[{FunctionName}] -> Job disabled")]
    public static partial void JobDisabled(this ILogger logger, string functionName);

    [LoggerMessage(LogLevel.Information, message: "[{FunctionName}] -> Completed in {Seconds:F2}s.")]
    public static partial void JobCompleted(this ILogger logger, string functionName, double seconds);

    [LoggerMessage(LogLevel.Error, message: "[{FunctionName}] -> Unexpected api exception occurred: {Message}")]
    public static partial void UnexpectedApiError(this ILogger logger, string functionName, string message);

    [LoggerMessage(LogLevel.Error, message: "[{FunctionName}] -> Unexpected system exception occurred: {Message}")]
    public static partial void UnexpectedSystemError(this ILogger logger, string functionName, string message);
}