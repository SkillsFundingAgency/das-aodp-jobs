namespace SAF.DAS.AODP.Models.Qualification
{
    public partial class FundedQualificationDTO
    {
        public int Id { get; set; }
        public DateTime? DateOfOfqualDataSnapshot { get; set; }
        public string? QualificationName { get; set; }
        public string? AwardingOrganisation { get; set; }
        public string? QualificationNumber { get; set; }
        public string? Level { get; set; }
        public string? QualificationType { get; set; }
        public string? Subcategory { get; set; }
        public string? SectorSubjectArea { get; set; }
        public string? Status { get; set; }
        public string? AwardingOrganisationUrl { get; set; }
        public ICollection<FundedQualificationOfferDTO> Offers { get; set; } = new List<FundedQualificationOfferDTO>();
    }
}
