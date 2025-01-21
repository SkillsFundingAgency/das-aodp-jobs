using SFA.DAS.AODP.Data;
using SFA.DAS.AODP.Models.Qualification;

namespace SFA.DAS.AODP.Jobs.Interfaces
{
    public interface IQualificationsApiService
    {
        Task<PaginatedResult<RegulatedQualification>> SearchPrivateQualificationsAsync(RegulatedQualificationQueryParameters parameters, int page, int limit);

    }
}
