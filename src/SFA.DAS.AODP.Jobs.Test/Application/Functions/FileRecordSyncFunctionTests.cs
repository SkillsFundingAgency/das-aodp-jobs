using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging.Abstractions;
using SFA.DAS.AODP.Data.Entities.Files;
using SFA.DAS.AODP.Data.Repositories.Jobs;
using SFA.DAS.AODP.Jobs.Functions;
using SFA.DAS.AODP.Jobs.Test.Mocks;
using System.Collections.Specialized;

namespace SFA.DAS.AODP.Jobs.UnitTests.Application.Functions;

public class FileRecordSyncFunctionTests
{
    private readonly Mock<IFileRecordRepository> _fileRepository = new();
    private readonly Mock<BlobServiceClient> _blobServiceClient = new();
    private readonly Mock<FunctionContext> _functionContext = new();

    private readonly FileRecordSyncFunction _function;

    public FileRecordSyncFunctionTests()
    {
        _function = new FileRecordSyncFunction(
            NullLogger<FileRecordSyncFunction>.Instance,
            _fileRepository.Object,
            _blobServiceClient.Object);
    }

    [Fact]
    public async Task Run_ShouldReturnBadRequest_WhenNoValidCategoriesSupplied()
    {
        var request = CreateRequest("categories", "NotARealCategory");

        var result = await _function.Run(request);

        Assert.IsType<BadRequestObjectResult>(result);
        _fileRepository.Verify(r => r.GetByCategoryAsync(It.IsAny<FileCategory>()), Times.Never);
    }

    [Fact]
    public async Task Run_SingleRecordCategory_ShouldSkip_WhenRecordAlreadyExists()
    {
        _fileRepository
            .Setup(r => r.GetByCategoryAsync(FileCategory.ApprovedFunding))
            .ReturnsAsync(new FileRecord { Id = Guid.NewGuid() });

        var request = CreateRequest("categories", "ApprovedFunding");

        var result = await _function.Run(request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("0 FileRecord(s) created", ok.Value?.ToString() ?? string.Empty);
        _fileRepository.Verify(r => r.InsertAsync(It.IsAny<FileRecord>()), Times.Never);
    }

    [Fact]
    public async Task Run_ApprovedFunding_ShouldCreateRecordAsNotScanned_WhenBlobExistsAndUntracked()
    {
        _fileRepository
            .Setup(r => r.GetByCategoryAsync(FileCategory.ApprovedFunding))
            .ReturnsAsync((FileRecord?)null);

        var blobClient = new Mock<BlobClient>();
        blobClient.Setup(b => b.ExistsAsync(default)).ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));
        blobClient
            .Setup(b => b.GetPropertiesAsync(null, default))
            .ReturnsAsync(Response.FromValue(
                BlobsModelFactory.BlobProperties(contentType: "text/csv"), Mock.Of<Response>()));

        var containerClient = new Mock<BlobContainerClient>();
        containerClient.Setup(c => c.GetBlobClient("approved.csv")).Returns(blobClient.Object);

        _blobServiceClient
            .Setup(s => s.GetBlobContainerClient("funded-qualifications-import"))
            .Returns(containerClient.Object);

        var request = CreateRequest("categories", "ApprovedFunding");

        var result = await _function.Run(request);

        Assert.IsType<OkObjectResult>(result);
        _fileRepository.Verify(r => r.InsertAsync(It.Is<FileRecord>(f =>
            f.FileCategory == FileCategory.ApprovedFunding &&
            f.BlobContainer == "funded-qualifications-import" &&
            f.BlobPath == "approved.csv" &&
            f.ScanResult == MalwareScanStatus.NotScanned &&
            f.LastScanAt == null)),
            Times.Once);
        blobClient.Verify(b => b.GetTagsAsync(null, default), Times.Never);
    }

    [Fact]
    public async Task Run_ApprovedFunding_ShouldDoNothing_WhenBlobDoesNotExist()
    {
        _fileRepository
            .Setup(r => r.GetByCategoryAsync(FileCategory.ApprovedFunding))
            .ReturnsAsync((FileRecord?)null);

        var blobClient = new Mock<BlobClient>();
        blobClient.Setup(b => b.ExistsAsync(default)).ReturnsAsync(Response.FromValue(false, Mock.Of<Response>()));

        var containerClient = new Mock<BlobContainerClient>();
        containerClient.Setup(c => c.GetBlobClient("approved.csv")).Returns(blobClient.Object);

        _blobServiceClient
            .Setup(s => s.GetBlobContainerClient("funded-qualifications-import"))
            .Returns(containerClient.Object);

        var request = CreateRequest("categories", "ApprovedFunding");

        await _function.Run(request);

        _fileRepository.Verify(r => r.InsertAsync(It.IsAny<FileRecord>()), Times.Never);
    }

    [Fact]
    public async Task Run_Pldns_ShouldPickMostRecentlyModifiedBlob_UnderCategoryFolder()
    {
        _fileRepository
            .Setup(r => r.GetByCategoryAsync(FileCategory.Pldns))
            .ReturnsAsync((FileRecord?)null);

        var older = BlobsModelFactory.BlobItem(
            name: "Pldns/old-upload.xlsx",
            properties: BlobsModelFactory.BlobItemProperties(true, lastModified: DateTimeOffset.UtcNow.AddDays(-10)));
        var newer = BlobsModelFactory.BlobItem(
            name: "Pldns/new-upload.xlsx",
            properties: BlobsModelFactory.BlobItemProperties(true, lastModified: DateTimeOffset.UtcNow.AddDays(-1)));

        var containerClient = new Mock<BlobContainerClient>();
        containerClient
            .Setup(c => c.GetBlobsAsync(BlobTraits.None, BlobStates.None, "Pldns/", It.IsAny<CancellationToken>()))
            .Returns(ToAsyncPageable(new[] { older, newer }));

        var blobClient = new Mock<BlobClient>();
        blobClient.Setup(b => b.ExistsAsync(default)).ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));
        blobClient
            .Setup(b => b.GetPropertiesAsync(null, default))
            .ReturnsAsync(Response.FromValue(
                BlobsModelFactory.BlobProperties(contentType: "application/vnd.ms-excel"), Mock.Of<Response>()));

        containerClient.Setup(c => c.GetBlobClient("Pldns/new-upload.xlsx")).Returns(blobClient.Object);

        _blobServiceClient
            .Setup(s => s.GetBlobContainerClient("importfilescontainer"))
            .Returns(containerClient.Object);

        var request = CreateRequest("categories", "Pldns");

        await _function.Run(request);

        _fileRepository.Verify(r => r.InsertAsync(It.Is<FileRecord>(f =>
            f.BlobPath == "Pldns/new-upload.xlsx")),
            Times.Once);
    }

    [Fact]
    public async Task Run_MultiRecordCategory_ShouldOnlyCreate_ForBlobsWithNoExistingRecord()
    {
        var applicationId = Guid.NewGuid();
        var trackedQuestionId = Guid.NewGuid();
        var untrackedQuestionId = Guid.NewGuid();

        var trackedPath = $"{applicationId}/{trackedQuestionId}/tracked-file.pdf";
        var untrackedPath = $"{applicationId}/{untrackedQuestionId}/untracked-file.pdf";

        var tracked = BlobsModelFactory.BlobItem(
            name: trackedPath,
            properties: BlobsModelFactory.BlobItemProperties(true));
        var untracked = BlobsModelFactory.BlobItem(
            name: untrackedPath,
            properties: BlobsModelFactory.BlobItemProperties(true));

        var containerClient = new Mock<BlobContainerClient>();
        containerClient
            .Setup(c => c.GetBlobsAsync(BlobTraits.None, BlobStates.None, null, It.IsAny<CancellationToken>()))
            .Returns(ToAsyncPageable(new[] { tracked, untracked }));

        _fileRepository
            .Setup(r => r.GetByPathAsync("files", trackedPath))
            .ReturnsAsync(new FileRecord { Id = Guid.NewGuid() });
        _fileRepository
            .Setup(r => r.GetByPathAsync("files", untrackedPath))
            .ReturnsAsync((FileRecord?)null);

        var blobClient = new Mock<BlobClient>();
        blobClient.Setup(b => b.ExistsAsync(default)).ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));
        blobClient
            .Setup(b => b.GetPropertiesAsync(null, default))
            .ReturnsAsync(Response.FromValue(
                BlobsModelFactory.BlobProperties(contentType: "application/pdf"), Mock.Of<Response>()));

        containerClient
            .Setup(c => c.GetBlobClient(untrackedPath))
            .Returns(blobClient.Object);

        _blobServiceClient
            .Setup(s => s.GetBlobContainerClient("files"))
            .Returns(containerClient.Object);

        var request = CreateRequest("categories", "QuestionUpload");

        var result = await _function.Run(request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("1 FileRecord(s) created, 1 blob(s) already tracked", ok.Value?.ToString() ?? string.Empty);
        _fileRepository.Verify(r => r.InsertAsync(It.Is<FileRecord>(f =>
            f.BlobPath == untrackedPath)),
            Times.Once);
    }

    private MockHttpRequestData CreateRequest(string key, string value)
    {
        var query = new NameValueCollection { { key, value } };
        return new MockHttpRequestData(_functionContext.Object, query);
    }

    private static AsyncPageable<BlobItem> ToAsyncPageable(IEnumerable<BlobItem> items)
    {
        return AsyncPageable<BlobItem>.FromPages(new[] { Page<BlobItem>.FromValues(items.ToList(), null, Mock.Of<Response>()) });
    }
}
