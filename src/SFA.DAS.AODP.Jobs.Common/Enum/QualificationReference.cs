using System.Text.RegularExpressions;

namespace SFA.DAS.AODP.Common.Enum;

public static class QualificationReference
{
    public static bool IsIneligibleType(string? type) =>
        QualificationType.IsIneligible(type);

    public static bool HasIneligibleTitle(string? level, string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return false;
        }

        var qualificationLevel = QualificationLevel.TryFromString(level);
        if (qualificationLevel is null)
        {
            return Contains(title, CommonIneligibleTitles);
        }

        return HasIneligibleTitle(qualificationLevel, title);
    }

    public static bool HasIneligibleTitle(QualificationLevel level, string title)
    {
        var trimmedTitle = title.Trim();

        if (Contains(trimmedTitle, CommonIneligibleTitles))
        {
            return true;
        }

        if (!RulesByLevel.TryGetValue(level, out var rules))
        {
            return false;
        }

        return Contains(trimmedTitle, rules.Title.Select(x => x.Value)) ||
               ContainsWholeWord(trimmedTitle, rules.Abbreviations.Select(x => x.Value));
    }

    private static readonly IReadOnlyCollection<string> CommonIneligibleTitles =
    [
        QualificationTitle.EsolInternational.Value
    ];

    private static readonly IReadOnlyDictionary<QualificationLevel, QualificationTitleRules> RulesByLevel =
        new Dictionary<QualificationLevel, QualificationTitleRules>
        {
            [QualificationLevel.Level8] = new(
                Title: [QualificationTitle.Doctor],
                Abbreviations:
                [
                    QualificationTitle.PhD,
                    QualificationTitle.EngD
                ]),

            [QualificationLevel.Level7] = new(
                Title:
                [
                    QualificationTitle.Master,
                    QualificationTitle.PostgraduateCertificateInEducation,
                    QualificationTitle.PostgraduateDiplomaInEducation
                ],
                Abbreviations:
                [
                    QualificationTitle.MPhil,
                    QualificationTitle.MSc,
                    QualificationTitle.MA,
                    QualificationTitle.MBA,
                    QualificationTitle.MDes,
                    QualificationTitle.MRes,
                    QualificationTitle.PGCE,
                    QualificationTitle.PGDE
                ]),

            [QualificationLevel.Level6] = new(
                Title:
                [
                    QualificationTitle.Degree,
                    QualificationTitle.ProfessionalGraduateCertificateInEducation,
                    QualificationTitle.ProfessionalGraduateDiplomaInEducation
                ],
                Abbreviations:
                [
                    QualificationTitle.BA,
                    QualificationTitle.BSc,
                    QualificationTitle.BEd,
                    QualificationTitle.BEng,
                    QualificationTitle.BTech,
                    QualificationTitle.PgCE,
                    QualificationTitle.PgDE
                ]),

            [QualificationLevel.Level5] = new(
                Title:
                [
                    QualificationTitle.FoundationDegree,
                    QualificationTitle.HigherNationalDiploma,
                    QualificationTitle.DiplomaOfHigherEducation,
                    QualificationTitle.DiplomaInTeachingFurtherEducationAndSkills,
                    QualificationTitle.DiplomaInTeachingFeAndSkills,
                    QualificationTitle.DiplomaInTeachingFe,
                    QualificationTitle.FurtherEducationAndSkills,
                    QualificationTitle.CertificateInEducation,
                    QualificationTitle.LearningAndSkillsTeacher
                ],
                Abbreviations:
                [
                    QualificationTitle.HND,
                    QualificationTitle.DipHE,
                    QualificationTitle.FdA,
                    QualificationTitle.FdEng,
                    QualificationTitle.FdSc,
                    QualificationTitle.DiT,
                    QualificationTitle.DIT,
                    QualificationTitle.CertEd,
                    QualificationTitle.CertED,
                    QualificationTitle.LST
                ]),

            [QualificationLevel.Level4] = new(
                Title:
                [
                    QualificationTitle.HigherNationalCertificate,
                    QualificationTitle.CertificateOfHigherEducation
                ],
                Abbreviations: [QualificationTitle.HNC, QualificationTitle.CertHE])
        };

    private static bool Contains(string title, IEnumerable<string> values) =>
        values.Any(value => title.Contains(value, StringComparison.OrdinalIgnoreCase));

    private static bool ContainsWholeWord(string title, IEnumerable<string> values) =>
        values.Any(value => ContainsWholeWord(title, value));

    private static bool ContainsWholeWord(string title, string value)
    {
        var pattern = $@"\b{Regex.Escape(value)}\b";

        return Regex.IsMatch(
            title,
            pattern,
            RegexOptions.IgnoreCase);
    }
}