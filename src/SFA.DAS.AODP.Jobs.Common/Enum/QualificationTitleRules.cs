namespace SFA.DAS.AODP.Common.Enum;

public sealed record QualificationTitleRules(
    IReadOnlyCollection<QualificationTitle> Title,
    IReadOnlyCollection<QualificationTitle> Abbreviations);