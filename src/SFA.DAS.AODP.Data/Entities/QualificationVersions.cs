using System.ComponentModel.DataAnnotations.Schema;

namespace SFA.DAS.AODP.Data.Entities;

[Table("QualificationVersions", Schema = "regulated")]
public partial class QualificationVersions
{
    public Guid Id { get; set; }

    public Guid QualificationId { get; set; }

    public Guid VersionFieldChangesId { get; set; }

    public Guid ProcessStatusId { get; set; }

    public int AdditionalKeyChangesReceivedFlag { get; set; }

    public Guid LifecycleStageId { get; set; }

    public string? OutcomeJustificationNotes { get; set; }

    public Guid AwardingOrganisationId { get; set; }

    public string Status { get; set; } = null!;

    public string Type { get; set; } = null!;

    public string Ssa { get; set; } = null!;

    public string Level { get; set; } = null!;

    public string SubLevel { get; set; } = null!;

    public string EqfLevel { get; set; } = null!;

    public string? GradingType { get; set; }

    public string? GradingScale { get; set; }

    public int? TotalCredits { get; set; }

    public int? Tqt { get; set; }

    public int? Glh { get; set; }

    public int? MinimumGlh { get; set; }

    public int? MaximumGlh { get; set; }

    public DateTime RegulationStartDate { get; set; }

    public DateTime OperationalStartDate { get; set; }

    public DateTime? OperationalEndDate { get; set; }

    public DateTime? CertificationEndDate { get; set; }

    public DateTime? ReviewDate { get; set; }

    public bool OfferedInEngland { get; set; }

    public bool OfferedInNi { get; set; }

    public bool? OfferedInternationally { get; set; }

    public string? Specialism { get; set; }

    public string? Pathways { get; set; }

    public string? AssessmentMethods { get; set; }

    public string? ApprovedForDelFundedProgramme { get; set; }

    public string? LinkToSpecification { get; set; }

    public string? ApprenticeshipStandardReferenceNumber { get; set; }

    public string? ApprenticeshipStandardTitle { get; set; }

    public bool RegulatedByNorthernIreland { get; set; }

    public string? NiDiscountCode { get; set; }

    public string? GceSizeEquivelence { get; set; }

    public string? GcseSizeEquivelence { get; set; }

    public string? EntitlementFrameworkDesign { get; set; }

    public DateTime LastUpdatedDate { get; set; }

    public DateTime UiLastUpdatedDate { get; set; }

    public DateTime InsertedDate { get; set; }

    public int? Version { get; set; }

    public bool? AppearsOnPublicRegister { get; set; }

    public int? LevelId { get; set; }

    public int? TypeId { get; set; }

    public int? SsaId { get; set; }

    public int? GradingTypeId { get; set; }

    public int? GradingScaleId { get; set; }

    public bool? PreSixteen { get; set; }

    public bool? SixteenToEighteen { get; set; }

    public bool? EighteenPlus { get; set; }

    public bool? NineteenPlus { get; set; }

    public string? ImportStatus { get; set; }

    public virtual LifecycleStage LifecycleStage { get; set; } = null!;

    public virtual AwardingOrganisation Organisation { get; set; } = null!;

    public virtual ProcessStatus ProcessStatus { get; set; } = null!;

    public virtual Qualification Qualification { get; set; } = null!;

    public virtual VersionFieldChange VersionFieldChanges { get; set; } = null!;
}
