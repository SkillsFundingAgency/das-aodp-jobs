using System.ComponentModel.DataAnnotations.Schema;

namespace SFA.DAS.AODP.Data.Entities;

[Table("QualificationOffer", Schema = "funded")]

public partial class QualificationOffer
{
    public Guid Id { get; set; }

    public Guid QualificationId { get; set; }

    public string? Name { get; set; }

    public string? Notes { get; set; }

    public bool? FundingAvailable { get; set; }

    public DateTime? FundingApprovalStartDate { get; set; }

    public DateTime? FundingApprovalEndDate { get; set; }

    public virtual Qualifications Qualification { get; set; } = null!;
}
