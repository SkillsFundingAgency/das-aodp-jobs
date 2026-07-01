using SFA.DAS.AODP.Jobs.Functions.Abstractions;
using SFA.DAS.AODP.Jobs.Interfaces.Rollover;

namespace SFA.DAS.AODP.Jobs.Functions.Rollover;

public class RolloverCandidatesFunction(
    ILogger<RolloverCandidatesFunction> logger,
    IRolloverCandidateService rolloverCandidateService,
    IJobFunctionRunner jobFunctionRunner)
{
    private readonly ILogger<RolloverCandidatesFunction> _logger = logger;
    private readonly IRolloverCandidateService _rolloverCandidateService = rolloverCandidateService;
    private readonly IJobFunctionRunner _jobFunctionRunner = jobFunctionRunner;

    private const string FunctionName = nameof(RolloverCandidatesFunction);
    private const string SystemUser = "SYSTEM";

    [Function("RolloverCandidatesFunction")]
    public async Task<IActionResult> Run([TimerTrigger("%RolloverCandidatesTimerSchedule%")] TimerInfo timerInfo, FunctionContext functionContext)
    {
        return await _jobFunctionRunner.RunAsync(
            FunctionName, 
            SystemUser, 
            JobNames.RolloverCandidates,
            DoWorkAsync, 
            functionContext.CancellationToken);
    }

    private async Task<int> DoWorkAsync(JobControl control, CancellationToken cancellationToken) 
        => await _rolloverCandidateService.GenerateRolloverCandidatesAsync(cancellationToken);
}