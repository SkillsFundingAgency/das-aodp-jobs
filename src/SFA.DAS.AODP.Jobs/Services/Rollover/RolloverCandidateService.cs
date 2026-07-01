using SFA.DAS.AODP.Infrastructure.Interfaces.Rollover;
using SFA.DAS.AODP.Jobs.Interfaces.Rollover;
using SFA.DAS.AODP.Jobs.Models.Rollover;

namespace SFA.DAS.AODP.Jobs.Services.Rollover;

public class RolloverCandidateService(
    IRolloverCandidateRepository repository,
    ISystemClockService systemClockService)
    : IRolloverCandidateService
{
    public Task<int> GenerateRolloverCandidatesAsync(CancellationToken cancellationToken)
    {
        var now = systemClockService.UtcNow;
        var academicYear = AcademicYear.FromDate(now);

        return repository.CreateInitialRolloverCandidatesAsync(
            academicYear.Name,
            academicYear.EndDate,
            now,
            cancellationToken);
    }
}