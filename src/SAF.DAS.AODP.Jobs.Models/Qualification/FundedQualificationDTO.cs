namespace SFA.DAS.AODP.Models.Qualification
{
    public class FundedQualificationDTO
    {
        public Guid Id { get; set; }
        public DateTime? DateOfOfqualDataSnapshot { get; set; }
        public Guid? QualificationId { get; set; }
        public Guid? AwardingOrganisationId { get; set; }
        public string? Level { get; set; }
        public string? QualificationType { get; set; }
        public string? Subcategory { get; set; }
        public string? SectorSubjectArea { get; set; }
        public string? Status { get; set; }
        public string? AwardingOrganisationURL { get; set; }
        public DateTime ImportDate { get; set; } = DateTime.Now;
        public ICollection<FundedQualificationOfferDTO> Offers { get; set; } = new List<FundedQualificationOfferDTO>();
    }
}
