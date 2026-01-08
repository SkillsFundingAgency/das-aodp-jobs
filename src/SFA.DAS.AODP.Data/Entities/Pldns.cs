using System.ComponentModel.DataAnnotations.Schema;

namespace SFA.DAS.AODP.Data.Entities;

[Table("Pldns")]
public partial class Pldns
{
    public int Id { get; set; }

    [Column("QAN")]
    public string Qan { get; set; } = null!;

    [Column("ListUpdatedDate")]
    public DateTime? ListUpdatedDate { get; set; }

    [Column("Notes")]
    public string? Notes { get; set; }

    [Column("PLDNS14-16")]
    public DateTime? Pldns14To16 { get; set; }

    [Column("Notes_14-16")]
    public string? Pldns14To16Note { get; set; }

    [Column("PLDNS16-19")]
    public DateTime? Pldns16To19 { get; set; }

    [Column("Notes16-19")]
    public string? Pldns16To19Note { get; set; }

    [Column("LocalFlex")]
    public DateTime? LocalFlex { get; set; }

    [Column("NotesLocalFlex")]
    public string? LocalFlexNote { get; set; }

    [Column("LegalEntitlementL2-L3")]
    public DateTime? LegalEntitlementL2L3 { get; set; }

    [Column("NotesLegalEntitlementL2-L3")]
    public string? LegalEntitlementL2L3Note { get; set; }

    [Column("LegalEntitlementEngMaths")]
    public DateTime? LegalEntitlementEngMaths { get; set; }

    [Column("NotesLegalEntitlementEngMaths")]
    public string? LegalEntitlementEngMathsNote { get; set; }

    [Column("DigitalEntitlement")]
    public DateTime? DigitalEntitlement { get; set; }

    [Column("NotesDigitalEntitlement")]
    public string? DigitalEntitlementNote { get; set; }

    [Column("ESF-L3-L4")]
    public DateTime? EsfL3L4 { get; set; }

    [Column("NotesESF-L3-L4")]
    public string? EsfL3L4Note { get; set; }

    [Column("Loans")]
    public DateTime? Loans { get; set; }

    [Column("NotesLoans")]
    public string? LoansNote { get; set; }

    [Column("LifelongLearningEntitlement")]
    public DateTime? LifelongLearning { get; set; }

    [Column("NotesLifelongLearningEntitlement")]
    public string? LifelongLearningNote { get; set; }

    [Column("Level3FreeCoursesForJobs")]
    public DateTime? Level3FCoursesForJobs { get; set; }

    [Column("NotesLevel 3FreeCoursesForJobs")]
    public string? Level3FCoursesForJobsNote { get; set; }

    [Column("CoF")]
    public DateTime? Cof { get; set; }

    [Column("NotesCoF")]
    public string? CofNote { get; set; }

    [Column("StartDate")]
    public DateTime? StartDate { get; set; }

    [Column("NotesStartDate")]
    public string? StartDateNote { get; set; }

    [Column("ImportDate")]
    public DateTime ImportDate { get; set; }
}
