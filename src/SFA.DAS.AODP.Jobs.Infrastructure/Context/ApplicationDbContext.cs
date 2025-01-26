using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SFA.DAS.AODP.Data.Entities;
using System.Data;
using Z.BulkOperations;

namespace SFA.DAS.AODP.Infrastructure.Context
{
    public class ApplicationDbContext : DbContext, IApplicationDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public virtual DbSet<FundedQualification> FundedQualifications { get; set; }
        public virtual DbSet<FundedQualificationOffer> FundedQualificationOffers { get; set; }

        public virtual DbSet<ProcessedRegulatedQualification> ProcessedRegulatedQualifications { get; set; }

        public virtual DbSet<RegulatedQualificationsImport> RegulatedQualificationsImport { get; set; }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return base.SaveChangesAsync(cancellationToken);
        }

        public async Task BulkInsertAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : class
        {
            if(entities.Any())
            await this.BulkInsertAsync(entities.ToList(), options => { options.BatchSize = 1000; options.IncludeGraph = true; }, cancellationToken: cancellationToken);
        }

        public async Task DeleteTable<T>() where T:class
        {
            await this.Set<T>().ExecuteDeleteAsync();
        }
    }
}

