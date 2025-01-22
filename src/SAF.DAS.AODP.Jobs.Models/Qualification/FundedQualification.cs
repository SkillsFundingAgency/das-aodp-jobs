﻿namespace SAF.DAS.AODP.Models.Qualification;
public class FundedQualification
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
    public string? AwardingOrganisationURL { get; set; }
    public string? QualificationNumberVarchar { get; set; }
    public ICollection<FundedQualificationOffer> Offers { get; set; } = new List<FundedQualificationOffer>();
}
}