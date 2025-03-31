using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using RestEase;
using SFA.DAS.AODP.Jobs.Enum;
using SFA.DAS.AODP.Jobs.Interfaces;
using SFA.DAS.AODP.Models.Config;

namespace SFA.DAS.AODP.Jobs.Functions
{
    public class ScheduledImportJobRunner
    {
        private readonly ILogger<ScheduledImportJobRunner> _logger;
        private readonly IJobConfigurationService _jobConfigurationService;
        private readonly AodpJobsConfiguration _aodpJobsConfiguration;
        private readonly ISchedulerClientService _schedulerClientService;

        public ScheduledImportJobRunner(ILogger<ScheduledImportJobRunner> logger, 
            IJobConfigurationService jobConfigurationService, 
            AodpJobsConfiguration aodpJobsConfiguration,
            ISchedulerClientService schedulerClientService)
        {
            _logger = logger;
            _jobConfigurationService = jobConfigurationService;
            _aodpJobsConfiguration = aodpJobsConfiguration;
            _schedulerClientService = schedulerClientService;
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

                if (executeOfqualImport)
                {
                    var requestedJobRun = await _jobConfigurationService.GetLastJobRunAsync(JobNames.RegulatedQualifications.ToString());

                    if (requestedJobRun.Id != Guid.Empty && requestedJobRun.Status == JobStatus.Requested.ToString())
                    {
                        _logger.LogInformation($"[{nameof(ScheduledImportJobRunner)}] -> Found requested Ofqual import job run. Triggering job.");

                        await _jobConfigurationService.UpdateJobRun(requestedJobRun.User, requestedJobRun.JobId, requestedJobRun.Id, requestedJobRun.RecordsProcessed ?? 0, JobStatus.RequestSent);

                        await _schedulerClientService.ExecuteFunction(requestedJobRun, "regulatedQualificationsImport", "gov/regulatedQualificationsImport");                                      
                    }
                    else
                    {
                        _logger.LogInformation($"[{nameof(ScheduledImportJobRunner)}] -> No requested Ofqual import job runs found.");
                    }
                }
                
                if (executeFundedmport)
                {
                    var requestedJobRun = await _jobConfigurationService.GetLastJobRunAsync(JobNames.FundedQualifications.ToString());

                    if (requestedJobRun.Id != Guid.Empty && requestedJobRun.Status == JobStatus.Requested.ToString())
                    {
                        _logger.LogInformation($"[{nameof(ScheduledImportJobRunner)}] -> Found requested Funded CSV import job run. Triggering job.");

                        await _jobConfigurationService.UpdateJobRun(requestedJobRun.User, requestedJobRun.JobId, requestedJobRun.Id, requestedJobRun.RecordsProcessed ?? 0, JobStatus.RequestSent);

                        await _schedulerClientService.ExecuteFunction(requestedJobRun, "approvedQualificationsImport", "api/approvedQualificationsImport");
                    }
                    else
                    {
                        _logger.LogInformation($"[{nameof(ScheduledImportJobRunner)}] -> No requested Funded CSV import job runs found.");
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
