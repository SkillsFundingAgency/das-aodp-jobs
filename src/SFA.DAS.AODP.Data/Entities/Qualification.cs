using System.ComponentModel.DataAnnotations.Schema;

namespace SFA.DAS.AODP.Data.Entities;

public partial class Qualification
{
    [Column("id")]
    public int Id { get; set; }

    [Column("qan")]
    public string Qan { get; set; } = null!;

    [Column("qualification_name")]
    public string? QualificationName { get; set; }

    public virtual ICollection<Qualifications> Qualifications { get; set; } = new List<Qualifications>();

    public virtual ICollection<QualificationDiscussionHistory> QualificationDiscussionHistories { get; set; } = new List<QualificationDiscussionHistory>();

    public virtual ICollection<QualificationVersion> QualificationVersions { get; set; } = new List<QualificationVersion>();
}
