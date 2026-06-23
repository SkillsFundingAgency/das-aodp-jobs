using static SFA.DAS.AODP.Infrastructure.Repositories.QualificationVersionRepository;

namespace SFA.DAS.AODP.Infrastructure.Interfaces
{
    public interface IQualificationVersionRepository
    {
        Task<List<QualificationLookupItem>> GetLatestQualificationVersionSnapshotsAsync();
    }
}
