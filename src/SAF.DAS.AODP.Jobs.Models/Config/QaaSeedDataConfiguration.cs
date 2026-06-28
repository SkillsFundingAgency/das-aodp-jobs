using System.Diagnostics.CodeAnalysis;

namespace SFA.DAS.AODP.Models.Config;

[ExcludeFromCodeCoverage]
public class QaaSeedDataConfiguration
{
    public const string SectionName = "QaaSeedData";

    public bool Enabled { get; set; }

    public string? ContainerName { get; set; }

    public string? BlobName { get; set; }
}