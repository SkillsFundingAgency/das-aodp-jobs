using System.Diagnostics.CodeAnalysis;

namespace SFA.DAS.AODP.Models.Config;

/// <summary>
/// Defines configuration settings for blob storage.
/// </summary>
[ExcludeFromCodeCoverage]
public record StorageConfiguration
{
    /// <summary>
    /// The name of the section within configuration to bind to this object.
    /// </summary>
    public const string SectionName = "Storage";

    /// <summary>
    /// The Uri is essentially the endpoint for the storage account.
    /// </summary>
    public required string ServiceUri { get; set; }

    /// <summary>
    /// The Uri is essentially the endpoint for the storage account.
    /// We have 2 ServiceUri properties due to having two storage accounts so these two properties represent the two accounts.
    /// </summary>
    public required string ServiceUri2 { get; set; }
}