namespace SFA.DAS.AODP.Jobs.Services;

/// <summary>
/// Defines the service layer for importing QAA qualification (diploma) data and handling any processing.
/// </summary>
public interface IQaaQualificationImportService
{
    /// <summary>
    /// Imports the QAA data from the external data source.
    /// </summary>
    /// <param name="cancellationToken">Propagates a notification that the operation should be cancelled.</param>
    /// <returns>Total number of records imported.</returns>
    Task<int> ImportDataAsync(CancellationToken cancellationToken);
}