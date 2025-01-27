using Microsoft.EntityFrameworkCore;
using SFA.DAS.AODP.Data.Entities;

namespace SFA.DAS.AODP.Infrastructure.Context
{
    public interface IApplicationDbContext
    {
        DbSet<FundedQualification> FundedQualifications { get; set; }
        DbSet<ProcessedRegulatedQualification> ProcessedRegulatedQualifications { get; set; }
        DbSet<RegulatedQualificationsImport> RegulatedQualificationsImport { get; set; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task BulkInsertAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : class;
        Task DeleteTable<T>() where T : class;
    }
}
