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
        
        public virtual DbSet<RegulatedQaaQualification> RegulatedQaaQualification { get; set; }

        public virtual DbSet<RegulatedQaaQualificationVersion> RegulatedQaaQualificationVersion { get; set; }

        public virtual void StartingBulkInsert() => ChangeTracker.AutoDetectChangesEnabled = false;

        public virtual void FinishedBulkInsert() => ChangeTracker.AutoDetectChangesEnabled = true;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<RegulatedQaaQualification>(entity =>
            {
                entity.HasIndex(q => q.AimCode).IsUnique();
                entity.HasIndex(q => q.ChangeVersion);
                entity.HasIndex(q => q.DateOfDataSnapshot);
                entity.HasIndex(q => q.LastDateForRegistration);
                entity.HasIndex(q => q.IsDiscontinued);
                entity.HasIndex(q => q.LatestImportComparisonOutcome);
                entity.HasIndex(q => q.PublicationStatus);
                entity.HasIndex(q => q.LastDateForRegistrationChangeType);
                entity.HasIndex(q => q.IsRegistrationDateExtended);
                entity.HasIndex(q => q.IsRegistrationDateBroughtForward);
                entity.HasIndex(q => q.DiscontinuedDate);

                entity.Property(q => q.SectorSubjectArea)
                    .HasConversion(
                        ssaTier => ssaTier.Name,
                        ssaName => SectorSubjectArea.FromName(ssaName));
            });

            modelBuilder.Entity<RegulatedQaaQualificationVersion>(entity =>
            {
                entity.HasIndex(q => q.QaaQualificationId);
                entity.HasIndex(q => q.AimCode);
                entity.HasIndex(q => q.ChangeVersion);
                entity.HasIndex(q => q.ChangedAt);
                entity.HasIndex(q => q.LastDateForRegistrationChangeType);

                entity.Property(q => q.SectorSubjectArea)
                    .HasConversion(
                        ssaTier => ssaTier.Name,
                        ssaName => SectorSubjectArea.FromName(ssaName));
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
