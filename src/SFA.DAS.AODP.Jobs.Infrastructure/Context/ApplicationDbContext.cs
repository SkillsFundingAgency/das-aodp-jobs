using Microsoft.EntityFrameworkCore;
using SFA.DAS.AODP.Data.Entities;

namespace SFA.DAS.AODP.Infrastructure.Context
{
    public class ApplicationDbContext : DbContext, IApplicationDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public virtual DbSet<ApprovedQualificationsImport> ApprovedQualificationsImport { get; set; }

        public virtual DbSet<ProcessedRegisteredQualification> ProcessedRegisteredQualifications { get; set; }

        public virtual DbSet<RegisteredQualificationsImport> RegisteredQualificationsImport { get; set; }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return base.SaveChangesAsync(cancellationToken);
        }

        public async Task BulkInsertAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : class
        {
            await this.BulkInsertAsync(entities.ToList(), options => options.BatchSize = 1000, cancellationToken: cancellationToken);
        }

    }
}

