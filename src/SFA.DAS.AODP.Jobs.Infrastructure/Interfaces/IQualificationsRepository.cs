namespace SFA.DAS.AODP.Infrastructure.Interfaces
{
    public interface IQualificationsRepository
    {
        Task<List<AwardingOrganisation>> GetAwardingOrganisationsAsync();
        Task<List<Qualification>> GetQualificationsAsync();
        Task TruncateFundingTables();
    }
}