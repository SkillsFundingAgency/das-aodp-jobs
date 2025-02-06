using System.ComponentModel.DataAnnotations.Schema;

namespace SFA.DAS.AODP.Data.Entities;

[Table("ProcessStatus", Schema = "regulated")]
public partial class ProcessStatus
{
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public int? IsOutcomeDecision { get; set; }

    public virtual ICollection<QualificationVersions> QualificationVersions { get; set; } = new List<QualificationVersions>();
}

