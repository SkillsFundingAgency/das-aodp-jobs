namespace SFA.DAS.AODP.Jobs.Functions;

public class ScheduledImportJobRunner(
    ILogger<ScheduledImportJobRunner> logger,
    IJobConfigurationService jobConfigurationService,
    AodpJobsConfiguration aodpJobsConfiguration,
    ISchedulerClientService schedulerClientService,
    ISystemClockService systemClockService)
{
    private readonly AodpJobsConfiguration _aodpJobsConfiguration = aodpJobsConfiguration;

    [Function("ScheduledImportJobRunner")]
    public async Task<IActionResult> Run([TimerTrigger("0 */1 * * * *")] TimerInfo myTimer)
    {
        logger.LogInformation($"[{nameof(ScheduledImportJobRunner)}] -> Scheduled import job runner started at: {DateTime.Now}");

        if (myTimer.ScheduleStatus is not null)
        {
            logger.LogInformation($"[{nameof(ScheduledImportJobRunner)}] -> Next timer schedule at: {myTimer.ScheduleStatus.Next}");
        }

        try
        {
            var executeOfqualImport = true;
            var jobControl = await jobConfigurationService.ReadRegulatedJobConfiguration();
            if (!jobControl.JobEnabled)
            {
                logger.LogInformation($"[{nameof(ScheduledImportJobRunner)}] -> Ofqual import disabled.");
                executeOfqualImport = false;
            }

            if (jobControl.Status == JobStatus.Running.ToString())
            {
                logger.LogInformation($"[{nameof(ScheduledImportJobRunner)}] -> Ofqual import currently running.");
                executeOfqualImport = false;
            }

            var executeFundedmport = true;
            var fundedJobControl = await jobConfigurationService.ReadFundedJobConfiguration();
            if (!fundedJobControl.JobEnabled)
            {
                logger.LogInformation($"[{nameof(ScheduledImportJobRunner)}] -> Funded CSV import disabled.");
                executeFundedmport = false;
            }

            if (fundedJobControl.Status == JobStatus.Running.ToString())
            {
                logger.LogInformation($"[{nameof(ScheduledImportJobRunner)}] -> Funded CSV import currently running.");
                executeFundedmport = false;
            }

            var exectuePldnsImports = true;
            var pldnsJobControl = await jobConfigurationService.ReadPldnsImportConfiguration();
            if (!pldnsJobControl.JobEnabled)
            {
                logger.LogInformation($"[{nameof(ScheduledImportJobRunner)}] -> PLDNS import disabled.");
                exectuePldnsImports = false;
            }
            if (pldnsJobControl.Status == JobStatus.Running.ToString())
            {
                logger.LogInformation($"[{nameof(ScheduledImportJobRunner)}] -> PLDNS import currently running.");
                exectuePldnsImports = false;
            }

            var exectueDefundingListImports = true;
            var defundingListJobControl = await jobConfigurationService.ReadDefundingListImportConfiguration();
            if (!defundingListJobControl.JobEnabled)
            {
                logger.LogInformation($"[{nameof(ScheduledImportJobRunner)}] -> Defunding List import disabled.");
                exectueDefundingListImports = false;
            }
            if (defundingListJobControl.Status == JobStatus.Running.ToString())
            {
                logger.LogInformation($"[{nameof(ScheduledImportJobRunner)}] -> Defunding List import currently running.");
                exectueDefundingListImports = false;
            }

            if (executeOfqualImport)
            {
                var requestedJobRun = await jobConfigurationService.GetLastJobRunAsync(JobNames.RegulatedQualifications.ToString());

                if (requestedJobRun.Id != Guid.Empty && requestedJobRun.Status == JobStatus.Requested.ToString())
                {
                    logger.LogInformation($"[{nameof(ScheduledImportJobRunner)}] -> Found requested Ofqual import job run. Triggering job.");

                    await jobConfigurationService.UpdateJobRun(requestedJobRun.User, requestedJobRun.JobId, requestedJobRun.Id, requestedJobRun.RecordsProcessed ?? 0, JobStatus.RequestSent);

                    var success = await schedulerClientService.ExecuteFunction(requestedJobRun, "regulatedQualificationsImport", "gov/regulatedQualificationsImport");
                    if (!success)
                    {
                        logger.LogError($"[{nameof(ScheduledImportJobRunner)}] -> Call to regulatedQualificationsImport failed");
                        await jobConfigurationService.UpdateJobRun(requestedJobRun.User, requestedJobRun.JobId, requestedJobRun.Id, requestedJobRun.RecordsProcessed ?? 0, JobStatus.Error);
                        return new BadRequestObjectResult("Call to regulatedQualificationsImport failed");
                    }
                }
                else
                {
                    logger.LogInformation($"[{nameof(ScheduledImportJobRunner)}] -> No requested Ofqual import job runs found.");
                }

                // Cleanup operation
                if (requestedJobRun.Id != Guid.Empty && requestedJobRun.Status == JobStatus.RequestSent.ToString())
                {
                    if (requestedJobRun.StartTime < systemClockService.UtcNow.AddHours(-4))
                    {
                        await jobConfigurationService.UpdateJobRun(requestedJobRun.User, requestedJobRun.JobId, requestedJobRun.Id, requestedJobRun.RecordsProcessed ?? 0, JobStatus.Error);
                    }
                }
            }
                
            if (executeFundedmport)
            {
                var requestedJobRun = await jobConfigurationService.GetLastJobRunAsync(JobNames.FundedQualifications.ToString());

                if (requestedJobRun.Id != Guid.Empty && requestedJobRun.Status == JobStatus.Requested.ToString())
                {
                    logger.LogInformation($"[{nameof(ScheduledImportJobRunner)}] -> Found requested Funded CSV import job run. Triggering job.");

                    await jobConfigurationService.UpdateJobRun(requestedJobRun.User, requestedJobRun.JobId, requestedJobRun.Id, requestedJobRun.RecordsProcessed ?? 0, JobStatus.RequestSent);

                    var success = await schedulerClientService.ExecuteFunction(requestedJobRun, "approvedQualificationsImport", "api/approvedQualificationsImport");
                    if (!success)
                    {
                        logger.LogError($"[{nameof(ScheduledImportJobRunner)}] -> Call to approvedQualificationsImport failed");
                        await jobConfigurationService.UpdateJobRun(requestedJobRun.User, requestedJobRun.JobId, requestedJobRun.Id, requestedJobRun.RecordsProcessed ?? 0, JobStatus.Error);
                        return new BadRequestObjectResult("Call to approvedQualificationsImport failed");
                    }
                }
                else
                {
                    logger.LogInformation($"[{nameof(ScheduledImportJobRunner)}] -> No requested Funded CSV import job runs found.");
                }

                // Cleanup operation
                if (requestedJobRun.Id != Guid.Empty && requestedJobRun.Status == JobStatus.RequestSent.ToString())
                {
                    if (requestedJobRun.StartTime < systemClockService.UtcNow.AddHours(-4))
                    {
                        await jobConfigurationService.UpdateJobRun(requestedJobRun.User, requestedJobRun.JobId, requestedJobRun.Id, requestedJobRun.RecordsProcessed ?? 0, JobStatus.Error);
                    }
                }
            }

            if (exectuePldnsImports)
            {
                var requestedJobRun = await jobConfigurationService.GetLastJobRunAsync(JobNames.Pldns.ToString());

                if (requestedJobRun.Id == Guid.Empty)
                {
                    logger.LogInformation($"[{nameof(ScheduledImportJobRunner)}] -> No requested PLDNS import job runs found.");
                }
                else if (requestedJobRun.Status == JobStatus.Requested.ToString())
                {
                    logger.LogInformation($"[{nameof(ScheduledImportJobRunner)}] -> Found requested PLDNS import job run. Triggering job.");
                    await jobConfigurationService.UpdateJobRun(requestedJobRun.User, requestedJobRun.JobId, requestedJobRun.Id, requestedJobRun.RecordsProcessed ?? 0, JobStatus.RequestSent);
                    var success = await schedulerClientService.ExecuteFunction(requestedJobRun, "importPldns", "api/importPldns");
                    if (!success)
                    {
                        logger.LogError($"[{nameof(ScheduledImportJobRunner)}] -> Call to pldnsImport failed");
                        await jobConfigurationService.UpdateJobRun(requestedJobRun.User, requestedJobRun.JobId, requestedJobRun.Id, requestedJobRun.RecordsProcessed ?? 0, JobStatus.Error);
                        return new BadRequestObjectResult("Call to pldnsImport failed");
                    }
                }
                else if (requestedJobRun.Status == JobStatus.RequestSent.ToString())
                {
                    if (requestedJobRun.StartTime < systemClockService.UtcNow.AddHours(-4))
                    {
                        await jobConfigurationService.UpdateJobRun(requestedJobRun.User, requestedJobRun.JobId, requestedJobRun.Id, requestedJobRun.RecordsProcessed ?? 0, JobStatus.Error);
                    }
                }
                else
                {
                    logger.LogInformation($"[{nameof(ScheduledImportJobRunner)}] -> No requested PLDNS import job runs found.");
                }
            }

            if (exectueDefundingListImports)
            {
                var requestedJobRun = await jobConfigurationService.GetLastJobRunAsync(JobNames.DefundingList.ToString());

                if (requestedJobRun.Id == Guid.Empty)
                {
                    logger.LogInformation($"[{nameof(ScheduledImportJobRunner)}] -> No requested Defunding list import job runs found.");
                }
                else if (requestedJobRun.Status == JobStatus.Requested.ToString())
                {
                    logger.LogInformation($"[{nameof(ScheduledImportJobRunner)}] -> Found requested Defunding list import job run. Triggering job.");
                    await jobConfigurationService.UpdateJobRun(requestedJobRun.User, requestedJobRun.JobId, requestedJobRun.Id, requestedJobRun.RecordsProcessed ?? 0, JobStatus.RequestSent);
                    var success = await schedulerClientService.ExecuteFunction(requestedJobRun, "importDefundingList", "api/importDefundingList");
                    if (!success)
                    {
                        logger.LogError($"[{nameof(ScheduledImportJobRunner)}] -> Call to importDefundingList failed");
                        await jobConfigurationService.UpdateJobRun(requestedJobRun.User, requestedJobRun.JobId, requestedJobRun.Id, requestedJobRun.RecordsProcessed ?? 0, JobStatus.Error);
                        return new BadRequestObjectResult("Call to importDefundingList failed");
                    }
                }
                else if (requestedJobRun.Status == JobStatus.RequestSent.ToString())
                {
                    if (requestedJobRun.StartTime < systemClockService.UtcNow.AddHours(-4))
                    {
                        await jobConfigurationService.UpdateJobRun(requestedJobRun.User, requestedJobRun.JobId, requestedJobRun.Id, requestedJobRun.RecordsProcessed ?? 0, JobStatus.Error);
                    }
                }
                else
                {
                    logger.LogInformation($"[{nameof(ScheduledImportJobRunner)}] -> No requested Defunding list import job runs found.");
                }
            }
        }
        catch (ApiException ex)
        {
            logger.LogError($"[{nameof(ScheduledImportJobRunner)}] -> Unexpected api exception occurred: {ex.Message}");
        }
        catch (SystemException ex)
        {
            logger.LogError($"[{nameof(ScheduledImportJobRunner)}] -> Unexpected system exception occurred: {ex.Message}");
        }


        return new OkObjectResult($"[{nameof(ScheduledImportJobRunner)}] -> Job execution complete.");
    }       
}