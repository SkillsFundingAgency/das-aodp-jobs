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

        public virtual DbSet<ActionType> ActionTypes { get; set; }

        public virtual DbSet<LifecycleStage> LifecycleStages { get; set; }

        public virtual DbSet<Organisation> Organisation { get; set; }

        public virtual DbSet<ProcessStatus> ProcessStatuses { get; set; }

        public virtual DbSet<Qualification> Qualification { get; set; }

        public virtual DbSet<Qualifications> FundedQualifications { get; set; }

        public virtual DbSet<QualificationDiscussionHistory> QualificationDiscussionHistories { get; set; }

        public virtual DbSet<QualificationOffer> QualificationOffers { get; set; }

        public virtual DbSet<QualificationVersion> QualificationVersions { get; set; }

        public virtual DbSet<StagedQualifications> StagedQualifications { get; set; }

        public virtual DbSet<VersionFieldChange> VersionFieldChanges { get; set; }

        public virtual DbSet<RegulatedQualificationsImportStaging> RegulatedQualificationsImportStaging { get; set; }

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

