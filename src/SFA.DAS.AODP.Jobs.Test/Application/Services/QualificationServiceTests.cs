using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.EntityFrameworkCore;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Infrastructure.Context;
using SFA.DAS.AODP.Jobs.Services;
using SFA.DAS.AODP.Models.Qualification;
using Xunit;

namespace SFA.DAS.AODP.Jobs.Test.Application.Services
{
    public class QualificationsServiceTests
    {
        private readonly Mock<ILogger<QualificationsService>> _mockLogger;
        private readonly Mock<IApplicationDbContext> _mockDbContext;
        private readonly Mock<IMapper> _mockMapper;
        private readonly QualificationsService _service;

        public QualificationsServiceTests()
        {
            _mockLogger = new Mock<ILogger<QualificationsService>>();
            _mockDbContext = new Mock<IApplicationDbContext>();
            _mockMapper = new Mock<IMapper>();

            _service = new QualificationsService(
                _mockLogger.Object,
                _mockDbContext.Object,
                _mockMapper.Object,
                _mockDbContext.Object
            );
        }

        [Fact]
        public async Task CompareAndUpdateQualificationsAsync_UpdatesChangedFieldsAndSaves()
        {
            // Arrange
            var importedQualifications = new List<QualificationDTO>
            {
                new QualificationDTO
                {
                    QualificationNumberNoObliques = "123",
                    OrganisationName = "Org1",
                    Title = "Title1",
                    Level = "Level1",
                    ImportStatus = ""
                }
            };

            var processedQualifications = new List<QualificationDTO>
            {
                new QualificationDTO
                {
                    QualificationNumberNoObliques = "123",
                    OrganisationName = "Org1",
                    Title = "Title2",
                    Level = "Level1"
                }
            };

            _mockDbContext.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

            // Act
            await _service.CompareAndUpdateQualificationsAsync(importedQualifications, processedQualifications);

            // Assert
            Assert.Equal("Updated", importedQualifications[0].ImportStatus);
            Assert.Contains("Title", importedQualifications[0].ChangedFields);
            _mockDbContext.Verify(x => x.SaveChangesAsync(default), Times.Once);
        }
    }
}