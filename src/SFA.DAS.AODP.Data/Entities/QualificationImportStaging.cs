namespace SFA.DAS.AODP.Data.Entities;

public partial class QualificationImportStaging
{
    public Guid Id { get; set; }

    public string? JsonData { get; set; }

    public DateTime? CreatedDate { get; set; }
}