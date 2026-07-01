using SFA.DAS.AODP.Infrastructure.Models;

namespace SFA.DAS.AODP.Infrastructure.Interfaces.Rollover;

public interface IRolloverCandidateRepository
{
    Task<int> CreateInitialRolloverCandidatesAsync(
        AcademicYear academicYear,
        CancellationToken cancellationToken);
}