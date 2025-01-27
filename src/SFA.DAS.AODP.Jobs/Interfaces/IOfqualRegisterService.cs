using System.Collections.Specialized;
using SFA.DAS.AODP.Data;
using SFA.DAS.AODP.Models.Qualification;

namespace SFA.DAS.AODP.Jobs.Interfaces
{
    public interface IOfqualRegisterService
    {
        Task<RegulatedQualificationsPaginatedResult<QualificationDTO>> SearchPrivateQualificationsAsync(RegulatedQualificationsQueryParameters parameters);

        List<QualificationDTO> ExtractQualificationsList(RegulatedQualificationsPaginatedResult<QualificationDTO> paginatedResult);

        RegulatedQualificationsQueryParameters ParseQueryParameters(NameValueCollection query);
    }
}
