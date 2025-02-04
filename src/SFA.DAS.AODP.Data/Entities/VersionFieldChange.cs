namespace SFA.DAS.AODP.Data.Entities;

public partial class VersionFieldChange
{
    public int Id { get; set; }

    public int? QanId { get; set; } // not needed for now

    public int? QualificationVersionNumber { get; set; }

    public string? ChangedFieldNames { get; set; }

    public virtual ICollection<QualificationVersion> QualificationVersions { get; set; } = new List<QualificationVersion>();
}
