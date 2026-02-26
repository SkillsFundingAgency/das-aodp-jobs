namespace SFA.DAS.AODP.Jobs.Functions;

public class FundedQualificationsDataFunction(
    ILogger<FundedQualificationsDataFunction> logger,
    ICsvReaderService csvReaderService,
    AodpJobsConfiguration config,
    IJobConfigurationService jobConfigurationService,
    IFundedQualificationWriter fundedQualificationWriter,
    IQualificationsRepository qualificationsRepository)
{
    [Function("ApprovedQualificationsDataFunction")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "api/approvedQualificationsImport/{username}")] HttpRequestData req, string username = "")
    {
        string? fundedUrlFilePath = config.FundedQualificationsImportUrl;
        string? archivedUrlFilePath = config.ArchivedFundedQualificationsImportUrl;      

        if (string.IsNullOrEmpty(fundedUrlFilePath))
        {
            var errorMsg = "Config for 'FundedQualificationsImportUrl' is not set or empty.";
            logger.LogError(errorMsg);
            return new BadRequestObjectResult($"[{nameof(FundedQualificationsDataFunction)}] -> {errorMsg}");
        }

        if (string.IsNullOrEmpty(archivedUrlFilePath))
        {
            var errorMsg = "Config for 'ArchivedFundedQualificationsImportUrl' is not set or empty.";
            logger.LogError(errorMsg);
            return new BadRequestObjectResult($"[{nameof(FundedQualificationsDataFunction)}] -> {errorMsg}");
        }
            
        logger.LogInformation($"[{nameof(FundedQualificationsDataFunction)}] -> Reading Configuration");
        var jobControl = await jobConfigurationService.ReadFundedJobConfiguration();

        if (!jobControl.JobEnabled)
        {
            return new OkObjectResult($"[{nameof(FundedQualificationsDataFunction)}] -> Job disabled");
        }

        if (jobControl.Status == JobStatus.Running.ToString())
        {
            return new OkObjectResult($"[{nameof(FundedQualificationsDataFunction)}] -> Job currently running");
        }

        try
        {
            logger.LogInformation($"[{nameof(FundedQualificationsDataFunction)}] -> Starting Job");
            var lastJobRun = await jobConfigurationService.GetLastJobRunAsync(JobNames.FundedQualifications.ToString());
            if (lastJobRun.Id != Guid.Empty && lastJobRun.Status == JobStatus.RequestSent.ToString())
            {
                jobControl.JobRunId = lastJobRun.Id;
                await jobConfigurationService.UpdateJobRun(username, jobControl.JobId, jobControl.JobRunId, 0, JobStatus.Running);
            }
            else
            {
                jobControl.JobRunId = await jobConfigurationService.InsertJobRunAsync(jobControl.JobId, username, JobStatus.Running);
            }

            var qualifications = await qualificationsRepository.GetQualificationsAsync();              
            var organisations = await qualificationsRepository.GetAwardingOrganisationsAsync();
                
            var totalRecords = 0;
            var totalArchivedRecords = 0;

            var tablesCleared = false;
            if (jobControl.ImportFundedCsv)
            {
                logger.LogInformation($"[{nameof(FundedQualificationsDataFunction)}] -> Importing Funded CSV");
                var approvedQualifications = await csvReaderService.ReadCsvFileFromUrlAsync<FundedQualificationDTO, FundedQualificationsImportClassMap>(fundedUrlFilePath, qualifications, organisations, logger);
                //Commented out method to read a file from disk, useful for testing
                //var path = "D:\\Source\\Repos\\das-aodp-jobs\\src\\SFA.DAS.AODP.Jobs\\Data\\approved.csv";
                //var approvedQualifications = _csvReaderService.ReadCSVFromFilePath<FundedQualificationDTO, FundedQualificationsImportClassMap>(path, qualifications, organisations, _logger);

                if (approvedQualifications.Any())
                {
                    await qualificationsRepository.TruncateFundingTables();
                    tablesCleared = true;
                    await fundedQualificationWriter.WriteQualifications(approvedQualifications);                        
                }
                else
                {
                    var warningMsg = "No data found found in approved qualifications csv";
                    logger.LogWarning(warningMsg);
                    await jobConfigurationService.UpdateJobRun(username, jobControl.JobId, jobControl.JobRunId, 0, JobStatus.Error);
                    return new NotFoundObjectResult($"[{nameof(FundedQualificationsDataFunction)}] -> {warningMsg}");
                }
                totalRecords = approvedQualifications.Count();
            }

            if (jobControl.ImportArchivedCsv)
            {
                logger.LogInformation($"[{nameof(FundedQualificationsDataFunction)}] -> Importing Archived CSV");
                var archivedQualifications = await csvReaderService.ReadCsvFileFromUrlAsync<FundedQualificationDTO, FundedQualificationsImportClassMap>(archivedUrlFilePath, qualifications, organisations, logger);
                if (archivedQualifications.Any())
                {
                    if (!tablesCleared)
                    {
                        await qualificationsRepository.TruncateFundingTables();
                    }
                    await fundedQualificationWriter.WriteQualifications(archivedQualifications);
                }
                else
                {
                    var warningMsg = "No data found found in archived qualifications csv";
                    logger.LogWarning(warningMsg);
                    await jobConfigurationService.UpdateJobRun(username, jobControl.JobId, jobControl.JobRunId, 0, JobStatus.Error);
                    return new NotFoundObjectResult($"[{nameof(FundedQualificationsDataFunction)}] -> {warningMsg}");
                }

                totalArchivedRecords = archivedQualifications.Count();
                logger.LogInformation($"{totalArchivedRecords} records imported");
            }
                
            var totalProcessedRecords = totalRecords + totalArchivedRecords;
            if ((totalProcessedRecords) > 0)
            {
                logger.LogInformation($"Seeding funded data into funding offers");
                await fundedQualificationWriter.SeedFundingData();
            }

            await jobConfigurationService.UpdateJobRun(username, jobControl.JobId, jobControl.JobRunId, totalProcessedRecords, JobStatus.Completed);

            var msg = $"[{nameof(FundedQualificationsDataFunction)}] -> {totalRecords} approved qualifications imported, {totalArchivedRecords} archived qualifications imported";
            logger.LogInformation(msg);
            return new OkObjectResult(msg);
        }
        catch (ApiException ex)
        {
            logger.LogError(ex, $"[{nameof(FundedQualificationsDataFunction)}] -> Unexpected api exception occurred: {ex.Message}");
            await jobConfigurationService.UpdateJobRun(username, jobControl.JobId, jobControl.JobRunId, 0, JobStatus.Error);
            return new StatusCodeResult((int)ex.StatusCode);
        }
        catch (SystemException ex)
        {
            logger.LogError(ex, $"[{nameof(FundedQualificationsDataFunction)}] -> Unexpected system exception occurred: {ex.Message}");
            await jobConfigurationService.UpdateJobRun(username, jobControl.JobId, jobControl.JobRunId, 0, JobStatus.Error);
            return new StatusCodeResult(500);
        }
    }
}