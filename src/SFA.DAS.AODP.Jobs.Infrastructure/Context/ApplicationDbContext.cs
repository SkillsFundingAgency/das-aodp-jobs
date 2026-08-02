using Microsoft.EntityFrameworkCore;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Data.Entities.Rollover;
using SFA.DAS.AODP.Infrastructure.Interfaces.Rollover;


namespace SFA.DAS.AODP.Infrastructure.Context
{
    public class ApplicationDbContext : DbContext, IApplicationDbContext
    {
        private readonly IFundingDomainEventDispatcher? _fundingDomainEventDispatcher;
        private bool _dispatchingFundingDomainEvents;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : this(options, null) { }

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            IFundingDomainEventDispatcher? fundingDomainEventDispatcher)
            : base(options)
        {
            _fundingDomainEventDispatcher = fundingDomainEventDispatcher;
        }

        public virtual DbSet<Application> Applications { get; set; }

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

        public virtual DbSet<RegulatedQaaQualificationHistory> RegulatedQaaQualificationHistory { get; set; }

        public virtual DbSet<QaaQualificationFunding> QaaQualificationFundings { get; set; }

        
        public virtual DbSet<RolloverCandidate> RolloverCandidates { get; set; }

        public virtual DbSet<RolloverWorkflowCandidate> RolloverWorkflowCandidates { get; set; }

        public virtual void StartingBulkInsert() => ChangeTracker.AutoDetectChangesEnabled = false;

        public virtual void FinishedBulkInsert() => ChangeTracker.AutoDetectChangesEnabled = true;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<RegulatedQaaQualification>(entity =>
            {
                entity.HasIndex(q => q.AimCode).IsUnique();
                entity.HasIndex(q => q.DateOfDataSnapshot);
                entity.HasIndex(q => q.FirstSeenAt);
                entity.HasIndex(q => q.LastDateForRegistration);
                entity.HasIndex(q => q.IsDiscontinued);
                entity.HasIndex(q => q.LatestImportComparisonOutcome);
                entity.HasIndex(q => q.LastDateForRegistrationChangeType);
                entity.HasIndex(q => q.DiscontinuedDate);
                entity.HasIndex(q => q.LatestQaaQualificationHistoryId);

                entity.Property(q => q.SectorSubjectArea)
                    .HasConversion(
                        ssaTier => ssaTier.Name,
                        ssaName => SectorSubjectArea.FromName(ssaName));

                entity.Property(q => q.LatestImportComparisonOutcome)
                    .HasConversion<string>()
                    .HasColumnType("nvarchar(50)");

                entity.Property(q => q.LastDateForRegistrationChangeType)
                    .HasConversion<string>()
                    .HasColumnType("nvarchar(50)");
            });

            modelBuilder.Entity<RegulatedQaaQualificationHistory>(entity =>
            {
                entity.HasIndex(q => q.QaaQualificationId);
                entity.HasIndex(q => q.AimCode);
                entity.HasIndex(q => q.ChangedAt);
                entity.HasIndex(q => q.LastDateForRegistrationChangeType);

                entity.Property(q => q.SectorSubjectArea)
                    .HasConversion(
                        ssaTier => ssaTier.Name,
                        ssaName => SectorSubjectArea.FromName(ssaName));

                entity.Property(q => q.LastDateForRegistrationChangeType)
                    .HasConversion<string>()
                    .HasColumnType("nvarchar(50)");
            });

            modelBuilder.Entity<QaaQualificationFunding>(entity =>
            {
                entity.HasIndex(funding => funding.QaaQualificationId);
                entity.HasIndex(funding => new
                    {
                        funding.QaaQualificationId,
                        funding.FundingOfferId
                    })
                    .IsUnique();

                entity.HasOne(funding => funding.QaaQualification)
                    .WithMany(qualification => qualification.Fundings)
                    .HasForeignKey(funding => funding.QaaQualificationId);
            });

            modelBuilder.Entity<RolloverCandidate>(b =>
            {
                b.Property(x => x.RolloverStatus)
                    .HasConversion<string>();

                b.Property(x => x.SourceType)
                    .HasMaxLength(50);

                b.HasIndex(x => new
                    {
                        x.SourceType,
                        x.SourceQualificationId,
                        x.FundingOfferId,
                        x.AcademicYear,
                        x.RolloverRound
                    })
                    .IsUnique();
            });

            modelBuilder.Entity<RolloverWorkflowCandidate>(b =>
            {
                b.Property(x => x.InvalidationReason)
                    .HasMaxLength(255);
            });

        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (_dispatchingFundingDomainEvents || _fundingDomainEventDispatcher is null)
            {
                return await base.SaveChangesAsync(cancellationToken);
            }

            var domainEvents = FundingDomainEventCollector.Collect(ChangeTracker);
            if (domainEvents.Count == 0)
            {
                return await base.SaveChangesAsync(cancellationToken);
            }

            var ownsTransaction = Database.IsRelational() && Database.CurrentTransaction is null;
            await using var transaction = ownsTransaction
                ? await Database.BeginTransactionAsync(cancellationToken)
                : null;

            try
            {
                var affectedRows = await base.SaveChangesAsync(cancellationToken);
                _dispatchingFundingDomainEvents = true;
                await _fundingDomainEventDispatcher.DispatchAsync(
                    this,
                    domainEvents,
                    cancellationToken);
                affectedRows += await base.SaveChangesAsync(cancellationToken);

                if (transaction is not null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }

                FundingDomainEventCollector.Clear(ChangeTracker);
                return affectedRows;
            }
            catch
            {
                if (transaction is not null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }

                throw;
            }
            finally
            {
                _dispatchingFundingDomainEvents = false;
            }
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
