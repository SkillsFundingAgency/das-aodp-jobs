namespace SFA.DAS.AODP.Models.Qualification
{
    public class FundedQualificationOfferDTO
    {
        public Guid Id { get; set; }
        public Guid QualificationId { get; set; }
        public string? Name { get; set; }
        public string? Notes { get; set; }
        public string? FundingAvailable { get; set; }
        public DateTime? FundingApprovalStartDate { get; set; }
        public DateTime? FundingApprovalEndDate { get; set; }
        public FundedQualificationDTO FundedQualifications { get; set; }

    }
}