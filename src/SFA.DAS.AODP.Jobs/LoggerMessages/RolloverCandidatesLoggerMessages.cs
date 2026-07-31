namespace SFA.DAS.AODP.Jobs.LoggerMessages;

public static partial class RolloverCandidatesLoggerMessages
{
    [LoggerMessage(LogLevel.Information, message: "[{FunctionName}] -> Rollover candidate generation started.")]
    public static partial void GenerationStarted(this ILogger logger, string functionName);

    [LoggerMessage(LogLevel.Information, message: "[{FunctionName}] -> Next timer schedule at: {NextSchedule}")]
    public static partial void NextTimerSchedule(this ILogger logger, string functionName, DateTime nextSchedule);

    [LoggerMessage(LogLevel.Information, message: "[{FunctionName}] -> Added {RolloverCandidatesCreated} qualifications as rollover candidates.")]
    public static partial void CandidatesCreated(this ILogger logger, string functionName, int rolloverCandidatesCreated);

    [LoggerMessage(LogLevel.Information, message: "[{FunctionName}] -> No qualification versions were added as rollover candidates.")]
    public static partial void NoCandidatesCreated(this ILogger logger, string functionName);

    [LoggerMessage(LogLevel.Error, message: "[{FunctionName}] -> Rollover candidate generation failed.")]
    public static partial void GenerationFailed(this ILogger logger, Exception exception, string functionName);

    [LoggerMessage(LogLevel.Error, message: "[{FunctionName}] -> Failed to mark rollover candidate job run as errored.")]
    public static partial void FailedToMarkJobRunAsErrored(this ILogger logger, Exception exception, string functionName);
}
