namespace SFA.DAS.AODP.Jobs.Services;

public class SchedulerClientService(
    ILogger<SchedulerClientService> logger,
    AodpJobsConfiguration aodpJobsConfiguration,
    IHttpClientFactory httpClientFactory)
    : ISchedulerClientService
{
    public async Task<bool> ExecuteFunction(JobRunControl requestedJobRun, string functionName, string functionUrlPartial)
    {
        var success = false;

        using (HttpClient client = httpClientFactory.CreateClient(functionName))
        {
            string functionBaseUrl = aodpJobsConfiguration.FunctionAppBaseUrl ?? "http://localhost:7000";
            string functionHostKey = aodpJobsConfiguration.FunctionHostKey ?? string.Empty;

            string username = string.IsNullOrWhiteSpace(requestedJobRun.User) ? "ScheduledJob" : requestedJobRun.User;
            string functionUrl = $"{functionBaseUrl}/{functionUrlPartial}/{username}";
            if (!string.IsNullOrWhiteSpace(functionHostKey))
            {
                functionUrl = $"{functionUrl}?code={functionHostKey}";
                logger.LogInformation($"[{nameof(ScheduledImportJobRunner)}] -> Calling function {functionName} job using host key");
            }
            else
            {
                logger.LogInformation($"[{nameof(ScheduledImportJobRunner)}] -> Calling function {functionName} job");
            }

            HttpResponseMessage response = await client.GetAsync(functionUrl);
            string responseBody = "";
            if (response.Content != null)
            {
                responseBody = await response.Content.ReadAsStringAsync();
            }

            if (response.IsSuccessStatusCode)
            {                    
                logger.LogInformation($"[{nameof(ScheduledImportJobRunner)}] -> {functionName} called successfully: {responseBody}");
                success = true;
            }
            else
            {                    
                logger.LogError($"[{nameof(ScheduledImportJobRunner)}] -> Error calling {functionName}: {response.StatusCode}. {responseBody}");
                success = false;
            }
        }

        return success;
    }
}