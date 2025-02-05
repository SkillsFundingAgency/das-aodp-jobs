using System.ComponentModel.DataAnnotations.Schema;

namespace SFA.DAS.AODP.Data.Entities;

public partial class Organisation
{
    public int Id { get; set; }

    public string? RecognitionNumber { get; set; }

    public string? Name { get; set; }

    public string? Acronym { get; set; }

    public virtual ICollection<Qualifications> Qualifications { get; set; } = new List<Qualifications>();

    public virtual ICollection<QualificationVersions> QualificationVersions { get; set; } = new List<QualificationVersions>();
}
