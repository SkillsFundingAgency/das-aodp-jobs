using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using SFA.DAS.AODP.Infrastructure.Context;
using SFA.DAS.AODP.Jobs.Services;


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
                _mockMapper.Object,
                _mockDbContext.Object
            );
        }

    }
}