namespace SFA.DAS.AODP.Data.Entities;

public partial class AwardingOrganisation
{
    public int Id { get; set; }
    
    public int? Ukpm { get; set; }

    public string? RecognitionNumber { get; set; }

    public string? NameLegal { get; set; }

    public string? NameOfqual { get; set; }

    public string? NameGovUk { get; set; }

    public string? Name_Dsi { get; set; }

    public string? Acronym { get; set; }

    public virtual ICollection<Qualifications> Qualifications { get; set; } = new List<Qualifications>();

    public virtual ICollection<QualificationVersions> QualificationVersions { get; set; } = new List<QualificationVersions>();
}
