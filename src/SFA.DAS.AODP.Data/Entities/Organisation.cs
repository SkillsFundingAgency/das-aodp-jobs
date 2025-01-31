using System.ComponentModel.DataAnnotations.Schema;

namespace SFA.DAS.AODP.Data.Entities;

public partial class Organisation
{
    [Column("id")]
    public int Id { get; set; }

    [Column("recognition_number")]
    public string? RecognitionNumber { get; set; }

    [Column("name")]
    public string? Name { get; set; }

    [Column("acronym")]
    public string? Acronym { get; set; }

    public virtual ICollection<Qualifications> Qualifications { get; set; } = new List<Qualifications>();

    public virtual ICollection<QualificationVersion> QualificationVersions { get; set; } = new List<QualificationVersion>();
}
