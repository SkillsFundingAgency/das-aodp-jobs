using Microsoft.Extensions.Logging;
using SFA.DAS.AODP.Jobs.Functions;
using SFA.DAS.AODP.Jobs.Interfaces;
using SFA.DAS.AODP.Jobs.Models.Jobs;
using SFA.DAS.AODP.Models.Config;

namespace SFA.DAS.AODP.Jobs.Services
{
    public class SchedulerClientService : ISchedulerClientService
    {
        private readonly ILogger<SchedulerClientService> _logger;
        private readonly AodpJobsConfiguration _aodpJobsConfiguration;
        private readonly IHttpClientFactory _httpClientFactory;

        public SchedulerClientService(ILogger<SchedulerClientService> logger,
            AodpJobsConfiguration aodpJobsConfiguration,
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _aodpJobsConfiguration = aodpJobsConfiguration;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<bool> ExecuteFunction(JobRunControl requestedJobRun, string functionName, string functionUrlPartial)
        {
            var success = false;

            using (HttpClient client = _httpClientFactory.CreateClient(functionName))
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
                string responseBody = "";
                if (response.Content != null)
                {
                    responseBody = await response.Content.ReadAsStringAsync();
                }

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation(
                        $"[{nameof(ScheduledImportJobRunner)}] -> {functionName} called successfully: {responseBody}"
                    );
                    success = true;
                }
                else
                {
                    //404 means the URL is wrong (double /api, missing username, wrong route)
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        _logger.LogError(
                            $"[{nameof(ScheduledImportJobRunner)}] -> 404 Not Found calling {functionName}. " +
                            $"This usually means the function URL is incorrect. Final URL: '{functionUrl}'. " +
                            $"Response: {responseBody}"
                        );
                    }
                    else
                    {
                        _logger.LogError(
                            $"[{nameof(ScheduledImportJobRunner)}] -> Error calling {functionName}: " +
                            $"Status={response.StatusCode}, Url='{functionUrl}', Body='{responseBody}'"
                        );
                    }

                    success = false;
                }
            }

            return success;
        }
    }
}
