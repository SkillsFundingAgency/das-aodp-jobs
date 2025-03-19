using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using RestEase;
using SFA.DAS.AODP.Jobs.Enum;
using SFA.DAS.AODP.Jobs.Interfaces;

namespace SFA.DAS.AODP.Jobs.Functions
{
    public class ScheduledImportJobRunner
    {
        private readonly ILogger _logger;
        private readonly IJobConfigurationService _jobConfigurationService;

        public ScheduledImportJobRunner(ILoggerFactory loggerFactory, IJobConfigurationService jobConfigurationService)
        {
            _logger = loggerFactory.CreateLogger<ScheduledImportJobRunner>();
            _jobConfigurationService = jobConfigurationService;
        }

        [Function("ScheduledImportJobRunner")]
        public async Task<IActionResult> Run([TimerTrigger("*/5 * * * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation($"[{nameof(ScheduledImportJobRunner)}] -> Scheduled import job runner started at: {DateTime.Now}");

            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation($"[{nameof(ScheduledImportJobRunner)}] -> Next timer schedule at: {myTimer.ScheduleStatus.Next}");
            }

            try
            {
                var requestedJobRun = await _jobConfigurationService.GetRequestedJobsAsync();

                if (requestedJobRun.Id != Guid.Empty)
                {
                    _logger.LogInformation($"[{nameof(ScheduledImportJobRunner)}] -> Found requested job run. Triggering job.");

                    await _jobConfigurationService.UpdateJobRun(requestedJobRun.User, requestedJobRun.JobId, requestedJobRun.Id, requestedJobRun.RecordsProcessed ?? 0, JobStatus.Running);

                    // fire and forget the requested job!
                    _ = Task.Run(async () =>
                    {
                        using (HttpClient client = new HttpClient())
                        {
                            string functionBaseUrl = Environment.GetEnvironmentVariable("FUNCTION_APP_BASE_URL") ?? "http://localhost:7000";

                            string username = "ScheduledJob";
                            string functionUrl = $"{functionBaseUrl}/gov/regulatedQualificationsImport/{username}";

                            HttpResponseMessage response = await client.GetAsync(functionUrl);

                            if (response.IsSuccessStatusCode)
                            {
                                string responseBody = await response.Content.ReadAsStringAsync();
                                _logger.LogInformation($"[{nameof(ScheduledImportJobRunner)}] -> RegulatedQualificationsDataFunction called successfully: {responseBody}");
                            }
                            else
                            {
                                _logger.LogError($"[{nameof(ScheduledImportJobRunner)}] -> Error calling RegulatedQualificationsDataFunction: {response.StatusCode}");
                            }
                        }
                    });
                }
                else
                {
                    _logger.LogInformation($"[{nameof(ScheduledImportJobRunner)}] -> No requested job runs found.");
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
