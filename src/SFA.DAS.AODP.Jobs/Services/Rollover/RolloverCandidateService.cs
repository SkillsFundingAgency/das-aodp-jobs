using SFA.DAS.AODP.Infrastructure.Interfaces.Rollover;
using SFA.DAS.AODP.Jobs.Interfaces.Rollover;
using SFA.DAS.AODP.Jobs.Models.Rollover;
using SFA.DAS.Funding.ApprenticeshipEarnings.Domain.Services;

namespace SFA.DAS.AODP.Jobs.Services.Rollover;

public class RolloverCandidateService : IRolloverCandidateService
{
    private readonly IRolloverCandidateRepository _repository;
    private readonly ISystemClockService _systemClockService;

    public RolloverCandidateService(
        IRolloverCandidateRepository repository,
        ISystemClockService systemClockService)
    {
        _repository = repository;
        _systemClockService = systemClockService;
    }

    public Task<int> GenerateRolloverCandidatesAsync(CancellationToken cancellationToken = default)
    {
        var now = _systemClockService.UtcNow;
        var academicYear = AcademicYear.FromDate(now);

        return _repository.CreateInitialRolloverCandidatesAsync(
            academicYear.Name,
            academicYear.EndDate,
            now,
            cancellationToken);
    }
}
