using System.Collections.Specialized;
using SFA.DAS.AODP.Data;
using SFA.DAS.AODP.Models.Qualification;

namespace SFA.DAS.AODP.Jobs.Interfaces
{
    public interface IOfqualRegisterService
    {
        Task<RegulatedQualificationsPaginatedResult<RegulatedQualificationDTO>> SearchPrivateQualificationsAsync(RegulatedQualificationsQueryParameters parameters, int page, int limit);

        List<RegulatedQualificationDTO> ExtractQualificationsList(RegulatedQualificationsPaginatedResult<RegulatedQualificationDTO> paginatedResult);

        RegulatedQualificationsQueryParameters ParseQueryParameters(NameValueCollection query);
    }
}
