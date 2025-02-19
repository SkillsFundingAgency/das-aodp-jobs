using Microsoft.Azure.Functions.Worker.Http;

namespace SFA.DAS.AODP.Jobs.Interfaces
{
    public interface IOfqualImportService
    {
        Task StageQualificationsDataAsync(HttpRequestData request);

        //Task ProcessQualificationsDataAsync();
    }
}
