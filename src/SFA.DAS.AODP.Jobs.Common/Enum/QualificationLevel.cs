using System.Diagnostics.CodeAnalysis;

namespace SFA.DAS.AODP.Common.Enum;

[ExcludeFromCodeCoverage]
public record QualificationLevel(int? Code, string Value)
{
    public static readonly QualificationLevel EntryLevel = new(null, "Entry Level");
    public static readonly QualificationLevel Level1And2 = new(null, "Level 1/Level 2");
    public static readonly QualificationLevel Level1 = new(1, "Level 1");
    public static readonly QualificationLevel Level2 = new(2, "Level 2");
    public static readonly QualificationLevel Level3 = new(3, "Level 3");
    public static readonly QualificationLevel Level4 = new(4, "Level 4");
    public static readonly QualificationLevel Level5 = new(5, "Level 5");
    public static readonly QualificationLevel Level6 = new(6, "Level 6");
    public static readonly QualificationLevel Level7 = new(7, "Level 7");
    public static readonly QualificationLevel Level8 = new(8, "Level 8");

    private static readonly IReadOnlyCollection<QualificationLevel> Lookup =
    [
        EntryLevel, Level1And2, Level1, Level2, Level3, Level4, Level5, Level6, Level7, Level8
    ];

    public static QualificationLevel? TryFromString(string? level) =>
        Lookup.SingleOrDefault(o => string.Equals(o.Value, level, StringComparison.OrdinalIgnoreCase));
}