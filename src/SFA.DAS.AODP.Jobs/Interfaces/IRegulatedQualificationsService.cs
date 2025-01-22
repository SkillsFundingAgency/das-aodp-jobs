using SFA.DAS.AODP.Data;
using SFA.DAS.AODP.Models.Qualification;

namespace SFA.DAS.AODP.Jobs.Interfaces
{
    public interface IRegulatedQualificationsService
    {
        Task<RegulatedQualificationsPaginatedResult<RegulatedQualification>> SearchPrivateQualificationsAsync(RegulatedQualificationsQueryParameters parameters, int page, int limit);

        Task CompareAndUpdateQualificationsAsync(List<RegulatedQualification> importedQualifications, List<RegulatedQualification> processedQualifications);
    }
}
