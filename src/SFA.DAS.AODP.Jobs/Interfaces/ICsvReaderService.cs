using SFA.DAS.AODP.Data.Entities;

namespace SFA.DAS.AODP.Jobs.Interfaces
{
    public interface ICsvReaderService
    {
        Task<List<FundedQualification>> ReadQualifications(string url);
    }
}