using Microsoft.EntityFrameworkCore;
using SFA.DAS.AODP.Data.Entities;

namespace SFA.DAS.AODP.Infrastructure.Context
{
    public interface IApplicationDbContext
    {
        DbSet<ApprovedQualificationsImport> ApprovedQualificationsImport { get; set; }
        DbSet<ProcessedRegisteredQualification> ProcessedRegisteredQualifications { get; set; }
        DbSet<RegisteredQualificationsImport> RegisteredQualificationsImport { get; set; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task BulkInsertAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : class;


    }
}
