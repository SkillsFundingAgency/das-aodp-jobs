namespace SFA.DAS.AODP.Data.Entities;

public record ActionTypeLookup
{
    public static readonly ActionTypeLookup NoActionRequired = new(Guid.Parse("00000000-0000-0000-0000-000000000001"), "No Action Required");
    public static readonly ActionTypeLookup ActionRequired = new(Guid.Parse("00000000-0000-0000-0000-000000000002"), "Action Required");
    public static readonly ActionTypeLookup Ignore = new(Guid.Parse("00000000-0000-0000-0000-000000000003"), "Ignore");

    private static readonly IReadOnlyDictionary<Guid, ActionTypeLookup> IdLookup = new List<ActionTypeLookup>
    {
        NoActionRequired, ActionRequired, Ignore
    }.ToDictionary(x => x.Id);

    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public ActionTypeLookup(Guid id, string name)
    {
        Id = id;
        Name = name;
    }

    public static ActionTypeLookup FromId(Guid id) => IdLookup.Single(o => o.Key == id).Value;
}