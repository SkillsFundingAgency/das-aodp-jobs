using SFA.DAS.AODP.Models.Qualification;

namespace SFA.DAS.AODP.Jobs.Interfaces
{
    public interface IQualificationsService
    {
        Task SaveQualificationsStagingAsync(List<string> qualificationsJson);

        Task<List<QualificationDTO>> GetStagedQualificationsBatchAsync(int batchSize, int processedCount);

    }
}
