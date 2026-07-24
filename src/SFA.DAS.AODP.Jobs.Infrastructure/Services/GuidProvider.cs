using System.Diagnostics.CodeAnalysis;

namespace SFA.DAS.AODP.Infrastructure.Services;

/// <summary>
/// Default implementation of <see cref="IGuidProvider"/> that generates a new guid using <see cref="Guid.NewGuid()"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public class GuidProvider : IGuidProvider
{
    /// <inheritdoc/>.
    public Guid NewGuidFor(string name) => Guid.NewGuid();
}