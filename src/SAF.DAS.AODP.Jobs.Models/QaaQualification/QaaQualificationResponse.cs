using System.Text.Json.Serialization;

namespace SFA.DAS.AODP.Models.QaaQualification;

/// <summary>
/// The details of a QAA education qualification (diploma) as retrieved from the API.
/// </summary>
public record QaaQualificationResponse
{
    // TODO[HS]: Need to check with QAA what they define as a duplicate diploma, is it just the AIM Code or is it a combination of other fields? As from my own understanding of other similar qualification data from other sources the AIM Code can be the same for different qualifications if they are different versions or types across different awarding bodies.

    /// <summary>
    /// National Learning Aim identifier for this Access to Higher Education Diploma.
    /// Used by funding/ILR/MIS systems as the definitive unique key for the qualification
    /// (i.e., unique even across different awarding bodies/AVAs).
    /// </summary>
    [JsonPropertyName("AIM_code")]
    public string AimCode { get; set; } = null!;

    /// <summary>
    /// The organisation that designs, quality‑assures, and awards this Diploma
    /// (often called the awarding body or AVA in the Access to Higher Education context).
    /// </summary>
    [JsonPropertyName("Awarding_Body")]
    public string AwardingBody { get; set; } = null!;

    /// <summary>
    /// The official, QAA‑recognised title of the Diploma (e.g. "Access to Higher Education Diploma (Science)").
    /// Together with the awarding body, this describes the academic focus and intended progression.
    /// </summary>
    [JsonPropertyName("Diploma_Title")]
    public string DiplomaTitle { get; set; } = null!;

    /// <summary>
    /// Sector Subject Area (SSA) Tier 1 classification, indicating the broad subject sector
    /// for funding, analytics, and curriculum mapping (e.g., "Science and Mathematics").
    /// </summary>
    [JsonPropertyName("SSA_Tier_1")]
    public string SsaTier1 { get; set; } = null!;

    /// <summary>
    /// Sector Subject Area (SSA) Tier 2 classification, providing a more granular subject category
    /// within the Tier 1 area (e.g., "Mathematics" vs "Biology") to refine reporting and search.
    /// </summary>
    [JsonPropertyName("SSA_Tier_2")]
    public string SsaTier2 { get; set; } = null!;

    /// <summary>
    /// The first date on which learners can be registered against this qualification specification.
    /// Often aligns with a new specification cycle or funding year.
    /// </summary>
    [JsonPropertyName("Start_date_of_qualification")]
    public DateTime StartDateOfQualification { get; set; }

    /// <summary>
    /// The last date providers are permitted to register new learners on this qualification.
    /// After this date, only existing learners can continue towards certification.
    /// </summary>
    [JsonPropertyName("Last_date_for_registrations")]
    public DateTime LastDateForRegistrations { get; set; }

    /// <summary>
    /// The final date by which certifications must be claimed for learners registered on this qualification.
    /// Past this date, no further awards can be issued under this specification.
    /// </summary>
    [JsonPropertyName("Last_date_for_certifications")]
    public DateTime LastDateForCertifications { get; set; }

    /// <summary>
    /// Current lifecycle state of the qualification (e.g., Active, Review, Withdrawn).
    /// Helps determine whether the Diploma is available for delivery, funding, or certification.
    /// </summary>
    [JsonPropertyName("award_status")]
    public string AwardStatus { get; set; } = null!;

    /// <summary>
    /// The date the qualification was formally discontinued/withdrawn (if applicable).
    /// </summary>
    [JsonPropertyName("Discontinued_date")]
    public DateTime? DiscontinuedDate { get; set; }
}