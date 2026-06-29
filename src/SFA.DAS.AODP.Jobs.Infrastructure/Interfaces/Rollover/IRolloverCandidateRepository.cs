namespace SFA.DAS.AODP.Infrastructure.Interfaces.Rollover;

public interface IRolloverCandidateRepository
{
    Task<int> CreateInitialRolloverCandidatesAsync(
        string academicYear,
        DateOnly academicYearEndDate,
        DateTime createdAt,
        CancellationToken cancellationToken = default);
}
