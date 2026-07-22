using Azure.Messaging.EventGrid;
using Azure.Storage.Blobs;
using CsvHelper;
using SFA.DAS.AODP.Common.Storage;
using SFA.DAS.AODP.Data.Entities.Files;

/**
 * Handles Event Grid notifications to check file scan status in blob storage. Untested.
 * */
public class DefenderScanResultFunction
{
    private readonly ILogger<DefenderScanResultFunction> _logger;
    private readonly IFileRecordRepository _fileRepository;
    private readonly BlobServiceClient _blobServiceClient;

    public DefenderScanResultFunction(
        ILogger<DefenderScanResultFunction> logger,
        IFileRecordRepository fileRepository,
        BlobServiceClient blobServiceClient)
    {
        _logger = logger;
        _fileRepository = fileRepository;
        _blobServiceClient = blobServiceClient;
    }

    [Function("DefenderScanResultFunction")]
    public async Task Run([EventGridTrigger] EventGridEvent eventGridEvent)
    {
        _logger.LogInformation("Received Defender scan event");

        var data = eventGridEvent.Data.ToObjectFromJson<DefenderScanEvent>();

        if (data?.BlobUri == null)
        {
            _logger.LogWarning("Invalid event payload");
            return;
        }

        var uri = new Uri(data.BlobUri);

        var containerName = uri.Segments[1].TrimEnd('/');
        var blobPath = string.Join("", uri.Segments.Skip(2));

        _logger.LogInformation("Processing blob: {Container}/{Path}", containerName, blobPath);

        var fileRecord = await _fileRepository.GetByPathAsync(containerName, blobPath);

        if (fileRecord == null)
        {
            _logger.LogWarning("FileRecord missing — loading blob metadata");

            var container = _blobServiceClient.GetBlobContainerClient(containerName);

            if (!await container.ExistsAsync())
            {
                _logger.LogWarning(
                    "Blob container '{Container}' does not exist — cannot process Defender event.",
                    containerName);

                return;
            }

            var blob = container.GetBlobClient(blobPath);

            if (!await blob.ExistsAsync())
            {
                _logger.LogWarning(
                    "Blob does not exist — cannot create FileRecord. " +
                    "Event may have arrived after blob deletion."
                );
                return; 
            }

            var blobProperties = await blob.GetPropertiesAsync();

            var parsedBlobPath = ParseBlobPath(containerName, blobPath);

            fileRecord = new FileRecord
            {
                Id = Guid.NewGuid(),
                FileName = Path.GetFileName(blobPath),
                ContentType = blobProperties.Value.ContentType,
                BlobPath = blobPath,
                BlobContainer = containerName,
                FileCategory = parsedBlobPath.Category,
                ApplicationId = parsedBlobPath.ApplicationId,
                MessageId = parsedBlobPath.MessageId,
                QuestionId = parsedBlobPath.QuestionId,
                UploadedAt = blobProperties.Value.CreatedOn.UtcDateTime,
                UploadedByDisplayName = "DfEStaffUser",
                ScanResult = MalwareScanStatus.NotScanned,
                LastScanAt = null
            };

            await _fileRepository.InsertAsync(fileRecord);
        }

        var status = MapScanResult(data.ScanResultType);

        fileRecord.ScanResult = status;
        fileRecord.LastScanAt = DateTime.UtcNow;

        if (status == MalwareScanStatus.Malicious)
        {
            _logger.LogWarning("Malware detected — deleting blob");

            var container = _blobServiceClient.GetBlobContainerClient(containerName);
            var blob = container.GetBlobClient(blobPath);

            await blob.DeleteIfExistsAsync();
        }

        await _fileRepository.UpdateAsync(fileRecord);

        _logger.LogInformation(
            "Updated file status to {Status} (raw metadata = {Raw})",
            status,
            data.ScanResultType
        );
    }

    private MalwareScanStatus MapScanResult(string? scanResult)
    {
        return scanResult?.ToLowerInvariant() switch
        {
            "no threats found" => MalwareScanStatus.Clean,
            "malicious" => MalwareScanStatus.Malicious,
            "error" => MalwareScanStatus.Error,
            "unsupported" => MalwareScanStatus.Error,
            "scan timed out" => MalwareScanStatus.Error,
            _ => MalwareScanStatus.NotScanned
        };
    }

    public static (FileCategory Category, Guid? ApplicationId, Guid? MessageId, Guid? QuestionId) ParseBlobPath(string containerName, string blobPath)
    {
        var segments = blobPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        FileCategory category = FileCategory.Unknown;
        Guid? applicationId = null;
        Guid? messageId = null;
        Guid? questionId = null;

        // importfilescontainer/DefundingList/{fileId}
        if (containerName.Equals(BlobStoragePaths.ContainerImportFiles, StringComparison.OrdinalIgnoreCase))
        {
            if (segments[0].Equals(BlobStoragePaths.FolderDefundingList, StringComparison.OrdinalIgnoreCase))
            {
                category = FileCategory.DefundingList;
            }
            else if (segments[0].Equals(BlobStoragePaths.FolderPldns, StringComparison.OrdinalIgnoreCase))
            {
                category = FileCategory.Pldns;
            }
        }
        // files/messages/{appId}/{messageId}/{fileId}
        else if (containerName.Equals(BlobStoragePaths.ContainerFiles, StringComparison.OrdinalIgnoreCase))
        {
            if (segments[0].Equals(BlobStoragePaths.FolderMessages, StringComparison.OrdinalIgnoreCase))
            {
                category = FileCategory.MessageAttachment;
                applicationId = Guid.Parse(segments[1]);
                messageId = Guid.Parse(segments[2]);
            }
            else
            {
                // files/{applicationId}/{questionId}/{fileId}
                category = FileCategory.QuestionUpload;
                applicationId = Guid.Parse(segments[0]);
                questionId = Guid.Parse(segments[1]);
            }
        }
        // funded-qualifiations-import/approved.csv or archived.csv
        else if (containerName.Equals(BlobStoragePaths.ContainerFundingImport, StringComparison.OrdinalIgnoreCase))
        {
            if (segments[0].Equals(BlobStoragePaths.ApprovedFundingFileName, StringComparison.OrdinalIgnoreCase))
            {
                category = FileCategory.ApprovedFunding;
            }
            else if (segments[0].Equals(BlobStoragePaths.ArchivedFundingFileName, StringComparison.OrdinalIgnoreCase))
            {
                category = FileCategory.ArchivedFunding;
            }
        }
        // funded-qualifications-output/{date}-AOdPApprovedOutputFile.csv
        else if (containerName.Equals(BlobStoragePaths.ContainerFundingOutput, StringComparison.OrdinalIgnoreCase))
        {
            category = FileCategory.FundingOutput;
        }

        return (category, applicationId, messageId, questionId);
    }


}
