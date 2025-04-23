using System.ComponentModel.DataAnnotations.Schema;

namespace SFA.DAS.AODP.Data.Entities;

[Table("QualificationFundingFeedbacks", Schema = "funded")]
public class QualificationFundingFeedback
{
    public Guid Id { get; set; }
    public Guid QualificationVersionId { get; set; }
    public bool? Approved { get; set; }
    public string? Comments { get; set; }

    public virtual QualificationVersions QualificationVersion { get; set; }
}
