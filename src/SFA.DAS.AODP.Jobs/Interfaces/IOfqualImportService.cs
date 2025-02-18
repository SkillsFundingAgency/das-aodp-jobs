using Microsoft.Azure.Functions.Worker.Http;

namespace SFA.DAS.AODP.Jobs.Interfaces
{
    public interface IOfqualImportService
    {
        Task<int> StageQualificationsDataAsync(HttpRequestData request);

        Task ProcessQualificationsDataAsync();
    }
}
