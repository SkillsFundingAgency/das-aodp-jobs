using System.ComponentModel.DataAnnotations.Schema;

namespace SFA.DAS.AODP.Data.Entities;

public partial class QualificationVersion
{
    [Column("id")]
    public int Id { get; set; }

    [Column("qualification)id")]
    public int QualificationId { get; set; }

    [Column("version_field_changes_id")]
    public int VersionFieldChangesId { get; set; }

    [Column("process_status_id")]
    public int ProcessStatusId { get; set; }

    [Column("additional_key_changes_recieved_flag")]
    public int AdditionalKeyChangesReceivedFlag { get; set; }

    [Column("lifecycle_stage_id")]
    public int LifecycleStageId { get; set; }

    [Column("outcome_justification_notes")]
    public string? OutcomeJustificationNotes { get; set; }

    [Column("organisation_id")]
    public int OrganisationId { get; set; }

    [Column("status")]
    public string Status { get; set; } = null!;

    [Column("type")]
    public string Type { get; set; } = null!;

    [Column("ssa")]
    public string Ssa { get; set; } = null!;

    [Column("level")]
    public string Level { get; set; } = null!;

    [Column("sub_level")]
    public string SubLevel { get; set; } = null!;

    [Column("efq_level")]
    public string EqfLevel { get; set; } = null!;

    [Column("grading_type")]
    public string? GradingType { get; set; }

    [Column("grading_scale")]
    public string? GradingScale { get; set; }

    [Column("total_credits")]
    public int? TotalCredits { get; set; }

    [Column("tqt")]
    public int? Tqt { get; set; }

    [Column("glh")]
    public int? Glh { get; set; }

    [Column("minimum_glh")]
    public int? MinimumGlh { get; set; }

    [Column("maximum_glh")]
    public int? MaximumGlh { get; set; }

    [Column("regulation_start_date")]
    public DateTime RegulationStartDate { get; set; }

    [Column("operational_start_date")]
    public DateTime OperationalStartDate { get; set; }

    [Column("operational_end_date")]
    public DateTime? OperationalEndDate { get; set; }

    [Column("certification_end_date")]
    public DateTime? CertificationEndDate { get; set; }

    [Column("review_date")]
    public DateTime? ReviewDate { get; set; }

    [Column("offered_in_england")]
    public bool OfferedInEngland { get; set; }

    [Column("offered_in_ni")]
    public bool OfferedInNi { get; set; }

    [Column("offered_internationally")]
    public bool? OfferedInternationally { get; set; }

    [Column("specialism")]
    public string? Specialism { get; set; }

    [Column("pathways")]
    public string? Pathways { get; set; }

    [Column("assessment_methods")]
    public string? AssessmentMethods { get; set; }

    [Column("approved_for_del_funded_programme")]
    public string? ApprovedForDelFundedProgramme { get; set; }

    [Column("link_to_specification")]
    public string? LinkToSpecification { get; set; }

    [Column("apprenticeship_standard_reference_number")]
    public string? ApprenticeshipStandardReferenceNumber { get; set; }

    [Column("apprenticeship_standard_title")]
    public string? ApprenticeshipStandardTitle { get; set; }

    [Column("regulated_by_northern_ireland")]
    public bool RegulatedByNorthernIreland { get; set; }

    [Column("ni_discount_code")]
    public string? NiDiscountCode { get; set; }

    [Column("gce_size_equivelence")]
    public string? GceSizeEquivelence { get; set; }

    [Column("gcse_size_equivelence")]
    public string? GcseSizeEquivelence { get; set; }

    [Column("entitlement_framework_design")]
    public string? EntitlementFrameworkDesign { get; set; }

    [Column("last_updated_date")]
    public DateTime LastUpdatedDate { get; set; }

    [Column("ui_last_updated_date")]
    public DateTime UiLastUpdatedDate { get; set; }

    [Column("inserted_date")]
    public DateTime InsertedDate { get; set; }

    [Column("version")]
    public int? Version { get; set; }

    [Column("appears_on_public_register")]
    public bool? AppearsOnPublicRegister { get; set; }

    [Column("level_id")]
    public int? LevelId { get; set; }

    [Column("type_id")]
    public int? TypeId { get; set; }

    [Column("ssa_id")]
    public int? SsaId { get; set; }

    [Column("grading_type_id")]
    public int? GradingTypeId { get; set; }

    [Column("grading_scale_id")]
    public int? GradingScaleId { get; set; }

    [Column("pre_sixteen")]
    public bool? PreSixteen { get; set; }

    [Column("sixteen_to_eighteen")]
    public bool? SixteenToEighteen { get; set; }

    [Column("eighteen_plus")]
    public bool? EighteenPlus { get; set; }

    [Column("nineteen_plus")]
    public bool? NineteenPlus { get; set; }

    [Column("import_status")]
    public string? ImportStatus { get; set; }

    public virtual LifecycleStage LifecycleStage { get; set; } = null!;

    public virtual Organisation Organisation { get; set; } = null!;

    public virtual ProcessStatus ProcessStatus { get; set; } = null!;

    public virtual Qualification Qualification { get; set; } = null!;

    public virtual VersionFieldChange VersionFieldChanges { get; set; } = null!;
}
