namespace SFA.DAS.AODP.Common.Enum;

public record QualificationType(string Value)
{
    public static readonly QualificationType EndPointAssessment = new("End-Point Assessment");

    public static readonly QualificationType ApprenticeshipAssessmentQualification = new("Apprenticeship Assessment Qualification");

    private static readonly IReadOnlyCollection<QualificationType> IneligibleTypes =
    [
        EndPointAssessment,
        ApprenticeshipAssessmentQualification
    ];

    public static bool IsIneligible(string? type) => IneligibleTypes.Any(x => string.Equals(x.Value, type, StringComparison.OrdinalIgnoreCase));
}