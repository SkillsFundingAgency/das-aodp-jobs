namespace SFA.DAS.AODP.Jobs.Functions;

public class RegulatedQualificationsDataFunction(
    ILogger<RegulatedQualificationsDataFunction> logger,
    IApplicationDbContext appDbContext,
    IQualificationsService qualificationsService,
    IOfqualImportService ofqualImportService,
    IJobConfigurationService jobConfigurationService)
{
    private readonly IApplicationDbContext _applicationDbContext = appDbContext;
    private readonly IQualificationsService _qualificationsService = qualificationsService;

    [Function("RegulatedQualificationsDataFunction")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "gov/regulatedQualificationsImport/{username}")] HttpRequestData req, string username = "")
    {
        logger.LogInformation($"[{nameof(RegulatedQualificationsDataFunction)}] -> Processing request by user {username}");

        var stopWatch = new Stopwatch();

        logger.LogInformation($"[{nameof(RegulatedQualificationsDataFunction)}] -> Reading Configuration");
        var jobControl = await jobConfigurationService.ReadRegulatedJobConfiguration();           
        var totalRecords = 0;

        if (!jobControl.JobEnabled)
        {
            return new OkObjectResult($"[{nameof(RegulatedQualificationsDataFunction)}] -> Job disabled");
        }

        if (jobControl.Status == JobStatus.Running.ToString())
        {
            return new OkObjectResult($"[{nameof(RegulatedQualificationsDataFunction)}] -> Job currently running");
        }

        logger.LogInformation($"[{nameof(RegulatedQualificationsDataFunction)}] -> Configuration set to Run Api Import = {jobControl.RunApiImport}, Process Staging Data = {jobControl.ProcessStagingData}");

        try
        {
            stopWatch.Start();
                
            var lastJobRun = await jobConfigurationService.GetLastJobRunAsync(JobNames.RegulatedQualifications.ToString());
            if (lastJobRun != null && lastJobRun.Id != Guid.Empty && lastJobRun.Status == JobStatus.RequestSent.ToString())
            {
                jobControl.JobRunId = lastJobRun.Id;
                await jobConfigurationService.UpdateJobRun(username, jobControl.JobId, jobControl.JobRunId, 0, JobStatus.Running);
            }                
            else
            {
                jobControl.JobRunId = await jobConfigurationService.InsertJobRunAsync(jobControl.JobId, username, JobStatus.Running);
            }

            if (jobControl.RunApiImport)
            {
                // STAGE 1 - Import Ofqual Api data to staging area
                totalRecords = await ofqualImportService.ImportApiData(req);
            }

            if (jobControl.ProcessStagingData)
            {
                // STAGE 2 - Process staging data into AODP database
                await ofqualImportService.ProcessQualificationsDataAsync();
            }

            await jobConfigurationService.UpdateJobRun(username, jobControl.JobId, jobControl.JobRunId, totalRecords, JobStatus.Completed);

            stopWatch.Stop();

            logger.LogInformation($"RegulatedQualificationsDataFunction completed in {stopWatch.Elapsed.TotalSeconds:F2} seconds");

            return new OkObjectResult($"[{nameof(RegulatedQualificationsDataFunction)}] -> Successfully Imported Ofqual Data.");
        }
        catch (ApiException ex)
        {
            logger.LogError($"[{nameof(RegulatedQualificationsDataFunction)}] -> Unexpected api exception occurred: {ex.Message}");
            await jobConfigurationService.UpdateJobRun(username, jobControl.JobId, jobControl.JobRunId, totalRecords, JobStatus.Error);
            return new StatusCodeResult((int)ex.StatusCode);
        }
        catch (SystemException ex)
        {
            logger.LogError($"[{nameof(RegulatedQualificationsDataFunction)}] -> Unexpected system exception occurred: {ex.Message}");
            await jobConfigurationService.UpdateJobRun(username, jobControl.JobId, jobControl.JobRunId, totalRecords, JobStatus.Error);
            return new StatusCodeResult(500);
        }
    }        
}