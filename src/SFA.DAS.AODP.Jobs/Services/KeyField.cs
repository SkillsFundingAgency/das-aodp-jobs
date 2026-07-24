namespace SFA.DAS.AODP.Jobs.Services;

/// <summary>
/// Defines the key fields that we use as part of the eligibility evaluation.
/// </summary>
/// <param name="Key">The key to represent the key field.</param>
public sealed record KeyField(string Key)
{
    // Defines static objects that can be used as a reference lookup and a value object to define and use a key field.
    public static readonly KeyField OrganisationName = new("OrganisationName");
    public static readonly KeyField Title = new("Title");
    public static readonly KeyField Level = new("Level");
    public static readonly KeyField Type = new("Type");
    public static readonly KeyField TotalCredits = new("TotalCredits");
    public static readonly KeyField Ssa = new("Ssa");
    public static readonly KeyField GradingType = new("GradingType");
    public static readonly KeyField OfferedInEngland = new("OfferedInEngland");
    public static readonly KeyField PreSixteen = new("PreSixteen");
    public static readonly KeyField SixteenToEighteen = new("SixteenToEighteen");
    public static readonly KeyField EighteenPlus = new("EighteenPlus");
    public static readonly KeyField NineteenPlus = new("NineteenPlus");
    public static readonly KeyField IntentionToSeekFundingInEngland = new("IntentionToSeekFundingInEngland");
    public static readonly KeyField Glh = new("GLH");
    public static readonly KeyField MinimumGlh = new("MinimumGLH");
    public static readonly KeyField Tqt = new("TQT");
    public static readonly KeyField OperationalEndDate = new("OperationalEndDate");
    public static readonly KeyField OfferedInternationally = new("OfferedInternationally");
    public static readonly KeyField EligibleForFunding = new("EligibleForFunding");

    /// <summary>
    /// Defines a collection of all the key fields, this can be used to check if any of the key fields have changed as part of a change tracking process.
    /// </summary>
    public static IReadOnlyList<KeyField> All { get; } =
    [
        OrganisationName, Title, Level, Type, TotalCredits, Ssa, GradingType, OfferedInEngland,
        PreSixteen, SixteenToEighteen, EighteenPlus, NineteenPlus, IntentionToSeekFundingInEngland,
        Glh, MinimumGlh, Tqt, OperationalEndDate, OfferedInternationally, EligibleForFunding
    ];

    /// <summary>
    /// Determines whether given a list of raw changed field strings, if any of them are a key field that has changed.
    /// </summary>
    /// <param name="changedFields">The list of changed field strings to check against.</param>
    /// <remarks>This method is case-insensitive.</remarks>
    /// <returns><c>True</c> if any of the changed fields are deemed key fields, <c>False</c> otherwise.</returns>
    public static bool HaveKeyFieldsChanged(IList<string> changedFields) 
        => changedFields.Intersect(All.Select(kf => kf.ToString()), StringComparer.InvariantCultureIgnoreCase).Any();

    public override string ToString() => Key;
}