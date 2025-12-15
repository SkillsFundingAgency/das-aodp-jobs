using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using RestEase;
using SFA.DAS.AODP.Common.Enum;
using SFA.DAS.AODP.Jobs.Interfaces;
using SFA.DAS.AODP.Models.Config;
using SFA.DAS.Funding.ApprenticeshipEarnings.Domain.Services;

namespace SFA.DAS.AODP.Jobs.Functions
{
    public class ScheduledImportJobRunner
    {
        private readonly ILogger<ScheduledImportJobRunner> _logger;
        private readonly IJobConfigurationService _jobConfigurationService;
        private readonly AodpJobsConfiguration _aodpJobsConfiguration;
        private readonly ISchedulerClientService _schedulerClientService;
        private readonly ISystemClockService _systemClockService;

        public ScheduledImportJobRunner(ILogger<ScheduledImportJobRunner> logger, 
            IJobConfigurationService jobConfigurationService, 
            AodpJobsConfiguration aodpJobsConfiguration,
            ISchedulerClientService schedulerClientService,
            ISystemClockService systemClockService)
        {
            _logger = logger;
            _jobConfigurationService = jobConfigurationService;
            _aodpJobsConfiguration = aodpJobsConfiguration;
            _schedulerClientService = schedulerClientService;
            _systemClockService = systemClockService;
        }

        [Function("ScheduledImportJobRunner")]
        public async Task<IActionResult> Run([TimerTrigger("0 */1 * * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation($"[{nameof(ScheduledImportJobRunner)}] -> Scheduled import job runner started at: {DateTime.Now}");

            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation($"[{nameof(ScheduledImportJobRunner)}] -> Next timer schedule at: {myTimer.ScheduleStatus.Next}");
            }

            try
            {
                var executeOfqualImport = true;
                var jobControl = await _jobConfigurationService.ReadRegulatedJobConfiguration();
                if (!jobControl.JobEnabled)
                {
                    _logger.LogInformation($"[{nameof(ScheduledImportJobRunner)}] -> Ofqual import disabled.");
                    executeOfqualImport = false;
                }

                if (jobControl.Status == JobStatus.Running.ToString())
                {
                    _logger.LogInformation($"[{nameof(ScheduledImportJobRunner)}] -> Ofqual import currently running.");
                    executeOfqualImport = false;
                }

                var executeFundedmport = true;
                var fundedJobControl = await _jobConfigurationService.ReadFundedJobConfiguration();
                if (!fundedJobControl.JobEnabled)
                {
                    _logger.LogInformation($"[{nameof(ScheduledImportJobRunner)}] -> Funded CSV import disabled.");
                    executeFundedmport = false;
                }

                if (fundedJobControl.Status == JobStatus.Running.ToString())
                {
                    _logger.LogInformation($"[{nameof(ScheduledImportJobRunner)}] -> Funded CSV import currently running.");
                    executeFundedmport = false;
                }

                var exectuePldnsImports = true;
                var pldnsJobControl = await _jobConfigurationService.ReadPldnsImportConfiguration();
                if (!pldnsJobControl.JobEnabled)
                {
                    _logger.LogInformation($"[{nameof(ScheduledImportJobRunner)}] -> PLDNS import disabled.");
                    exectuePldnsImports = false;
                }
                if (pldnsJobControl.Status == JobStatus.Running.ToString())
                {
                    _logger.LogInformation($"[{nameof(ScheduledImportJobRunner)}] -> PLDNS import currently running.");
                    exectuePldnsImports = false;
                }

                var exectueDefundingListImports = true;
                var defundingListJobControl = await _jobConfigurationService.ReadDefundingListImportConfiguration();
                if (!defundingListJobControl.JobEnabled)
                {
                    _logger.LogInformation($"[{nameof(ScheduledImportJobRunner)}] -> Defunding List import disabled.");
                    exectueDefundingListImports = false;
                }
                if (defundingListJobControl.Status == JobStatus.Running.ToString())
                {
                    _logger.LogInformation($"[{nameof(ScheduledImportJobRunner)}] -> Defunding List import currently running.");
                    exectueDefundingListImports = false;
                }

                if (executeOfqualImport)
                {
                    var requestedJobRun = await _jobConfigurationService.GetLastJobRunAsync(JobNames.RegulatedQualifications.ToString());

                    if (requestedJobRun.Id != Guid.Empty && requestedJobRun.Status == JobStatus.Requested.ToString())
                    {
                        _logger.LogInformation($"[{nameof(ScheduledImportJobRunner)}] -> Found requested Ofqual import job run. Triggering job.");

                        await _jobConfigurationService.UpdateJobRun(requestedJobRun.User, requestedJobRun.JobId, requestedJobRun.Id, requestedJobRun.RecordsProcessed ?? 0, JobStatus.RequestSent);

                        var success = await _schedulerClientService.ExecuteFunction(requestedJobRun, "regulatedQualificationsImport", "gov/regulatedQualificationsImport");
                        if (!success)
                        {
                            _logger.LogError($"[{nameof(ScheduledImportJobRunner)}] -> Call to regulatedQualificationsImport failed");
                            await _jobConfigurationService.UpdateJobRun(requestedJobRun.User, requestedJobRun.JobId, requestedJobRun.Id, requestedJobRun.RecordsProcessed ?? 0, JobStatus.Error);
                            return new BadRequestObjectResult("Call to regulatedQualificationsImport failed");
                        }
                    }
                    else
                    {
                        _logger.LogInformation($"[{nameof(ScheduledImportJobRunner)}] -> No requested Ofqual import job runs found.");
                    }

                    // Cleanup operation
                    if (requestedJobRun.Id != Guid.Empty && requestedJobRun.Status == JobStatus.RequestSent.ToString())
                    {
                        if (requestedJobRun.StartTime < _systemClockService.UtcNow.AddHours(-4))
                        {
                            await _jobConfigurationService.UpdateJobRun(requestedJobRun.User, requestedJobRun.JobId, requestedJobRun.Id, requestedJobRun.RecordsProcessed ?? 0, JobStatus.Error);
                        }
                    }
                }
                
                if (executeFundedmport)
                {
                    var requestedJobRun = await _jobConfigurationService.GetLastJobRunAsync(JobNames.FundedQualifications.ToString());

                    if (requestedJobRun.Id != Guid.Empty && requestedJobRun.Status == JobStatus.Requested.ToString())
                    {
                        _logger.LogInformation($"[{nameof(ScheduledImportJobRunner)}] -> Found requested Funded CSV import job run. Triggering job.");

                        await _jobConfigurationService.UpdateJobRun(requestedJobRun.User, requestedJobRun.JobId, requestedJobRun.Id, requestedJobRun.RecordsProcessed ?? 0, JobStatus.RequestSent);

                        var success = await _schedulerClientService.ExecuteFunction(requestedJobRun, "approvedQualificationsImport", "api/approvedQualificationsImport");
                        if (!success)
                        {
                            _logger.LogError($"[{nameof(ScheduledImportJobRunner)}] -> Call to approvedQualificationsImport failed");
                            await _jobConfigurationService.UpdateJobRun(requestedJobRun.User, requestedJobRun.JobId, requestedJobRun.Id, requestedJobRun.RecordsProcessed ?? 0, JobStatus.Error);
                            return new BadRequestObjectResult("Call to approvedQualificationsImport failed");
                        }
                    }
                    else
                    {
                        _logger.LogInformation($"[{nameof(ScheduledImportJobRunner)}] -> No requested Funded CSV import job runs found.");
                    }

                    // Cleanup operation
                    if (requestedJobRun.Id != Guid.Empty && requestedJobRun.Status == JobStatus.RequestSent.ToString())
                    {
                        if (requestedJobRun.StartTime < _systemClockService.UtcNow.AddHours(-4))
                        {
                            await _jobConfigurationService.UpdateJobRun(requestedJobRun.User, requestedJobRun.JobId, requestedJobRun.Id, requestedJobRun.RecordsProcessed ?? 0, JobStatus.Error);
                        }
                    }
                }

                if (exectuePldnsImports)
                {
                    var requestedJobRun = await _jobConfigurationService.GetLastJobRunAsync(JobNames.Pldns.ToString());
                    if (requestedJobRun.Id != Guid.Empty && requestedJobRun.Status == JobStatus.Requested.ToString())
                    {
                        _logger.LogInformation($"[{nameof(ScheduledImportJobRunner)}] -> Found requested PLDNS import job run. Triggering job.");
                        await _jobConfigurationService.UpdateJobRun(requestedJobRun.User, requestedJobRun.JobId, requestedJobRun.Id, requestedJobRun.RecordsProcessed ?? 0, JobStatus.RequestSent);
                        var success = await _schedulerClientService.ExecuteFunction(requestedJobRun, "importPldns", "api/importPldns");
                        if (!success)
                        {
                            _logger.LogError($"[{nameof(ScheduledImportJobRunner)}] -> Call to pldnsImport failed");
                            await _jobConfigurationService.UpdateJobRun(requestedJobRun.User, requestedJobRun.JobId, requestedJobRun.Id, requestedJobRun.RecordsProcessed ?? 0, JobStatus.Error);
                            return new BadRequestObjectResult("Call to pldnsImport failed");
                        }
                    }
                    else
                    {
                        _logger.LogInformation($"[{nameof(ScheduledImportJobRunner)}] -> No requested PLDNS import job runs found.");
                    }
                    // Cleanup operation
                    if (requestedJobRun.Id != Guid.Empty && requestedJobRun.Status == JobStatus.RequestSent.ToString())
                    {
                        if (requestedJobRun.StartTime < _systemClockService.UtcNow.AddHours(-4))
                        {
                            await _jobConfigurationService.UpdateJobRun(requestedJobRun.User, requestedJobRun.JobId, requestedJobRun.Id, requestedJobRun.RecordsProcessed ?? 0, JobStatus.Error);
                        }
                    }
                }

                if (exectueDefundingListImports)
                {
                    var requestedJobRun = await _jobConfigurationService.GetLastJobRunAsync(JobNames.DefundingList.ToString());
                    if (requestedJobRun.Id != Guid.Empty && requestedJobRun.Status == JobStatus.Requested.ToString())
                    {
                        _logger.LogInformation($"[{nameof(ScheduledImportJobRunner)}] -> Found requested Defunding list import job run. Triggering job.");
                        await _jobConfigurationService.UpdateJobRun(requestedJobRun.User, requestedJobRun.JobId, requestedJobRun.Id, requestedJobRun.RecordsProcessed ?? 0, JobStatus.RequestSent);
                        var success = await _schedulerClientService.ExecuteFunction(requestedJobRun, "importDefundingList", "api/importDefundingList");
                        if (!success)
                        {
                            _logger.LogError($"[{nameof(ScheduledImportJobRunner)}] -> Call to importDefundingList failed");
                            await _jobConfigurationService.UpdateJobRun(requestedJobRun.User, requestedJobRun.JobId, requestedJobRun.Id, requestedJobRun.RecordsProcessed ?? 0, JobStatus.Error);
                            return new BadRequestObjectResult("Call to importDefundingList failed");
                        }
                    }
                    else
                    {
                        _logger.LogInformation($"[{nameof(ScheduledImportJobRunner)}] -> No requested Defunding list import job runs found.");
                    }
                    // Cleanup operation
                    if (requestedJobRun.Id != Guid.Empty && requestedJobRun.Status == JobStatus.RequestSent.ToString())
                    {
                        if (requestedJobRun.StartTime < _systemClockService.UtcNow.AddHours(-4))
                        {
                            await _jobConfigurationService.UpdateJobRun(requestedJobRun.User, requestedJobRun.JobId, requestedJobRun.Id, requestedJobRun.RecordsProcessed ?? 0, JobStatus.Error);
                        }
                    }
                }
            }
            catch (ApiException ex)
            {
                _logger.LogError($"[{nameof(ScheduledImportJobRunner)}] -> Unexpected api exception occurred: {ex.Message}");
            }
            catch (SystemException ex)
            {
                _logger.LogError($"[{nameof(ScheduledImportJobRunner)}] -> Unexpected system exception occurred: {ex.Message}");
            }


            return new OkObjectResult($"[{nameof(ScheduledImportJobRunner)}] -> Job execution complete.");
        }       
    }
}
