using Microsoft.Azure.Functions.Worker.Http;
using SFA.DAS.AODP.Jobs.Interfaces.Rollover;
using SFA.DAS.AODP.Jobs.LoggerMessages;

namespace SFA.DAS.AODP.Jobs.Functions.Rollover;

public class RolloverCandidatesFunction
{
    private readonly ILogger<RolloverCandidatesFunction> _logger;
    private readonly IRolloverCandidateService _rolloverCandidateService;
    private readonly IJobConfigurationService _jobConfigurationService;

    private const string FunctionName = nameof(RolloverCandidatesFunction);
    private const string SystemUser = "SYSTEM";

    public RolloverCandidatesFunction(
        ILogger<RolloverCandidatesFunction> logger,
        IRolloverCandidateService rolloverCandidateService,
        IJobConfigurationService jobConfigurationService)
    {
        _logger = logger;
        _rolloverCandidateService = rolloverCandidateService;
        _jobConfigurationService = jobConfigurationService;
    }

    [Function("RolloverCandidatesFunction")]
    public async Task Run([TimerTrigger("%RolloverCandidatesTimerSchedule%")] TimerInfo timerInfo, FunctionContext functionContext)
    {
        if (timerInfo.ScheduleStatus is not null)
        {
            _logger.NextTimerSchedule(FunctionName, timerInfo.ScheduleStatus.Next);
        }

        await ExecuteAsync(SystemUser, functionContext.CancellationToken);
    }

    [Function("RolloverCandidatesManualFunction")]
    public async Task<IActionResult> RunManual(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "api/rolloverCandidates/{username?}")]
        HttpRequestData request,
        string username = "")
    {
        var result = await ExecuteAsync(
            string.IsNullOrWhiteSpace(username) ? SystemUser : username,
            request.FunctionContext.CancellationToken);

        return new OkObjectResult($"[{FunctionName}] -> {result.Message}");
    }

    private async Task<RolloverCandidatesExecutionResult> ExecuteAsync(string username, CancellationToken cancellationToken)
    {
        var jobControl = new JobControl();

        try
        {
            _logger.GenerationStarted(FunctionName);

            jobControl = await _jobConfigurationService.ReadJobConfiguration(JobNames.RolloverCandidates);

            if (!jobControl.JobEnabled)
            {
                FunctionLoggerMessages.JobDisabled(_logger, FunctionName);
                return RolloverCandidatesExecutionResult.Skipped("Job disabled");
            }

            if (jobControl.Status == JobStatus.Running.ToString())
            {
                FunctionLoggerMessages.JobRunning(_logger, FunctionName);
                return RolloverCandidatesExecutionResult.Skipped("Job currently running");
            }

            jobControl.JobRunId = await _jobConfigurationService.InsertJobRunAsync(
                jobControl.JobId,
                username,
                JobStatus.Running);

            var candidatesCreated = await _rolloverCandidateService.GenerateRolloverCandidatesAsync(cancellationToken);

            await _jobConfigurationService.UpdateJobRun(
                username,
                jobControl.JobId,
                jobControl.JobRunId,
                candidatesCreated,
                JobStatus.Completed);

            if (candidatesCreated == 0)
            {
                _logger.NoCandidatesCreated(FunctionName);
                return RolloverCandidatesExecutionResult.NoCandidatesCreated();
            }

            _logger.CandidatesCreated(FunctionName, candidatesCreated);

            return RolloverCandidatesExecutionResult.Completed(candidatesCreated);
        }
        catch (Exception ex)
        {
            _logger.GenerationFailed(ex, FunctionName);

            try
            {
                if (jobControl.JobId != Guid.Empty)
                {
                    await _jobConfigurationService.UpdateJobRun(
                        username,
                        jobControl.JobId,
                        jobControl.JobRunId,
                        0,
                        JobStatus.Error);
                }
            }
            catch (Exception updateException)
            {
                _logger.FailedToMarkJobRunAsErrored(updateException, FunctionName);
            }

            throw;
        }
    }

    private sealed record RolloverCandidatesExecutionResult(string Message)
    {
        public static RolloverCandidatesExecutionResult Completed(int candidatesCreated) =>
            new($"{candidatesCreated} rollover candidates created.");

        public static RolloverCandidatesExecutionResult NoCandidatesCreated() =>
            new("No qualification versions were added as rollover candidates.");

        public static RolloverCandidatesExecutionResult Skipped(string reason) =>
            new(reason);
    }
}
