using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using RestEase;
using SFA.DAS.AODP.Jobs.Enum;
using SFA.DAS.AODP.Jobs.Interfaces;
using SFA.DAS.AODP.Jobs.Services;
using SFA.DAS.AODP.Models.Config;

namespace SFA.DAS.AODP.Jobs.Functions
{
    public class ScheduledImportJobRunner
    {
        private readonly ILogger<ScheduledImportJobRunner> _logger;
        private readonly IJobConfigurationService _jobConfigurationService;
        private readonly AodpJobsConfiguration _aodpJobsConfiguration;

        public ScheduledImportJobRunner(ILogger<ScheduledImportJobRunner> logger, IJobConfigurationService jobConfigurationService, AodpJobsConfiguration aodpJobsConfiguration)
        {
            _logger = logger;
            _jobConfigurationService = jobConfigurationService;
            _aodpJobsConfiguration = aodpJobsConfiguration;
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

                        await ExecuteFunction(requestedJobRun, "regulatedQualificationsImport", "gov/regulatedQualificationsImport");                                      
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

                        await ExecuteFunction(requestedJobRun, "approvedQualificationsImport", "api/approvedQualificationsImport");
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

        private async Task ExecuteFunction(JobRunControl requestedJobRun, string functionName, string functionUrlPartial)
        {
            using (HttpClient client = new HttpClient())
            {
                string functionBaseUrl = _aodpJobsConfiguration.FunctionAppBaseUrl ?? "http://localhost:7000";
                string functionHostKey = _aodpJobsConfiguration.FunctionHostKey ?? string.Empty;

                string username = string.IsNullOrWhiteSpace(requestedJobRun.User) ? "ScheduledJob" : requestedJobRun.User;
                string functionUrl = $"{functionBaseUrl}/{functionUrlPartial}/{username}";
                if (!string.IsNullOrWhiteSpace(functionHostKey))
                {
                    functionUrl = $"{functionUrl}?code={functionHostKey}";
                    _logger.LogInformation($"[{nameof(ScheduledImportJobRunner)}] -> Calling function {functionName} job using host key");
                }
                else
                {
                    _logger.LogInformation($"[{nameof(ScheduledImportJobRunner)}] -> Calling function {functionName} job");
                }

                HttpResponseMessage response = await client.GetAsync(functionUrl);

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"[{nameof(ScheduledImportJobRunner)}] -> {functionName} called successfully: {responseBody}");
                }
                else
                {
                    _logger.LogError($"[{nameof(ScheduledImportJobRunner)}] -> Error calling {functionName}: {response.StatusCode}");
                }
            }
        }
    }
}
