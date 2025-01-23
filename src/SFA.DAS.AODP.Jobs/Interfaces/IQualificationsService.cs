using SFA.DAS.AODP.Models.Qualification;

namespace SFA.DAS.AODP.Jobs.Interfaces
{
    public interface IQualificationsService
    {
        Task CompareAndUpdateQualificationsAsync(List<QualificationDTO> importedQualifications, List<QualificationDTO> processedQualifications);

        Task SaveRegulatedQualificationsAsync(List<QualificationDTO> qualifications);

        Task<List<QualificationDTO>> GetAllProcessedRegulatedQualificationsAsync();
    }
}
