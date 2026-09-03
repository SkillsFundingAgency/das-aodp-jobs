using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using Moq;
using SFA.DAS.AODP.Data.Entities.Files;
using SFA.DAS.AODP.Data.Repositories.Jobs;
using SFA.DAS.AODP.Jobs.FeatureManagement;
using SFA.DAS.AODP.Jobs.Functions;
using Xunit;

namespace SFA.DAS.AODP.Jobs.UnitTests.Application.Functions
{
    public class DefenderScanReconciliationFunctionTests
    {
        private readonly Mock<ILogger<DefenderScanReconciliationFunction>> _logger;
        private readonly Mock<IFileRecordRepository> _fileRepository;
        private readonly Mock<BlobServiceClient> _blobServiceClient;
        private readonly Mock<IOptionsSnapshot<FeatureManagementOptions>> _features;

        private readonly DefenderScanReconciliationFunction _function;

        public DefenderScanReconciliationFunctionTests()
        {
            _logger = new Mock<ILogger<DefenderScanReconciliationFunction>>();
            _fileRepository = new Mock<IFileRecordRepository>();
            _blobServiceClient = new Mock<BlobServiceClient>();
            _features = new Mock<IOptionsSnapshot<FeatureManagementOptions>>();

            _features.Setup(f => f.Value)
                .Returns(new FeatureManagementOptions { DefenderPollingEnabled = true });

            _function = new DefenderScanReconciliationFunction(
                _logger.Object,
                _fileRepository.Object,
                _blobServiceClient.Object,
                _features.Object);
        }

        [Fact]
        public async Task Run_ShouldNotReconcile_WhenFeatureDisabled()
        {
            _features.Setup(f => f.Value)
                .Returns(new FeatureManagementOptions { DefenderPollingEnabled = false });

            var function = new DefenderScanReconciliationFunction(
                _logger.Object,
                _fileRepository.Object,
                _blobServiceClient.Object,
                _features.Object);

            var timer = new TimerInfo
            {
                ScheduleStatus = new ScheduleStatus(),
                IsPastDue = false
            };

            await function.Run(timer);

            _fileRepository.Verify(r => r.GetPendingScanAsync(It.IsAny<DateTime>()), Times.Never);
        }


        [Fact]
        public async Task ProcessFile_ShouldReturn_WhenNoScanTag()
        {
            var file = new FileRecord
            {
                BlobContainer = "container",
                BlobPath = "file.csv"
            };

            var tagResult = BlobsModelFactory.GetBlobTagResult(new Dictionary<string, string>());

            var blobClient = new Mock<BlobClient>();
            blobClient
                .Setup(b => b.GetTagsAsync())
                .ReturnsAsync(Response.FromValue(tagResult, Mock.Of<Response>()));

            var containerClient = new Mock<BlobContainerClient>();
            containerClient.Setup(c => c.GetBlobClient("file.csv"))
                .Returns(blobClient.Object);

            _blobServiceClient.Setup(s => s.GetBlobContainerClient("container"))
                .Returns(containerClient.Object);

            await InvokeProcessFile(file);

            _fileRepository.Verify(r => r.UpdateAsync(It.IsAny<FileRecord>()), Times.Never);
        }

        [Fact]
        public async Task ProcessFile_ShouldReturn_WhenScanTagEmpty()
        {
            var file = new FileRecord
            {
                BlobContainer = "container",
                BlobPath = "file.csv"
            };

            var tags = new Dictionary<string, string>
            {
                { "Malware Scanning scan result", "" }
            };

            var tagResult = BlobsModelFactory.GetBlobTagResult(tags);

            var blobClient = new Mock<BlobClient>();
            blobClient
                .Setup(b => b.GetTagsAsync())
                .ReturnsAsync(Response.FromValue(tagResult, Mock.Of<Response>()));

            var containerClient = new Mock<BlobContainerClient>();
            containerClient.Setup(c => c.GetBlobClient("file.csv"))
                .Returns(blobClient.Object);

            _blobServiceClient.Setup(s => s.GetBlobContainerClient("container"))
                .Returns(containerClient.Object);

            await InvokeProcessFile(file);

            _fileRepository.Verify(r => r.UpdateAsync(It.IsAny<FileRecord>()), Times.Never);
        }

        [Fact]
        public async Task ProcessFile_ShouldUpdate_WhenClean()
        {
            var file = new FileRecord
            {
                BlobContainer = "container",
                BlobPath = "file.csv"
            };

            var tags = new Dictionary<string, string>
            {
                { "Malware Scanning scan result", "No threats found" }
            };

            var tagResult = BlobsModelFactory.GetBlobTagResult(tags);

            var blobClient = new Mock<BlobClient>();
            blobClient
                .Setup(b => b.GetTagsAsync())
                .ReturnsAsync(Response.FromValue(tagResult, Mock.Of<Response>()));

            var containerClient = new Mock<BlobContainerClient>();
            containerClient.Setup(c => c.GetBlobClient("file.csv"))
                .Returns(blobClient.Object);

            _blobServiceClient.Setup(s => s.GetBlobContainerClient("container"))
                .Returns(containerClient.Object);

            await InvokeProcessFile(file);

            Assert.Equal(MalwareScanStatus.Clean, file.ScanResult);
            _fileRepository.Verify(r => r.UpdateAsync(file), Times.Once);
        }

        [Fact]
        public async Task ProcessFile_ShouldDeleteAndUpdate_WhenMalicious()
        {
            var file = new FileRecord
            {
                BlobContainer = "container",
                BlobPath = "file.csv"
            };

            var tags = new Dictionary<string, string>
            {
                { "Malware Scanning scan result", "Malicious" }
            };

            var tagResult = BlobsModelFactory.GetBlobTagResult(tags);

            var blobClient = new Mock<BlobClient>();
            blobClient
                .Setup(b => b.GetTagsAsync())
                .ReturnsAsync(Response.FromValue(tagResult, Mock.Of<Response>()));

            blobClient
                .Setup(b => b.DeleteIfExistsAsync())
                .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

            var containerClient = new Mock<BlobContainerClient>();
            containerClient.Setup(c => c.GetBlobClient("file.csv"))
                .Returns(blobClient.Object);

            _blobServiceClient.Setup(s => s.GetBlobContainerClient("container"))
                .Returns(containerClient.Object);

            await InvokeProcessFile(file);

            Assert.Equal(MalwareScanStatus.Malicious, file.ScanResult);
            blobClient.Verify(b => b.DeleteIfExistsAsync(), Times.Once);
            _fileRepository.Verify(r => r.UpdateAsync(file), Times.Once);
        }

        [Fact]
        public async Task ProcessFile_ShouldSetError_WhenBlobMissing()
        {
            var file = new FileRecord
            {
                BlobContainer = "container",
                BlobPath = "file.csv"
            };

            var blobClient = new Mock<BlobClient>();
            blobClient
                .Setup(b => b.GetTagsAsync())
                .ThrowsAsync(new RequestFailedException(
                    status: 404,
                    message: "BlobNotFound",
                    errorCode: "BlobNotFound",
                    innerException: null
                ));


            var containerClient = new Mock<BlobContainerClient>();
            containerClient.Setup(c => c.GetBlobClient("file.csv"))
                .Returns(blobClient.Object);

            _blobServiceClient.Setup(s => s.GetBlobContainerClient("container"))
                .Returns(containerClient.Object);

            await InvokeProcessFile(file);

            Assert.Equal(MalwareScanStatus.Error, file.ScanResult);
            _fileRepository.Verify(r => r.UpdateAsync(file), Times.Once);
        }

        private Task InvokeProcessFile(FileRecord file)
        {
            return (Task)_function.GetType()
                .GetMethod("ProcessFile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .Invoke(_function, new object[] { file });
        }
    }
}
