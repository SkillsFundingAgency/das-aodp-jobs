namespace SFA.DAS.AODP.Infrastructure.Services;

/// <summary>
/// Defines an abstraction for a <see cref="Guid"/>.
/// </summary>
public interface IGuidProvider
{
    /// <summary>
    /// Generates a new guid.
    /// </summary>
    /// <returns>Returns a new guid.</returns>
    Guid NewGuidFor(string name);
}