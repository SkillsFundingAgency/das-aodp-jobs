using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Infrastructure.Context;
using SFA.DAS.AODP.Jobs.Enum;
using SFA.DAS.AODP.Jobs.Services;

namespace SFA.DAS.AODP.Jobs.Test.Application.Services
{
    public class ActionTypeServiceTests
    {
        private readonly Mock<ILogger<ReferenceDataService>> _mockLogger;
        private readonly Mock<IApplicationDbContext> _mockDbContext;
        private readonly List<ActionType> _actionTypes;
        private readonly List<Data.Entities.ProcessStatus> _processStatuses;
        private readonly Mock<DbSet<ActionType>> _mockDbSet;
        private readonly Mock<DbSet<Data.Entities.ProcessStatus>> _mockProcessStatusDbSet;
        private readonly List<Data.Entities.LifecycleStage> _lifecycleStages;
        private readonly Mock<DbSet<Data.Entities.LifecycleStage>> _mockLifecycleStageDbSet;

        public ActionTypeServiceTests()
        {
            _mockLogger = new Mock<ILogger<ReferenceDataService>>();
            _mockDbContext = new Mock<IApplicationDbContext>();

            // Setup test data
            _actionTypes = new List<ActionType>
            {
                new ActionType { Id = Guid.NewGuid(), Description = "No Action Required" },
                new ActionType { Id = Guid.NewGuid(), Description = "Action Required" },
                new ActionType { Id = Guid.NewGuid(), Description = "Ignore" }
            };

            _mockDbSet = new Mock<DbSet<ActionType>>();

            // Setup the DbSet mock
            var queryable = _actionTypes.AsQueryable();
            _mockDbSet.As<IQueryable<ActionType>>().Setup(m => m.Provider).Returns(queryable.Provider);
            _mockDbSet.As<IQueryable<ActionType>>().Setup(m => m.Expression).Returns(queryable.Expression);
            _mockDbSet.As<IQueryable<ActionType>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            _mockDbSet.As<IQueryable<ActionType>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());

            _mockDbContext.Setup(x => x.ActionType).Returns(_mockDbSet.Object);

            _processStatuses = new List<Data.Entities.ProcessStatus>
            {
                new Data.Entities.ProcessStatus { Id = Guid.NewGuid(), Name = "Decision Needed" },
                new Data.Entities.ProcessStatus { Id = Guid.NewGuid(), Name = "No Action Required" },
                new Data.Entities.ProcessStatus { Id = Guid.NewGuid(), Name = "Hold" },
                new Data.Entities.ProcessStatus { Id = Guid.NewGuid(), Name = "Approved" },
                new Data.Entities.ProcessStatus { Id = Guid.NewGuid(), Name = "Rejected" }
            };

            _mockProcessStatusDbSet = new Mock<DbSet<Data.Entities.ProcessStatus>>();

            // Setup the DbSet mock
            var queryableProcessStatus = _processStatuses.AsQueryable();
            _mockProcessStatusDbSet.As<IQueryable<Data.Entities.ProcessStatus>>().Setup(m => m.Provider).Returns(queryableProcessStatus.Provider);
            _mockProcessStatusDbSet.As<IQueryable<Data.Entities.ProcessStatus>>().Setup(m => m.Expression).Returns(queryableProcessStatus.Expression);
            _mockProcessStatusDbSet.As<IQueryable<Data.Entities.ProcessStatus>>().Setup(m => m.ElementType).Returns(queryableProcessStatus.ElementType);
            _mockProcessStatusDbSet.As<IQueryable<Data.Entities.ProcessStatus>>().Setup(m => m.GetEnumerator()).Returns(queryableProcessStatus.GetEnumerator());

            _mockDbContext.Setup(x => x.ProcessStatus).Returns(_mockProcessStatusDbSet.Object);

            _lifecycleStages = new List<Data.Entities.LifecycleStage>
            {
                new Data.Entities.LifecycleStage { Id = Guid.NewGuid(), Name = "New" },
                new Data.Entities.LifecycleStage { Id = Guid.NewGuid(), Name = "Changed" }
            };

            _mockLifecycleStageDbSet = new Mock<DbSet<Data.Entities.LifecycleStage>>();

            // Setup the DbSet mock
            var queryableLifecycleStage = _lifecycleStages.AsQueryable();
            _mockLifecycleStageDbSet.As<IQueryable<Data.Entities.LifecycleStage>>().Setup(m => m.Provider).Returns(queryableLifecycleStage.Provider);
            _mockLifecycleStageDbSet.As<IQueryable<Data.Entities.LifecycleStage>>().Setup(m => m.Expression).Returns(queryableLifecycleStage.Expression);
            _mockLifecycleStageDbSet.As<IQueryable<Data.Entities.LifecycleStage>>().Setup(m => m.ElementType).Returns(queryableLifecycleStage.ElementType);
            _mockLifecycleStageDbSet.As<IQueryable<Data.Entities.LifecycleStage>>().Setup(m => m.GetEnumerator()).Returns(queryableLifecycleStage.GetEnumerator());

            _mockDbContext.Setup(x => x.LifecycleStages).Returns(_mockLifecycleStageDbSet.Object);
        }

        [Fact]
        public void Constructor_ShouldInitializeActionTypeMap()
        {
            // Act
            var service = new ReferenceDataService(_mockLogger.Object, _mockDbContext.Object);

            // Assert
            _mockDbContext.Verify(x => x.ActionType, Times.Once);
        }

        [Fact]
        public void Constructor_WithInvalidActionTypeDescription_ThrowsArgumentException()
        {
            // Arrange
            var invalidActionTypes = new List<ActionType>
            {
                new ActionType { Id = Guid.NewGuid(), Description = "Invalid Description" }
            };

            var invalidQueryable = invalidActionTypes.AsQueryable();
            var invalidMockDbSet = new Mock<DbSet<ActionType>>();
            invalidMockDbSet.As<IQueryable<ActionType>>().Setup(m => m.Provider).Returns(invalidQueryable.Provider);
            invalidMockDbSet.As<IQueryable<ActionType>>().Setup(m => m.Expression).Returns(invalidQueryable.Expression);
            invalidMockDbSet.As<IQueryable<ActionType>>().Setup(m => m.ElementType).Returns(invalidQueryable.ElementType);
            invalidMockDbSet.As<IQueryable<ActionType>>().Setup(m => m.GetEnumerator()).Returns(invalidQueryable.GetEnumerator());

            _mockDbContext.Setup(x => x.ActionType).Returns(invalidMockDbSet.Object);

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(
                () => new ReferenceDataService(_mockLogger.Object, _mockDbContext.Object));
            Assert.Equal("Invalid action type description", exception.Message);
        }

        [Theory]
        [InlineData("No Action Required", ActionTypeEnum.NoActionRequired)]
        [InlineData("Action Required", ActionTypeEnum.ActionRequired)]
        [InlineData("Ignore", ActionTypeEnum.Ignore)]
        public void GetActionTypeId_WithValidEnum_ReturnsCorrectId(string description, ActionTypeEnum actionType)
        {
            // Arrange
            var expectedId = _actionTypes.First(x => x.Description == description).Id;
            var service = new ReferenceDataService(_mockLogger.Object, _mockDbContext.Object);

            // Act
            var result = service.GetActionTypeId(actionType);

            // Assert
            Assert.Equal(expectedId, result);
        }

        [Fact]
        public void GetActionTypeId_WithInvalidEnum_ThrowsKeyNotFoundException()
        {
            // Arrange
            var service = new ReferenceDataService(_mockLogger.Object, _mockDbContext.Object);
            var invalidEnum = (ActionTypeEnum)999;

            // Act & Assert
            var exception = Assert.Throws<KeyNotFoundException>(() => service.GetActionTypeId(invalidEnum));
            Assert.Equal($"ActionTypeEnum {invalidEnum} not found in the database.", exception.Message);
        }
    }
}



