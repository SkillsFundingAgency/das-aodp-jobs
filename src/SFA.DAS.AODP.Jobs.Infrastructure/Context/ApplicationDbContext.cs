using Microsoft.EntityFrameworkCore;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Data.Entities.Rollover;


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
        
        public virtual DbSet<RegulatedQaaQualification> RegulatedQaaQualification { get; set; }

        public virtual DbSet<QaaQualificationFunding> QaaQualificationFundings { get; set; }
        
        public virtual DbSet<RolloverCandidate> RolloverCandidates { get; set; }

        public virtual void StartingBulkInsert() => ChangeTracker.AutoDetectChangesEnabled = false;

        public virtual void FinishedBulkInsert() => ChangeTracker.AutoDetectChangesEnabled = true;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<RegulatedQaaQualification>()
                .Property(q => q.SectorSubjectArea)
                .HasConversion(
                    ssaTier => ssaTier.Name,
                    ssaName => SectorSubjectArea.FromName(ssaName));

            modelBuilder.Entity<RolloverCandidate>(b =>
            {
                b.Property(x => x.RolloverStatus)
                    .HasConversion<string>();

                b.HasIndex(x => new { x.SourceType, x.SourceQualificationId, x.FundingOfferId, x.AcademicYear, x.RolloverRound })
                    .IsUnique();
            });
        }

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
