namespace SFA.DAS.AODP.Infrastructure.Repositories;

public class QualificationsRepository(IApplicationDbContext context, ILogger<QualificationsRepository> logger)
    : IQualificationsRepository
{
    public async Task<List<Qualification>> GetQualificationsAsync()
    {
        var qualifications = new List<Qualification>();

        try
        {
            qualifications = await context.Qualification
                .AsNoTracking()
                .ToListAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error while retrieving Qualifications: {ex.Message}");
        }

        return qualifications;
    }

    public async Task<List<AwardingOrganisation>> GetAwardingOrganisationsAsync()
    {
        var organisations = new List<AwardingOrganisation>();

        try
        {
            organisations = await context.AwardingOrganisation
                .AsNoTracking()
                .OrderByDescending(o => o.RecognitionNumber)
                .GroupBy(o => o.NameOfqual)
                .Select(g => g.First())
                .ToListAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error while retrieving AwardingOrganisations: {ex.Message}");
        }

        return organisations;
    }

    public async Task TruncateFundingTables()
    {
        try
        {
            await context.Truncate_FundedQualifications();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error while truncating Funding Tables: {ex.Message}");
        }
    }
}