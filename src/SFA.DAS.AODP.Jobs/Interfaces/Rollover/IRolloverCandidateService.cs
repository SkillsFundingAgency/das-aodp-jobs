namespace SFA.DAS.AODP.Jobs.Interfaces.Rollover;

public interface IRolloverCandidateService
{
    Task<int> GenerateRolloverCandidatesAsync(CancellationToken cancellationToken);
}
