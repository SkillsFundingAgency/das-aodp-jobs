//using AutoMapper;
//using Microsoft.Extensions.Logging;
//using Moq;
//using Moq.EntityFrameworkCore;
//using SFA.DAS.AODP.Data.Entities;
//using SFA.DAS.AODP.Infrastructure.Context;
//using SFA.DAS.AODP.Jobs.Services;
//using SFA.DAS.AODP.Models.Qualification;
//using Xunit;

//namespace SFA.DAS.AODP.Jobs.Test.Application.Services
//{
//    public class QualificationsServiceTests
//    {
//        private readonly Mock<ILogger<QualificationsService>> _mockLogger;
//        private readonly Mock<IApplicationDbContext> _mockDbContext;
//        private readonly Mock<IMapper> _mockMapper;
//        private readonly QualificationsService _service;

//        public QualificationsServiceTests()
//        {
//            _mockLogger = new Mock<ILogger<QualificationsService>>();
//            _mockDbContext = new Mock<IApplicationDbContext>();
//            _mockMapper = new Mock<IMapper>();

//            _service = new QualificationsService(
//                _mockLogger.Object,
//                _mockDbContext.Object,
//                _mockMapper.Object,
//                _mockDbContext.Object
//            );
//        }

//        [Fact]
//        public async Task CompareAndUpdateQualificationsAsync_UpdatesChangedFieldsAndSaves()
//        {
//            // Arrange
//            var importedQualifications = new List<QualificationDTO>
//            {
//                new QualificationDTO
//                {
//                    QualificationNumberNoObliques = "123",
//                    OrganisationName = "Org1",
//                    Title = "Title1",
//                    Level = "Level1",
//                    ImportStatus = ""
//                }
//            };

//            var processedQualifications = new List<QualificationDTO>
//            {
//                new QualificationDTO
//                {
//                    QualificationNumberNoObliques = "123",
//                    OrganisationName = "Org1",
//                    Title = "Title2",
//                    Level = "Level1"
//                }
//            };

//            _mockDbContext.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

//            // Act
//            await _service.CompareAndUpdateQualificationsAsync(importedQualifications, processedQualifications);

//            // Assert
//            Assert.Equal("Updated", importedQualifications[0].ImportStatus);
//            Assert.Contains("Title", importedQualifications[0].ChangedFields);
//            _mockDbContext.Verify(x => x.SaveChangesAsync(default), Times.Once);
//        }

//        [Fact]
//        public async Task SaveRegulatedQualificationsAsync_SavesQualifications()
//        {
//            // Arrange
//            var qualifications = new List<QualificationDTO> { new QualificationDTO { QualificationNumberNoObliques = "123" } };
//            var qualificationEntities = new List<RegulatedQualificationsImport>{ new RegulatedQualificationsImport { QualificationNumberNoObliques = "123" } };

//            _mockMapper.Setup(m => m.Map<List<RegulatedQualificationsImport>>(qualifications))
//                       .Returns(qualificationEntities);

//            _mockDbContext.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

//            // Act
//            await _service.SaveRegulatedQualificationsAsync(qualifications);

//            // Assert
//            //_mockDbContext.Verify(x => x.RegulatedQualificationsImport.AddRange(qualificationEntities), Times.Once);
//            //_mockDbContext.Verify(x => x.SaveChangesAsync(default), Times.Once);
//            _mockDbContext.Verify(
//                db => db.BulkInsertAsync(It.IsAny<IEnumerable<RegulatedQualificationsImport>>(), It.IsAny<CancellationToken>()),
//                Times.Once
//            );
//        }

//        [Fact]
//        public async Task SaveRegulatedQualificationsAsync_LogsErrorOnException()
//        {
//            // Arrange
//            var qualifications = new List<QualificationDTO>
//        {
//            new QualificationDTO { QualificationNumberNoObliques = "123" }
//        };

//            _mockMapper.Setup(m => m.Map<List<RegulatedQualificationsImport>>(qualifications))
//                       .Throws(new Exception("Test exception"));

//            // Act & Assert
//            await Assert.ThrowsAsync<Exception>(async () =>
//                await _service.SaveRegulatedQualificationsAsync(qualifications));

//            _mockLogger.Verify(
//                x => x.Log(
//                    LogLevel.Error,
//                    It.IsAny<EventId>(),
//                    It.IsAny<It.IsAnyType>(),
//                    It.IsAny<Exception>(),
//                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
//                Times.Once);
//        }

//        [Fact]
//        public async Task GetAllProcessedRegulatedQualificationsAsync_ReturnsMappedQualifications()
//        {
//            // Arrange
//            var processedEntities = new List<ProcessedRegulatedQualification>
//            {
//                new ProcessedRegulatedQualification { QualificationNumberNoObliques = "123" }
//            };

//            var mappedQualifications = new List<QualificationDTO>
//            {
//                new QualificationDTO { QualificationNumberNoObliques = "123" }
//            };

//            _mockDbContext.Setup(x => x.ProcessedRegulatedQualifications)
//                          .ReturnsDbSet(processedEntities);

//            _mockMapper.Setup(m => m.Map<List<QualificationDTO>>(processedEntities))
//                       .Returns(mappedQualifications);

//            // Act
//            var result = await _service.GetAllProcessedRegulatedQualificationsAsync();

//            // Assert
//            Assert.Equal(mappedQualifications, result);
//            var loggerMock = new Mock<ILogger<QualificationsService>>();
//            loggerMock.Setup(logger =>
//                logger.Log(
//                    LogLevel.Information,
//                    It.IsAny<EventId>(),
//                    It.Is<It.IsAnyType>((v, t) => v.ToString() == "Retrieving all processed regulated qualification records..."),
//                    It.IsAny<Exception>(),
//                    It.IsAny<Func<It.IsAnyType, Exception, string>>())
//            );
//        }

//    }
//}