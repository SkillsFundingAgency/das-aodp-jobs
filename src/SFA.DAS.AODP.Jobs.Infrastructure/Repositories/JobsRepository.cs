using JobConfiguration = SFA.DAS.AODP.Data.Entities.JobConfiguration;

namespace SFA.DAS.AODP.Infrastructure.Repositories;

public class JobsRepository(IApplicationDbContext context, ILogger<JobsRepository> logger)
    : IJobsRepository
{
    public async Task<List<Job>> GetJobsAsync()
    {
        try
        {
            return await context.Jobs
                .ToListAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error while retrieving jobs: {ex.Message}");
        }

        return new List<Job>();
    }

    public async Task<Job?> GetJobByIdAsync(Guid id)
    {
        try
        {
            var record = await context.Jobs.FirstOrDefaultAsync(v => v.Id == id);
            return record is null ? throw new EntityNotFoundException($"Job record with id {id} not found") : record;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error while retrieving job: {ex.Message}");
        }

        return null;
    }

    public async Task<Job?> GetJobByNameAsync(string name)
    {
        try
        {
            var record = await context.Jobs.FirstOrDefaultAsync(v => v.Name == name);
            return record is null ? throw new EntityNotFoundException($"Job record with name {name} not found") : record;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error while retrieving job: {ex.Message}");
        }

        return null;
    }

    public async Task<bool> UpdateJobAsync(Guid id, DateTime lastRunTime, string status)
    {
        try
        {
            var record = await context.Jobs.FirstOrDefaultAsync(v => v.Id == id);

            if (record != null)
            {
                record.Status = status;
                record.LastRunTime = lastRunTime;

                await context.SaveChangesAsync();
                return true;
            }
            else
            {
                throw new EntityNotFoundException($"Job record with id {id} not found");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error while updating job: {ex.Message}");
        }

        return false;
    }

    public async Task<Guid> InsertJobRunAsync(Guid jobId, string user, DateTime startTime, string status)
    {
        try
        {
            var record = await context.Jobs
                .Include(i => i.JobRuns)
                .FirstOrDefaultAsync(v => v.Id == jobId);

            if (record != null)
            {
                var entity = await context.JobRuns.AddAsync(new JobRun
                {
                    JobId = jobId,
                    StartTime = startTime,
                    User = user,
                    Status = status
                });

                await context.SaveChangesAsync();
                return entity.Entity.Id;
            }
            else
            {
                throw new EntityNotFoundException($"Job record with id {jobId} not found");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error while inserting job run: {ex.Message}");
        }

        return Guid.Empty;
    }

    public async Task<JobRun?> GetJobRunByIdAsync(Guid id)
    {
        try
        {
            var record = await context.JobRuns.FirstOrDefaultAsync(v => v.Id == id);
            return record is null ? throw new EntityNotFoundException($"JobRun record with id {id} not found") : record;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error while retrieving job run: {ex.Message}");
        }

        return null;
    }

    public async Task<JobRun?> GetLastJobRunsAsync(string jobName)
    {
        try
        {
            var record = await context.JobRuns
                .Include(i => i.Job)
                .Where(w => w.Job.Name == jobName)
                .OrderByDescending(o => o.StartTime)
                .FirstOrDefaultAsync();
            return record;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error while retrieving job run: {ex.Message}");
        }

        return null;
    }

    public async Task<bool> UpdateJobRunAsync(Guid id, string user, DateTime stopTime, string status, int recordsProcessed)
    {
        try
        {

            var record = await context.JobRuns.FirstOrDefaultAsync(v => v.Id == id);

            if (record != null)
            {
                record.RecordsProcessed = recordsProcessed;
                record.EndTime = stopTime;
                record.Status = status;
                record.User = user;

                await context.SaveChangesAsync();
                return true;
            }
            else
            {
                throw new EntityNotFoundException($"Jobrun record with id {id} not found");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error while updating job run: {ex.Message}");
        }

        return false;
    }

    public async Task<List<JobConfiguration>> GetJobConfigurationsByIdAsync(Guid jobId)
    {
        try
        {
            var records = await context.JobConfigurations
                .Where(v => v.JobId == jobId)
                .AsNoTracking()
                .ToListAsync();

            return records;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error while retrieving job configurations: {ex.Message}");
        }

        return new List<JobConfiguration>();
    }
}