using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using SFA.DAS.AODP.Common.Storage;
using SFA.DAS.AODP.Data.Entities.Files;
using SFA.DAS.AODP.Data.Repositories.Jobs;
using SFA.DAS.AODP.Jobs.Functions;

namespace SFA.DAS.AODP.Jobs.UnitTests.Application.Functions;

public class FundedImportBlobTriggerFunctionTests
{
    private static readonly DateTimeOffset BlobLastModified = new(2026, 8, 15, 9, 30, 0, TimeSpan.Zero);

    private readonly Mock<ILogger<FundedImportBlobTriggerFunction>> _logger;
    private readonly Mock<IFileRecordRepository> _fileRepository;
    private readonly Mock<BlobServiceClient> _blobServiceClient;

    private readonly FundedImportBlobTriggerFunction _function;

    public FundedImportBlobTriggerFunctionTests()
    {
        _logger = new Mock<ILogger<FundedImportBlobTriggerFunction>>();
        _fileRepository = new Mock<IFileRecordRepository>();
        _blobServiceClient = new Mock<BlobServiceClient>();

        _function = new FundedImportBlobTriggerFunction(_logger.Object, _fileRepository.Object, _blobServiceClient.Object);
    }

    [Fact]
    public async Task Run_ShouldResetToNotScanned_AndUseBlobLastModified_WhenApprovedFundingRecordAlreadyExists()
    {
        var existing = new FileRecord
        {
            FileCategory = FileCategory.ApprovedFunding,
            ScanResult = MalwareScanStatus.Clean,
            LastScanAt = DateTime.UtcNow.AddDays(-1),
            UploadedAt = DateTime.UtcNow.AddDays(-30)
        };

        _fileRepository
            .Setup(r => r.GetByCategoryAsync(FileCategory.ApprovedFunding))
            .ReturnsAsync(existing);

        // Even if the blob still carries an old "Clean" tag from before this overwrite, the
        // update branch must not trust it — it can only describe the previous version.
        SetupBlob("approved.csv", "text/csv", scanResultTag: "No threats found");

        await _function.Run("csv content", "approved.csv");

        Assert.Equal(MalwareScanStatus.NotScanned, existing.ScanResult);
        Assert.Null(existing.LastScanAt);
        Assert.Equal(BlobLastModified.UtcDateTime, existing.UploadedAt);
        Assert.Equal("text/csv", existing.ContentType);
        _fileRepository.Verify(r => r.UpdateAsync(existing), Times.Once);
        _fileRepository.Verify(r => r.InsertAsync(It.IsAny<FileRecord>()), Times.Never);
    }

    [Fact]
    public async Task Run_ShouldInsertNotScannedRecord_WhenArchivedFundingRecordMissing_AndBlobHasNoScanTagYet()
    {
        _fileRepository
            .Setup(r => r.GetByCategoryAsync(FileCategory.ArchivedFunding))
            .ReturnsAsync((FileRecord?)null);

        SetupBlob("archived.csv", "text/csv", scanResultTag: null);

        await _function.Run("csv content", "archived.csv");

        _fileRepository.Verify(r =>
            r.InsertAsync(It.Is<FileRecord>(f =>
                f.FileCategory == FileCategory.ArchivedFunding &&
                f.ScanResult == MalwareScanStatus.NotScanned &&
                f.LastScanAt == null &&
                f.BlobPath == "archived.csv" &&
                f.UploadedAt == BlobLastModified.UtcDateTime)),
            Times.Once);
        _fileRepository.Verify(r => r.UpdateAsync(It.IsAny<FileRecord>()), Times.Never);
    }

    [Fact]
    public async Task Run_ShouldInsertRecordWithExistingCleanStatus_WhenApprovedFundingRecordMissing_AndBlobAlreadyCarriesACleanTag()
    {
        _fileRepository
            .Setup(r => r.GetByCategoryAsync(FileCategory.ApprovedFunding))
            .ReturnsAsync((FileRecord?)null);

        // Simulates this trigger's first run against a pre-existing, already-scanned blob:
        // there's no record for it yet, but the blob was genuinely scanned some time ago.
        SetupBlob("approved.csv", "text/csv", scanResultTag: "No threats found");

        await _function.Run("csv content", "approved.csv");

        _fileRepository.Verify(r =>
            r.InsertAsync(It.Is<FileRecord>(f =>
                f.FileCategory == FileCategory.ApprovedFunding &&
                f.ScanResult == MalwareScanStatus.Clean &&
                f.LastScanAt != null)),
            Times.Once);
    }

    [Fact]
    public async Task Run_ShouldIgnoreBlob_WhenNameDoesNotMatchAKnownFundedImportFile()
    {
        await _function.Run("csv content", "something-else.csv");

        _fileRepository.Verify(r => r.GetByCategoryAsync(It.IsAny<FileCategory>()), Times.Never);
        _fileRepository.Verify(r => r.InsertAsync(It.IsAny<FileRecord>()), Times.Never);
        _fileRepository.Verify(r => r.UpdateAsync(It.IsAny<FileRecord>()), Times.Never);
    }

    [Fact]
    public async Task Run_ShouldSkip_WhenBlobNoLongerExists()
    {
        var blobClient = SetupBlob("approved.csv", "text/csv", scanResultTag: null);
        blobClient
            .Setup(b => b.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(false, Mock.Of<Response>()));

        await _function.Run("csv content", "approved.csv");

        _fileRepository.Verify(r => r.GetByCategoryAsync(It.IsAny<FileCategory>()), Times.Never);
        _fileRepository.Verify(r => r.InsertAsync(It.IsAny<FileRecord>()), Times.Never);
        _fileRepository.Verify(r => r.UpdateAsync(It.IsAny<FileRecord>()), Times.Never);
    }

    [Fact]
    public async Task Run_ShouldSkipUpdate_WhenRecordAlreadyReflectsAVersionAtLeastAsNewAsThisBlob()
    {
        // Simulates a stale invocation finishing after a newer overwrite's invocation already
        // recorded its own (more recent) result.
        var alreadyRecorded = new FileRecord
        {
            FileCategory = FileCategory.ApprovedFunding,
            ScanResult = MalwareScanStatus.NotScanned,
            UploadedAt = BlobLastModified.UtcDateTime.AddMinutes(5)
        };

        _fileRepository
            .Setup(r => r.GetByCategoryAsync(FileCategory.ApprovedFunding))
            .ReturnsAsync(alreadyRecorded);

        SetupBlob("approved.csv", "text/csv", scanResultTag: null);

        await _function.Run("csv content", "approved.csv");

        _fileRepository.Verify(r => r.UpdateAsync(It.IsAny<FileRecord>()), Times.Never);
        _fileRepository.Verify(r => r.InsertAsync(It.IsAny<FileRecord>()), Times.Never);
    }

    private Mock<BlobClient> SetupBlob(string blobName, string contentType, string? scanResultTag)
    {
        var containerClient = new Mock<BlobContainerClient>();
        var blobClient = new Mock<BlobClient>();

        blobClient
            .Setup(b => b.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

        var blobProps = BlobsModelFactory.BlobProperties(
            contentType: contentType,
            lastModified: BlobLastModified);

        blobClient
            .Setup(b => b.GetPropertiesAsync())
            .ReturnsAsync(Response.FromValue(blobProps, Mock.Of<Response>()));

        var tags = scanResultTag == null
            ? new Dictionary<string, string>()
            : new Dictionary<string, string> { { "Malware Scanning scan result", scanResultTag } };

        var tagResult = BlobsModelFactory.GetBlobTagResult(tags);

        blobClient
            .Setup(b => b.GetTagsAsync())
            .ReturnsAsync(Response.FromValue(tagResult, Mock.Of<Response>()));

        containerClient
            .Setup(c => c.GetBlobClient(blobName))
            .Returns(blobClient.Object);

        _blobServiceClient
            .Setup(s => s.GetBlobContainerClient(BlobStoragePaths.ContainerFundingImport))
            .Returns(containerClient.Object);

        return blobClient;
    }
}
