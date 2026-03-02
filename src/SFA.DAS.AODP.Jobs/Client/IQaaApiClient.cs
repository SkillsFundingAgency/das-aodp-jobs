namespace SFA.DAS.AODP.Jobs.Client;

/// <summary>
/// Defines an API client for interacting with the QAA (Quality Assurance Agency for Higher Education) API.
/// </summary>
public interface IQaaApiClient
{
    /// <summary>
    /// Get all qualifications (or as is called on their side, all Diplomas) from the QAA API, this call returns all qualifications on their system, it is on us to perform checks on what has changed and act accordingly.
    /// </summary>
    /// <param name="cancellationToken">Propagates a notification that the operation should be cancelled.</param>
    /// <returns>Collection of <see cref="QaaQualificationResponse"/> containing details of the diplomas retrieved from the API.</returns>
    Task<IList<QaaQualificationResponse>> GetQualificationsAsync(CancellationToken cancellationToken);
}