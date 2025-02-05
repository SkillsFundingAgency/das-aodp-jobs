namespace SFA.DAS.AODP.Data.Entities;

public partial class Qualifications
{
    public int Id { get; set; }

    public DateTime? DateOfOfqualDataSnapshot { get; set; }

    public int? QualificationId { get; set; }

    public int? AwardingOrganisationId { get; set; }

    public string? Level { get; set; }

    public string? QualificationType { get; set; }

    public string? SubCategory { get; set; }

    public string? SectorSubjectArea { get; set; }

    public string? Status { get; set; }

    public string? AwardingOrganisationUrl { get; set; }

    public DateTime ImportDate { get; set; }

    public virtual AwardingOrganisation? Organisation { get; set; }

    public virtual Qualification? Qualification { get; set; }
}
