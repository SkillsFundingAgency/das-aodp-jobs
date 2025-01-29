namespace SFA.DAS.AODP.Data.Entities;

public partial class ProcessStatus
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public int? IsOutcomeDecision { get; set; }

    public virtual ICollection<QualificationVersion> QualificationVersions { get; set; } = new List<QualificationVersion>();
}

