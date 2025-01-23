using SFA.DAS.AODP.Models.Qualification;

namespace SFA.DAS.AODP.Jobs.Interfaces
{
    public interface IRegulatedQualificationsService
    {
        Task CompareAndUpdateQualificationsAsync(List<RegulatedQualificationDTO> importedQualifications, List<RegulatedQualificationDTO> processedQualifications);

        Task SaveRegulatedQualificationsAsync(List<RegulatedQualificationDTO> qualifications);
    }
}
