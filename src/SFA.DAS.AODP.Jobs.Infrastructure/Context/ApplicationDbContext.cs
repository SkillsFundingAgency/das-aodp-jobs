using Microsoft.EntityFrameworkCore;
using SFA.DAS.AODP.Data.Entities;
using Z.BulkOperations;

namespace SFA.DAS.AODP.Infrastructure.Context
{
    public class ApplicationDbContext : DbContext, IApplicationDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public virtual DbSet<FundedQualificationsImport> FundedQualificationsImport { get; set; }

        public virtual DbSet<ProcessedRegisteredQualification> ProcessedRegisteredQualifications { get; set; }

        public virtual DbSet<RegisteredQualificationsImport> RegisteredQualificationsImport { get; set; }


        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return base.SaveChangesAsync(cancellationToken);
        }

        public async Task BulkInsertAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : class
        {
            await this.BulkInsertAsync(entities.ToList(), options => { options.BatchSize = 1000; options.InsertIfNotExists = false;options.AutoMapOutputDirection = false; }, cancellationToken: cancellationToken);
        }

        public async Task TruncateTable(string tableName)
        {
            await this.Database.ExecuteSqlRawAsync($"truncate table [{tableName}]");
        }
    }
}

