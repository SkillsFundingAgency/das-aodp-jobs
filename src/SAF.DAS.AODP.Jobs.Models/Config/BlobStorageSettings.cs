namespace SFA.DAS.AODP.Models.Config;

public class BlobStorageSettings
{
    public required string ConnectionString { get; set; }
    public required string FileUploadContainerName { get; set; }
}
