namespace SFA.DAS.AODP.Infrastructure.Services;

/// <summary>
/// Defines an abstraction for a <see cref="Guid"/>.
/// </summary>
public interface IGuidProvider
{
    /// <summary>
    /// Generates a new guid.
    /// </summary>
    /// <remarks>The name parameter is a simple way to identify the purpose of the generated guid, useful when mocking in unit tests to have more precise control over guid generation.</remarks>
    /// <returns>Returns a new guid.</returns>
    Guid NewGuidFor(string name);
}