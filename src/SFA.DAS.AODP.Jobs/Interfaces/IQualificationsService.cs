using SFA.DAS.AODP.Models.Qualification;

namespace SFA.DAS.AODP.Jobs.Interfaces
{
    public interface IQualificationsService
    {
        Task CompareAndUpdateQualificationsAsync(List<QualificationDTO> importedQualifications, List<QualificationDTO> processedQualifications);

        Task SaveQualificationsStagingAsync(List<string> qualificationsJson);

        Task<List<QualificationDTO>> GetStagedQualifcationsAsync();
    }
}
