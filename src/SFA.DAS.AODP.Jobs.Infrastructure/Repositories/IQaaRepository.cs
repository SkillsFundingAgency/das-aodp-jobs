using SFA.DAS.AODP.Data.Entities;

namespace SFA.DAS.AODP.Infrastructure.Repositories;

/// <summary>
/// Defines methods to interact with the data for Qaa.
/// </summary>
public interface IQaaRepository
{
    /// <summary>
    /// Runs pre-import steps required to allow the import to work correctly.
    /// </summary>
    /// <param name="cancellationToken">Propagates a notification that the operation should be cancelled.</param>
    Task<int> RunPrerequisitesForImportAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Performs the import of the data as a single unit of work.
    /// </summary>
    /// <param name="entries">The <see cref="RegulatedQaaQualification"/>s to create.</param>
    /// <param name="cancellationToken">Propagates a notification that the operation should be cancelled.</param>
    /// <returns>The completed task.</returns>
    Task RunImportAsync(IEnumerable<RegulatedQaaQualification> entries, CancellationToken cancellationToken);
}