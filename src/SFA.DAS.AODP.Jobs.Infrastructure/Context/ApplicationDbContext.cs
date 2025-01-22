using Microsoft.EntityFrameworkCore;
using SFA.DAS.AODP.Data.Entities;

namespace SFA.DAS.AODP.Infrastructure.Context
{
    public class ApplicationDbContext : DbContext, IApplicationDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) {
            Database.SetCommandTimeout(TimeSpan.FromMinutes(20));
        }
        
        public virtual DbSet<FundedQualification> FundedQualifications { get; set; }

        public virtual DbSet<FundedQualificationOffer> FundedQualificationOffers { get; set; }

        public virtual DbSet<ProcessedRegisteredQualification> ProcessedRegisteredQualifications { get; set; }

        public virtual DbSet<RegisteredQualificationsImport> RegisteredQualificationsImport { get; set; }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return base.SaveChangesAsync(cancellationToken);
        }

        public async Task BulkInsertAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : class
        {
            if (entities.Any())
            await this.BulkInsertAsync(entities.Take(100), options => { options.IncludeGraph = true;options.BatchSize = 1000;options.InsertIfNotExists = false; }, cancellationToken: cancellationToken);
        }

        public async Task DeleteFromTable(string tableName)
        {
            await this.Database.ExecuteSqlRawAsync($"delete from [{tableName}]");
        }
    }
}

