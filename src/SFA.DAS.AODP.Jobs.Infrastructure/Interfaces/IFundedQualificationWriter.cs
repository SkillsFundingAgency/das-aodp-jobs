namespace SFA.DAS.AODP.Infrastructure.Interfaces
{
    public interface IFundedQualificationWriter
    {
        Task<bool> WriteQualifications(List<FundedQualificationDTO> qualifications);
        Task<bool> SeedFundingData();
    }
}