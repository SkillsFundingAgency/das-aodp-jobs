using Microsoft.EntityFrameworkCore;
using SFA.DAS.AODP.Data.Entities;


namespace SFA.DAS.AODP.Infrastructure.Context
{
    public class ApplicationDbContext : DbContext, IApplicationDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public virtual DbSet<ActionType> ActionType { get; set; }

        public virtual DbSet<LifecycleStage> LifecycleStages { get; set; }

        public virtual DbSet<AwardingOrganisation> AwardingOrganisation { get; set; }

        public virtual DbSet<ProcessStatus> ProcessStatus { get; set; }

        public virtual DbSet<Qualification> Qualification { get; set; }

        public virtual DbSet<Qualifications> FundedQualifications { get; set; }

        public virtual DbSet<QualificationDiscussionHistory> QualificationDiscussionHistory { get; set; }

        public virtual DbSet<QualificationOffer> QualificationOffers { get; set; }

        public virtual DbSet<QualificationVersions> QualificationVersions { get; set; }

        public virtual DbSet<QualificationImportStaging> QualificationImportStaging { get; set; }

        public virtual DbSet<VersionFieldChanges> VersionFieldChanges { get; set; }

        public virtual DbSet<Job> Jobs { get; set; }

        public virtual DbSet<JobConfiguration> JobConfigurations { get; set; }

        public virtual DbSet<JobRun> JobRuns { get; set; }

        public virtual DbSet<QualificationFunding> QualificationFundings { get; set; }

        public virtual DbSet<FundingOffer> FundingOffers { get; set; }

        public virtual DbSet<QualificationFundingFeedback> QualificationFundingFeedbacks { get; set; }

        public virtual DbSet<Pldns> Pldns { get; set; }
        public virtual DbSet<DefundingList> DefundingLists { get; set; }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return base.SaveChangesAsync(cancellationToken);
        }

        public async Task Truncate_FundedQualifications()
        {
            await this.Database.ExecuteSqlRawAsync($"EXEC [dbo].[Truncate_Funded_Qualifications]");            
        }

        public async Task Truncate_QualificationImportStaging()
        {
            await this.Database.ExecuteSqlRawAsync($"EXEC [dbo].[Truncate_QualificationImportStaging]");
        }

        public async Task DeleteDuplicateAsync(string sql, CancellationToken cancellationToken = default)
        {
            await this.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        }
    }
}