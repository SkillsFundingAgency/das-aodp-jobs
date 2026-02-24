namespace SFA.DAS.AODP.Jobs.Functions;

/// <summary>
/// Default implementation <see cref="IJobFunctionRunner"/>.
/// </summary>
/// <param name="logger">The logger to log to.</param>
/// <param name="jobConfigurationService">The service that handles managing the job configuration.</param>
public class JobFunctionRunner(
    ILogger<JobFunctionRunner> logger,
    IJobConfigurationService jobConfigurationService) : IJobFunctionRunner
{
    /// <inheritdoc/>.
    public async Task<IActionResult> RunAsync(
        string functionName,
        string username,
        JobNames jobName,
        Func<JobControl, CancellationToken, Task<int>> doImport,
        CancellationToken cancellationToken)
    {
        var sw = new Stopwatch();
        var jobControl = new JobControl();

        try
        {
            logger.LogInformation("[{FunctionName}] -> Reading configuration", functionName);
            jobControl = await jobConfigurationService.ReadJobConfiguration(jobName);

            if (!jobControl.JobEnabled)
            {
                logger.JobDisabled(functionName);
                return new OkObjectResult($"[{functionName}] -> Job disabled");
            }

            if (jobControl.Status is nameof(JobStatus.Running))
            {
                logger.JobRunning(functionName);
                return new OkObjectResult($"[{functionName}] -> Job currently running");
            }

            var lastJobRun = await jobConfigurationService.GetLastJobRunAsync(jobName.ToString());

            if (lastJobRun.Id != Guid.Empty && 
                lastJobRun.Status is not nameof(JobStatus.Running))
            {
                jobControl.JobRunId = lastJobRun.Id;
                await jobConfigurationService.UpdateJobRun(username, jobControl.JobId, jobControl.JobRunId, 0,
                    JobStatus.Running);
            }
            else
            {
                jobControl.JobRunId = await jobConfigurationService.InsertJobRunAsync(jobControl.JobId, username, JobStatus.Running);
            }

            sw.Start();
            
            var totalRecords = await doImport(jobControl, cancellationToken);

            await jobConfigurationService.UpdateJobRun(
                username,
                jobControl.JobId,
                jobControl.JobRunId,
                totalRecords,
                JobStatus.Completed);

            sw.Stop();

            logger.JobCompleted(functionName, sw.Elapsed.TotalSeconds);

            return new OkObjectResult($"[{functionName}] -> Completed.");
        }
        catch (ApiException ex)
        {
            return await HandleApiCallExceptionAsync(functionName, username, ex.Message, ex.StatusCode, jobControl);
        }
        catch (HttpRequestException ex)
        {
            return await HandleApiCallExceptionAsync(functionName, username, ex.Message, ex.StatusCode, jobControl);
        }
        catch (Exception ex)
        {
            return await HandleException(functionName, username, ex, jobControl);
        }
    }

    private async Task<IActionResult> HandleException(string functionName, string username, Exception ex,
        JobControl jobControl)
    {
        logger.UnexpectedSystemError(functionName, ex.Message);
        return await JobErrored(username, HttpStatusCode.InternalServerError, jobControl);
    }

    private async Task<IActionResult> HandleApiCallExceptionAsync(string functionName, string username, string message, HttpStatusCode? statusCode, JobControl jobControl)
    {
        logger.UnexpectedApiError(functionName, message);
        return await JobErrored(username, statusCode, jobControl);
    }

    private async Task<IActionResult> JobErrored(string username, HttpStatusCode? statusCode, JobControl jobControl)
    {
        await jobConfigurationService.UpdateJobRun(username, jobControl.JobId, jobControl.JobRunId, 0, JobStatus.Error);
        return new StatusCodeResult((int)statusCode.GetValueOrDefault());
    }
}