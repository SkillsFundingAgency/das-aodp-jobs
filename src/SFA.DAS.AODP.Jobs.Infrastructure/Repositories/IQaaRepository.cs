using SFA.DAS.AODP.Models.QaaQualification;

namespace SFA.DAS.AODP.Infrastructure.Repositories;

/// <summary>
/// Defines methods to interact with the data for Qaa.
/// </summary>
public interface IQaaRepository
{
    /// <summary>
    /// Imports QAA qualifications as an upsert keyed by AIM code.
    /// </summary>
    /// <param name="proposedQualifications">The QAA qualifications returned from the API.</param>
    /// <param name="dateOfSnapshot">The date and time of the snapshot.</param>
    /// <param name="cancellationToken">Propagates a notification that the operation should be cancelled.</param>
    /// <returns>The number of records processed.</returns>
    Task<int> ImportQaaQualificationsAsync(
        IReadOnlyCollection<QaaQualificationResponse> proposedQualifications,
        DateTime dateOfSnapshot,
        CancellationToken cancellationToken);
}
