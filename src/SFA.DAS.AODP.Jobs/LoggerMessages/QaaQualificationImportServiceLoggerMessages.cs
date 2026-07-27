namespace SFA.DAS.AODP.Jobs.LoggerMessages;

/// <summary>
/// Defines static logger messages using source generators to generate the logging messages at compile time for the QAA qualification import service.
/// </summary>
public static partial class QaaQualificationImportServiceLoggerMessages
{
    [LoggerMessage(LogLevel.Information, message: "No qualifications found from QAA Api, nothing to do.")]
    public static partial void NoQaaQualificationsFound(this ILogger logger);

    [LoggerMessage(LogLevel.Information, message: "Finished import, processed {NumberOfRecordsProcessed}")]
    public static partial void FinishedImport(this ILogger logger, int numberOfRecordsProcessed);

    [LoggerMessage(LogLevel.Error, message: "Could not call the Qaa API.")]
    public static partial void FailedToCallQaaApi(this ILogger logger, Exception ex);
}
