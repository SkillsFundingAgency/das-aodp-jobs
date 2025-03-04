using SFA.DAS.AODP.Models.Qualification;

namespace SFA.DAS.AODP.Jobs.Interfaces
{
    public interface IQualificationsService
    {
        Task SaveQualificationsStagingAsync();

        Task<List<QualificationDTO>> GetStagedQualificationsBatchAsync(int batchSize, int processedCount);

        Task AddQualificationsStagingRecords(List<string> qualificationsJson);

    }
}
