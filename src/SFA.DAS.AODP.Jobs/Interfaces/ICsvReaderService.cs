using SAF.DAS.AODP.Models.Qualification;
using SFA.DAS.AODP.Data.Entities;

namespace SFA.DAS.AODP.Jobs.Interfaces
{
    public interface ICsvReaderService
    {
        Task<IEnumerable<FundedQualificationDTO>> ReadQualifications(string url);
    }
}