using System.Diagnostics.CodeAnalysis;

namespace SFA.DAS.AODP.Common.Enum;

[ExcludeFromCodeCoverage]
public sealed record QualificationTitleRules(
    IReadOnlyCollection<QualificationTitle> Title,
    IReadOnlyCollection<QualificationTitle> Abbreviations);