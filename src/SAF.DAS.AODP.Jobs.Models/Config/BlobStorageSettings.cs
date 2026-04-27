using System.Diagnostics.CodeAnalysis;

namespace SFA.DAS.AODP.Models.Config;

[ExcludeFromCodeCoverage]
public class BlobStorageSettings
{
    public required string ConnectionString { get; set; }
    public required string SafeContainerName { get; set; }
    public required string QuarantineContainerName { get; set; }
}
