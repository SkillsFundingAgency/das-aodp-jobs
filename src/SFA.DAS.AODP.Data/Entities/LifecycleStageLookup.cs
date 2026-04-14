namespace SFA.DAS.AODP.Data.Entities;

public record LifecycleStageLookup
{
    public static readonly LifecycleStageLookup New = new(Guid.Parse("00000000-0000-0000-0000-000000000001"), "New");
    public static readonly LifecycleStageLookup Changed = new(Guid.Parse("00000000-0000-0000-0000-000000000002"), "Changed");
    public static readonly LifecycleStageLookup Completed = new(Guid.Parse("00000000-0000-0000-0000-000000000003"), "Completed");
    
    private static readonly IReadOnlyDictionary<Guid, LifecycleStageLookup> IdLookup = new List<LifecycleStageLookup>
    {
       New, Changed, Completed
    }.ToDictionary(x => x.Id);

    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public LifecycleStageLookup(Guid id, string name)
    {
        Id = id;
        Name = name;
    }

    public static LifecycleStageLookup FromId(Guid id) => IdLookup.Single(o => o.Key == id).Value;
}