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

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return base.SaveChangesAsync(cancellationToken);
        }

        public async Task TruncateTable<T>(string? schema = null) where T : class
        {
            var entityType = this.Model.FindEntityType(typeof(T));
            var tableName = entityType?.GetTableName();
            if (!string.IsNullOrEmpty(tableName))
            {
                if (!string.IsNullOrEmpty(schema) && IsValidSqlIdentifier(schema) && IsValidSqlIdentifier(tableName))
                {
                    var formattedTableName = $"[{schema}].[{tableName}]";
                    await this.Database.ExecuteSqlRawAsync($"DELETE FROM {formattedTableName}");
                }
                else if (IsValidSqlIdentifier(tableName))
                {
                    await this.Database.ExecuteSqlRawAsync($"DELETE FROM [{tableName}]");
                }
                else
                {
                    throw new ArgumentException("Invalid schema or table name");
                }
            }
        }

        private bool IsValidSqlIdentifier(string identifier)
        {
            return !string.IsNullOrEmpty(identifier) &&
                   System.Text.RegularExpressions.Regex.IsMatch(identifier, @"^[a-zA-Z0-9_]+$");
        }
    }
}