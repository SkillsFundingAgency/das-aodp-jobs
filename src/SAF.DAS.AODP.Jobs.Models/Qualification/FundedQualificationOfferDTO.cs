namespace SAF.DAS.AODP.Models.Qualification
{
    public class FundedQualificationOfferDTO
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Notes { get; set; }
        public string? FundingAvailable { get; set; }
        public DateTime? FundingApprovalStartDate { get; set; }
        public DateTime? FundingApprovalEndDate { get; set; }
        public int FundedQualificationId { get; set; }
        public FundedQualificationDTO FundedQualifications { get; set; }

    }
}