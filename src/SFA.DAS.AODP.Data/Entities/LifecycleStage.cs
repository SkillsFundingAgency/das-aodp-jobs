namespace SFA.DAS.AODP.Data.Entities;

public partial class LifecycleStage
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public virtual ICollection<QualificationVersion> QualificationVersions { get; set; } = new List<QualificationVersion>();
}
