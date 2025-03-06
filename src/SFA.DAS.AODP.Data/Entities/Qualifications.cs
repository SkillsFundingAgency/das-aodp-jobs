using System.ComponentModel.DataAnnotations.Schema;

namespace SFA.DAS.AODP.Data.Entities;

[Table("Qualifications", Schema = "funded")]
public partial class Qualifications
{
    public Guid Id { get; set; }

    public DateTime? DateOfOfqualDataSnapshot { get; set; }

    public Guid QualificationId { get; set; }

    public Guid AwardingOrganisationId { get; set; }

    public string? Level { get; set; }

    public string? QualificationType { get; set; }

    public string? SubCategory { get; set; }

    public string? SectorSubjectArea { get; set; }

    public string? Status { get; set; }

    public string? AwardingOrganisationUrl { get; set; }

    public DateTime ImportDate { get; set; }

    public virtual AwardingOrganisation AwardingOrganisation { get; set; } = null!;

    public virtual Qualification Qualification { get; set; } = null!;

    public virtual ICollection<QualificationOffer> QualificationOffers { get; set; } = new List<QualificationOffer>();
}