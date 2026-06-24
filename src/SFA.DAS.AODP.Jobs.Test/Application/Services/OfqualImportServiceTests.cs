using AutoFixture;
using DocumentFormat.OpenXml.EMMA;
using Microsoft.AspNetCore.Components;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SFA.DAS.AODP.Infrastructure.Context;
using SFA.DAS.AODP.Jobs.Models;
using SFA.DAS.AODP.Jobs.Models.Jobs.FundingEligibility;
using SFA.DAS.AODP.Models.Qualification;
using System.Collections.Specialized;
using System.Text.Json;
using static SFA.DAS.AODP.Jobs.Services.QualificationProcessor;

namespace SFA.DAS.AODP.Jobs.Test.Application.Services
{
    public class OfqualImportServiceTests
    {
        private readonly Mock<ILogger<OfqualImportService>> _loggerMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<IApplicationDbContext> _dbContextMock;
        private readonly Mock<IOfqualRegisterApi> _apiClientMock;
        private readonly Mock<IOfqualRegisterService> _ofqualRegisterServiceMock;
        private readonly Mock<IQualificationsService> _qualificationsServiceMock;
        private readonly Mock<IQualificationProcessor> _qualificationProcessorMock;
        private readonly Mock<ISystemClockService> _clockServiceMock;
        private readonly FunctionContext _functionContext;
        private ApplicationDbContext _dbContext;
        private Fixture _fixture;
        private Guid LifeCycleStageNew = new Guid("00000000-0000-0000-0000-000000000001");
        private Guid LifeCycleStageChanged = new Guid("00000000-0000-0000-0000-000000000002");
        private Guid ProcessStageNoAction = new Guid("00000000-0000-0000-0000-000000000001");
        private Guid ProcessStageDecision = new Guid("00000000-0000-0000-0000-000000000002");
        private Guid ProcessStageApproved = new Guid("00000000-0000-0000-0000-000000000003");
        private Guid ProcessStageRejected = new Guid("00000000-0000-0000-0000-000000000004");
        private Guid ProcessStageHold = new Guid("00000000-0000-0000-0000-000000000005");
        private Guid ActionTypeNoAction = new Guid("00000000-0000-0000-0000-000000000001");
        private Guid ActionTypeDecision = new Guid("00000000-0000-0000-0000-000000000002");
        private Guid FundingOfferId1 = new Guid("00000000-0000-0000-0000-000000000001");
        private Guid FundingOfferId2 = new Guid("00000000-0000-0000-0000-000000000002");
        private Guid FundingOfferId3 = new Guid("00000000-0000-0000-0000-000000000003");
        private string FundingOffer1 = "Age1618";
        private string FundingOffer2 = "Age1416";
        private string FundingOffer3 = "LifelongLearningEntitlement";

        public OfqualImportServiceTests()
        {
            _loggerMock = new Mock<ILogger<OfqualImportService>>();
            _configurationMock = new Mock<IConfiguration>();
            _dbContextMock = new Mock<IApplicationDbContext>();
            _apiClientMock = new Mock<IOfqualRegisterApi>();
            _ofqualRegisterServiceMock = new Mock<IOfqualRegisterService>();
            _qualificationsServiceMock = new Mock<IQualificationsService>();            
            _functionContext = new Mock<FunctionContext>().Object;
            _qualificationProcessorMock = new Mock<IQualificationProcessor>();
            _clockServiceMock = new Mock<ISystemClockService>();
            var now = DateTime.UtcNow;
            _clockServiceMock.Setup(c => c.UtcNow).Returns(now);

            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase("ApplicationDbContext" + Guid.NewGuid()).Options;
            var configuration = new Mock<IConfiguration>();
            _dbContext = new ApplicationDbContext(options);
            _fixture = new Fixture();            
        }

        [Fact]
        public async Task OfqualImportService_ImportApiData_Should_Clear_StagedQualifications()
        {
            var _service = CreateImportServiceWithMocks();
            var requestMock = new Mock<HttpRequestData>(_functionContext);
            var searchResult = new PaginatedResult<QualificationDTO>
            {
                Results = new List<QualificationDTO>
                {
                    _fixture.Create<QualificationDTO>()
                }
            };

            _ofqualRegisterServiceMock.Setup(s => s.ParseQueryParameters(It.IsAny<NameValueCollection>()))
                .Returns(new QualificationsQueryParameters { Limit = 10 });
            _ofqualRegisterServiceMock.Setup(s => s.SearchPrivateQualificationsAsync(It.IsAny<QualificationsQueryParameters>()))
                .ReturnsAsync(searchResult);
            _qualificationsServiceMock.Setup(s => s.SaveQualificationsStagingAsync())
                .Returns(Task.CompletedTask);


            _dbContextMock.Setup(db => db.Truncate_QualificationImportStaging()).Returns(Task.CompletedTask);

            await _service.ImportApiData(requestMock.Object);

            _dbContextMock.Verify(db => db.Truncate_QualificationImportStaging(), Times.Once);
        }

        [Fact]
        public async Task OfqualImportService_ImportApiData_Should_Process_Qualifications()
        {
            var _service = CreateImportServiceWithMocks();
            var requestMock = new Mock<HttpRequestData>(_functionContext);
            var queryParams = new Dictionary<string, string> { { "param", "value" } };
            var searchResult = new PaginatedResult<QualificationDTO>
            {
                Results = new List<QualificationDTO>
                {
                    _fixture.Create<QualificationDTO>()
                }
            };

            _ofqualRegisterServiceMock.Setup(s => s.ParseQueryParameters(It.IsAny<NameValueCollection>()))
                .Returns(new QualificationsQueryParameters { Limit = 10 });
            _ofqualRegisterServiceMock.Setup(s => s.SearchPrivateQualificationsAsync(It.IsAny<QualificationsQueryParameters>()))
                .ReturnsAsync(searchResult);
            _qualificationsServiceMock.Setup(s => s.SaveQualificationsStagingAsync())
                .Returns(Task.CompletedTask);

            await _service.ImportApiData(requestMock.Object);

            _qualificationsServiceMock.Verify(s => s.SaveQualificationsStagingAsync(), Times.Once);
        }

        [Fact]
        public async Task OfqualImportService_ImportApiData_ShouldThrowApiException_WhenApiExceptionOccurs()
        {
            // Arrange
            var _service = CreateImportServiceWithMocks();
            var requestMock = new Mock<HttpRequestData>(_functionContext);
            var searchResult = new PaginatedResult<QualificationDTO>
            {
                Results = new List<QualificationDTO>
                {
                    _fixture.Create<QualificationDTO>()
                }
            };
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, "https://test.com");
            var responseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
            {
                Content = new StringContent("Bad Request")
            };
            var apiException = new ApiException(requestMessage, responseMessage, "API error");

            _ofqualRegisterServiceMock.Setup(s => s.ParseQueryParameters(It.IsAny<NameValueCollection>()))
                .Returns(new QualificationsQueryParameters { Limit = 10 });

            _ofqualRegisterServiceMock.Setup(s => s.SearchPrivateQualificationsAsync(It.IsAny<QualificationsQueryParameters>()))
                .ReturnsAsync(searchResult);

            _qualificationsServiceMock.Setup(s => s.SaveQualificationsStagingAsync())
                .Returns(Task.CompletedTask);

            _ofqualRegisterServiceMock.Setup(s => s.SearchPrivateQualificationsAsync(It.IsAny<QualificationsQueryParameters>()))
                .ThrowsAsync(apiException);

            // Act & Assert
            await Assert.ThrowsAsync<ApiException>(() => _service.ImportApiData(requestMock.Object));
        }

        [Fact]
        public async Task OfqualImportService_ImportApiData_ShouldThrowSystemException_WhenSystemExceptionOccurs()
        {
            // Arrange
            var _service = CreateImportServiceWithMocks();
            var requestMock = new Mock<HttpRequestData>(_functionContext);
            var searchResult = new PaginatedResult<QualificationDTO>
            {
                Results = new List<QualificationDTO>
                {
                    _fixture.Create<QualificationDTO>()
                }
            };
            _ofqualRegisterServiceMock.Setup(s => s.ParseQueryParameters(It.IsAny<NameValueCollection>()))
                .Returns(new QualificationsQueryParameters { Limit = 10 });

            _ofqualRegisterServiceMock.Setup(s => s.SearchPrivateQualificationsAsync(It.IsAny<QualificationsQueryParameters>()))
                .ReturnsAsync(searchResult);

            _qualificationsServiceMock.Setup(s => s.SaveQualificationsStagingAsync())
                .Returns(Task.CompletedTask);

            _ofqualRegisterServiceMock.Setup(s => s.SearchPrivateQualificationsAsync(It.IsAny<QualificationsQueryParameters>()))
                .ThrowsAsync(new SystemException("System error"));

            // Act & Assert
            await Assert.ThrowsAsync<SystemException>(() => _service.ImportApiData(requestMock.Object));
        }

        #region Fixed ProcessQualificationsDataAsync Tests

        [Fact]
        public async Task OfqualImportService_ProcessQualificationsDataAsync_NewRecord_FailFundingTest()
        {
            // Arrange
            await PopulateDbWithReferenceData();
            var _service = CreateImportServiceWithDb();

            var organisationId1 = 10001;
            var qualificationNumber1 = "qan1";
            var qualificationName1 = "Qual1";

            var importRecord = this.CreateImportRecord(organisationId1, qualificationNumber1, qualificationName1);

            _qualificationsServiceMock.Setup(s => s.GetStagedQualificationsBatchAsync(It.IsAny<int>(), 0))
                .ReturnsAsync(new List<QualificationDTO> { importRecord });
            _qualificationsServiceMock.Setup(s => s.GetStagedQualificationsBatchAsync(It.IsAny<int>(), It.Is<int>(i => i > 0)))
                .ReturnsAsync(new List<QualificationDTO>());

            _qualificationProcessorMock.Setup(p => p.Process(It.IsAny<QualificationDTO>(), null, It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .Returns((QualificationDTO dto, QualificationVersions v, Guid qId, Guid oId, bool b1, bool b2) =>
                {
                    var newVersion = new QualificationVersions
                    {
                        Id = Guid.NewGuid(),
                        QualificationId = qId,
                        Version = 1,
                        LifecycleStageId = LifeCycleStageNew,
                        ProcessStatusId = ProcessStageNoAction,
                        EqfLevel = "3",
                        Level = "3",
                        Ssa = "1.1",
                        Status = "Active",
                        SubLevel = "N/A",
                        Type = "Type"
                    };

                    var discussion = new QualificationDiscussionHistory
                    {
                        Id = Guid.NewGuid(),
                        QualificationId = qId,
                        Notes = "Failed funding eligibility check",
                        ActionTypeId = ActionTypeNoAction
                    };

                    return new QualificationProcessorResult(newVersion, discussion, new VersionFieldChanges { Id = Guid.NewGuid() }, null);
                });

            // Act
            await _service.ProcessQualificationsDataAsync();

            // Assert
            var insertedQualification = _dbContext.Qualification.Single(w => w.Qan == qualificationNumber1);
            var savedVersion = _dbContext.QualificationVersions.Single(v => v.QualificationId == insertedQualification.Id);
            Assert.Equal(ProcessStageNoAction, savedVersion.ProcessStatusId);
        }

        [Fact]
        public async Task OfqualImportService_ProcessQualificationsDataAsync_NewRecord_PassFundingTest()
        {
            // Arrange
            await PopulateDbWithReferenceData();
            var _service = CreateImportServiceWithDb();

            var organisationId1 = 10001;
            var qualificationNumber1 = "qan1";
            var qualificationName1 = "Qual1";

            var importRecord = this.CreateImportRecord(organisationId1, qualificationNumber1, qualificationName1);

            _qualificationsServiceMock.Setup(s => s.GetStagedQualificationsBatchAsync(It.IsAny<int>(), 0))
                .ReturnsAsync(new List<QualificationDTO> { importRecord });
            _qualificationsServiceMock.Setup(s => s.GetStagedQualificationsBatchAsync(It.IsAny<int>(), It.Is<int>(i => i > 0)))
                .ReturnsAsync(new List<QualificationDTO>());

            _qualificationProcessorMock.Setup(p => p.Process(It.IsAny<QualificationDTO>(), null, It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .Returns((QualificationDTO dto, QualificationVersions v, Guid qId, Guid oId, bool b1, bool b2) =>
                {
                    var newVersion = new QualificationVersions
                    {
                        Id = Guid.NewGuid(),
                        QualificationId = qId,
                        AwardingOrganisationId = oId,
                        Version = 1,
                        LifecycleStageId = LifeCycleStageNew,
                        ProcessStatusId = ProcessStageDecision,
                        EqfLevel = "3",
                        Level = "3",
                        Ssa = "1.1",
                        Status = "Active",
                        SubLevel = "N/A",
                        Type = "Type"
                    };

                    var discussion = new QualificationDiscussionHistory
                    {
                        Id = Guid.NewGuid(),
                        QualificationId = qId,
                        Notes = ImportReason.DecisionRequired,
                        ActionTypeId = ActionTypeDecision
                    };

                    return new QualificationProcessorResult(newVersion, discussion, new VersionFieldChanges { Id = Guid.NewGuid() }, null);
                });

            // Act
            await _service.ProcessQualificationsDataAsync();

            // Assert
            var insertedQualification = _dbContext.Qualification.Single(w => w.Qan == qualificationNumber1);
            var insertedVersion = _dbContext.QualificationVersions.Include(i => i.ProcessStatus).Single(w => w.QualificationId == insertedQualification.Id);
            Assert.Equal(Common.Enum.ProcessStatus.DecisionRequired, insertedVersion.ProcessStatus.Name);
        }

        [Fact]
        public async Task OfqualImportService_ProcessQualificationsDataAsync_NewRecord_ExistingOrganisation()
        {
            // Arrange
            var organisationId1 = 10001;
            var qualificationNumber1 = "qan1";
            var qualificationName1 = "Qual1";

            await PopulateDbWithReferenceData();
            await CreateOrganisation(organisationId1);
            var _service = CreateImportServiceWithDb();

            var importRecord = this.CreateImportRecord(organisationId1, qualificationNumber1, qualificationName1);

            _qualificationsServiceMock.Setup(s => s.GetStagedQualificationsBatchAsync(It.IsAny<int>(), 0))
                .ReturnsAsync(new List<QualificationDTO> { importRecord });
            _qualificationsServiceMock.Setup(s => s.GetStagedQualificationsBatchAsync(It.IsAny<int>(), It.Is<int>(i => i > 0)))
                .ReturnsAsync(new List<QualificationDTO>());

            _qualificationProcessorMock.Setup(p => p.Process(It.IsAny<QualificationDTO>(), null, It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .Returns((QualificationDTO dto, QualificationVersions v, Guid qId, Guid oId, bool b1, bool b2) =>
                {
                    var newVersion = new QualificationVersions
                    {
                        Id = Guid.NewGuid(),
                        QualificationId = qId,
                        AwardingOrganisationId = oId,
                        Version = 1,
                        LifecycleStageId = LifeCycleStageNew,
                        ProcessStatusId = ProcessStageNoAction,
                        EqfLevel = "3",
                        Level = "3",
                        Ssa = "1.1",
                        Status = "Active",
                        SubLevel = "N/A",
                        Type = "Type"
                    };

                    return new QualificationProcessorResult(newVersion, new QualificationDiscussionHistory { QualificationId = qId, Notes = "Failed funding eligibility check on: Glh", ActionTypeId = ActionTypeNoAction }, new VersionFieldChanges { Id = Guid.NewGuid() }, null);
                });

            // Act
            await _service.ProcessQualificationsDataAsync();

            // Assert
            var awardingOrganisations = _dbContext.AwardingOrganisation.Where(w => w.Ukprn == organisationId1).ToList();
            Assert.Single(awardingOrganisations);
        }

        [Fact]
        public async Task ProcessQualificationsDataAsync_ExistingRecord_EligibleForFunding_Unprocessed_FieldsChanged()
        {
            // Arrange
            var organisationId1 = 10001;
            var qualificationNumber1 = "qan1";

            await PopulateDbWithReferenceData();
            await CreateQualificationRecordSet(organisationId1, qualificationNumber1, "Qual1", processStatus: ProcessStageNoAction);
            var _service = CreateImportServiceWithDb();

            var importRecord = this.CreateImportRecord(organisationId1, qualificationNumber1, "Qual1");

            ApplyMockBehaviour(importRecord, new List<QualificationDTO> { importRecord }, true, true, new(), true, true, new List<string> { "Column1" });

            // Act
            await _service.ProcessQualificationsDataAsync();

            // Assert
            var insertedQual = await _dbContext.Qualification.SingleAsync(w => w.Qan == qualificationNumber1, CancellationToken.None);
            var insertedVersion = await _dbContext.QualificationVersions.Include(i => i.ProcessStatus).OrderByDescending(o => o.Version).FirstAsync(w => w.QualificationId == insertedQual.Id, CancellationToken.None);

            Assert.Equal(2, insertedVersion.Version);
            Assert.Equal(Common.Enum.ProcessStatus.DecisionRequired, insertedVersion.ProcessStatus.Name);
        }

        [Fact]
        public async Task OfqualImportService_ProcessQualificationsDataAsync_ExistingRecord_NotEligibleForFunding()
        {
            // Arrange
            var organisationId1 = 10001;
            var qualificationNumber1 = "qan1";

            await PopulateDbWithReferenceData();
            await CreateQualificationRecordSet(organisationId1, qualificationNumber1, "Qual1", processStatus: ProcessStageNoAction);
            var _service = CreateImportServiceWithDb();

            var importRecord = this.CreateImportRecord(organisationId1, qualificationNumber1, "Qual1");

            ApplyMockBehaviour(importRecord, new List<QualificationDTO> { importRecord }, false, false, new List<string> { "Glh" }, true, false, new List<string> { "Glh" });

            // Act
            await _service.ProcessQualificationsDataAsync();

            // Assert
            var qualification = await _dbContext.Qualification.SingleAsync(w => w.Qan == qualificationNumber1, CancellationToken.None);
            var insertedVersion = await _dbContext.QualificationVersions.OrderByDescending(o => o.Version).FirstAsync(w => w.QualificationId == qualification.Id, CancellationToken.None);
            Assert.Equal(2, insertedVersion.Version);
        }


        private void ApplyMockBehaviour(QualificationDTO importRecord, List<QualificationDTO> importRecords, bool currentlyEligible, bool previouslyEligible, List<string> ruleFields, bool changesPresent, bool keyFieldsChanged, List<string> changedFields)
        {
            _qualificationsServiceMock.Setup(s => s.GetStagedQualificationsBatchAsync(It.IsAny<int>(), 0)).ReturnsAsync(importRecords);
            _qualificationsServiceMock.Setup(s => s.GetStagedQualificationsBatchAsync(It.IsAny<int>(), It.Is<int>(i => i > 0))).ReturnsAsync(new List<QualificationDTO>());

            _qualificationProcessorMock.Setup(p => p.Process(It.IsAny<QualificationDTO>(), It.IsAny<QualificationVersions>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .Returns((QualificationDTO dto, QualificationVersions existingV, Guid qId, Guid oId, bool b1, bool b2) =>
                {
                    if (!changesPresent && previouslyEligible == currentlyEligible) return null;

                    var nextVersion = (existingV?.Version ?? 0) + 1;
                    var fieldChanges = new VersionFieldChanges { Id = Guid.NewGuid(), QualificationVersionNumber = nextVersion, ChangedFieldNames = changedFields != null ? string.Join(", ", changedFields) : "" };

                    var newVersion = new QualificationVersions
                    {
                        Id = Guid.NewGuid(),
                        QualificationId = qId,
                        AwardingOrganisationId = oId,
                        Version = nextVersion,
                        ProcessStatusId = (keyFieldsChanged || currentlyEligible != previouslyEligible) ? ProcessStageDecision : (existingV?.ProcessStatusId ?? ProcessStageNoAction),
                        LifecycleStageId = LifeCycleStageChanged,
                        VersionFieldChanges = fieldChanges,
                        VersionFieldChangesId = fieldChanges.Id,
                        EqfLevel = "3",
                        Level = "3",
                        Ssa = "1.1",
                        Status = "Active",
                        SubLevel = "N/A",
                        Type = "Type"
                    };

                    QualificationFundingTracker? tracker = null;
                    if (existingV != null && currentlyEligible && previouslyEligible && !keyFieldsChanged)
                    {
                        tracker = new QualificationFundingTracker { OldVersionId = existingV.Id, NewVersionId = newVersion.Id };
                    }

                    return new QualificationProcessorResult(newVersion, new QualificationDiscussionHistory { Id = Guid.NewGuid(), QualificationId = qId, Notes = keyFieldsChanged ? "Decision Required - Changed Qualification" : "Minor Changes", ActionTypeId = ActionTypeDecision }, fieldChanges, tracker);
                });
        }

        #endregion

      

        

        

        

        [Fact]
        public async Task OfqualImportService_ProcessQualificationsDataAsync_UpdatesQualificationTitle_WhenTitleChanged()
        {
            // Arrange
            var organisationId = 10001;
            var qualificationNumber = "qan1";
            var originalTitle = "Original Qualification Title";
            var updatedTitle = "Updated Qualification Title";

            await PopulateDbWithReferenceData();
            // This creates the existing record in the In-Memory DB
            await CreateQualificationRecordSet(organisationId, qualificationNumber, originalTitle, processStatus: ProcessStageNoAction);
            var _service = CreateImportServiceWithDb();

            // Get the existing qualification to ensure we use the correct ID in the mock
            var existingQual = await _dbContext.Qualification.SingleAsync(q => q.Qan == qualificationNumber, CancellationToken.None);
            var existingVersion = await _dbContext.QualificationVersions.SingleAsync(v => v.QualificationId == existingQual.Id, CancellationToken.None);

            // Create import record with the updated title
            var importRecord = this.CreateImportRecord(organisationId, qualificationNumber, updatedTitle);
            var importRecords = new List<QualificationDTO>() { importRecord };

            _qualificationsServiceMock.Setup(s => s.GetStagedQualificationsBatchAsync(It.IsAny<int>(), 0))
                .ReturnsAsync(importRecords);
            _qualificationsServiceMock.Setup(s => s.GetStagedQualificationsBatchAsync(It.IsAny<int>(), 1))
                .ReturnsAsync(new List<QualificationDTO>());

            // Mock the Processor for an EXISTING record update
            _qualificationProcessorMock.Setup(p => p.Process(
                It.IsAny<QualificationDTO>(),
                It.Is<QualificationVersions>(v => v.QualificationId == existingQual.Id), // Ensure service finds the right version
                existingQual.Id,
                It.IsAny<Guid>(),
                It.IsAny<bool>(),
                It.IsAny<bool>()))
                .Returns((QualificationDTO dto, QualificationVersions v, Guid qId, Guid oId, bool b1, bool b2) =>
                {
                    var fieldChanges = new VersionFieldChanges
                    {
                        Id = Guid.NewGuid(),
                        QualificationVersionNumber = 2,
                        ChangedFieldNames = "Title, Glh, Status"
                    };

                    var newVersion = new QualificationVersions
                    {
                        Id = Guid.NewGuid(),
                        QualificationId = qId,
                        AwardingOrganisationId = oId,
                        Version = 2,
                        LifecycleStageId = LifeCycleStageChanged,
                        ProcessStatusId = ProcessStageNoAction,
                        VersionFieldChanges = fieldChanges,
                        VersionFieldChangesId = fieldChanges.Id,
                        // EF Core Required Fields
                        EqfLevel = "3",
                        Level = "3",
                        Ssa = "1.1",
                        Status = "Active",
                        SubLevel = "N/A",
                        Type = "Type"
                    };

                    var discussion = new QualificationDiscussionHistory
                    {
                        Id = Guid.NewGuid(),
                        QualificationId = qId,
                        Notes = "Minor Data Update - No Review Needed | Changes: Title, Glh, Status",
                        ActionTypeId = ActionTypeNoAction
                    };

                    return new QualificationProcessorResult(newVersion, discussion, fieldChanges, null);
                });

            // Act
            await _service.ProcessQualificationsDataAsync();

            // Assert
            // Check that qualification title was updated
            var updatedQualification = await _dbContext.Qualification.Where(w => w.Qan == qualificationNumber).SingleAsync(CancellationToken.None);
            Assert.Equal(qualificationNumber, updatedQualification.Qan);
            Assert.Equal(updatedTitle, updatedQualification.QualificationName);
            Assert.NotEqual(originalTitle, updatedQualification.QualificationName);

            // Verify a new qualification version was created
            var insertedVersion = await _dbContext.QualificationVersions
                                    .Include(i => i.ProcessStatus)
                                    .OrderByDescending(o => o.Version)
                                    .Where(w => w.QualificationId == updatedQualification.Id)
                                    .FirstAsync(CancellationToken.None);
            Assert.NotNull(insertedVersion);
            Assert.Equal(2, insertedVersion.Version);

            // Verify version field changes contain Title
            var versionFieldChange = await _dbContext.VersionFieldChanges
                                    .Where(w => w.QualificationVersionNumber == insertedVersion.Version)
                                    .FirstAsync(CancellationToken.None);
            Assert.NotNull(versionFieldChange);
            Assert.Contains("Title", versionFieldChange.ChangedFieldNames);
        }

        [Fact]
        public async Task OfqualImportService_ProcessQualificationsDataAsync_ExistingRecord_NoChangesDetected()
        {
            //Arrange
            var organisationId1 = 10001;
            var qualificationNumber1 = "qan1";
            var qualificationName1 = "Qual1";

            await PopulateDbWithReferenceData();
            await CreateQualificationRecordSet(organisationId1, qualificationNumber1, qualificationName1, processStatus: ProcessStageNoAction);
            var _service = CreateImportServiceWithDb();

            var importRecord = this.CreateImportRecord(organisationId1, qualificationNumber1, qualificationName1);
            var importRecords = new List<QualificationDTO>() { importRecord };

            ApplyMockBehaviour(
                importRecord,
                importRecords,
                previouslyEligible: false,
                currentlyEligible: false,
                ruleFields: new(),
                changesPresent: false,
                changedFields: new(),
                keyFieldsChanged: false);

            var initialVersionCount = await _dbContext.QualificationVersions.CountAsync(CancellationToken.None);

            //Act
            await _service.ProcessQualificationsDataAsync();

            //Assert
            var finalVersionCount = await _dbContext.QualificationVersions.CountAsync(CancellationToken.None);
            Assert.Equal(initialVersionCount, finalVersionCount); // Should remain 1
        }

        [Fact]
        public async Task OfqualImportService_ProcessQualificationsDataAsync_ExistingRecord_FundingsNotCopied_WhenApproved()
        {
            // Arrange
            var organisationId1 = 10001;
            var qualificationNumber1 = "qan1";
            var qualificationName1 = "Qual1";

            await PopulateDbWithReferenceData();
            // Creates Version 1 with 'Approved' status
            await CreateQualificationRecordSet(organisationId1, qualificationNumber1, qualificationName1, processStatus: ProcessStageApproved);
            var _service = CreateImportServiceWithDb();

            var qualification = await _dbContext.Qualification
                                        .Include(i => i.QualificationVersions)
                                        .SingleAsync(w => w.Qan == qualificationNumber1, CancellationToken.None);

            var oldVersion = qualification.QualificationVersions.First();
            await CreateFundingOffers(qualification.QualificationVersions.ToList());

            var importRecord = this.CreateImportRecord(organisationId1, qualificationNumber1, qualificationName1);
            var importRecords = new List<QualificationDTO>() { importRecord };

            _qualificationsServiceMock
                .SetupSequence(s => s.GetStagedQualificationsBatchAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(importRecords) // Call 1
                .ReturnsAsync(new List<QualificationDTO>()); // Call 2 (Breaks the loop)

            // Mock the Processor for an update that specifically DOES NOT copy funding
            _qualificationProcessorMock.Setup(p => p.Process(
                It.IsAny<QualificationDTO>(),
                It.Is<QualificationVersions>(v => v.Id == oldVersion.Id),
                qualification.Id,
                It.IsAny<Guid>(),
                It.IsAny<bool>(),
                It.IsAny<bool>()
                ))
                .Returns((QualificationDTO dto, QualificationVersions v, Guid qId, Guid oId, bool b1, bool b2) =>
                {
                    var fieldChanges = new VersionFieldChanges
                    {
                        Id = Guid.NewGuid(),
                        QualificationVersionNumber = 2,
                        ChangedFieldNames = "Title"
                    };

                    var newVersion = new QualificationVersions
                    {
                        Id = Guid.NewGuid(),
                        QualificationId = qId,
                        AwardingOrganisationId = oId,
                        Version = 2,
                        LifecycleStageId = LifeCycleStageChanged,
                        ProcessStatusId = ProcessStageDecision, // Decision Required
                        VersionFieldChanges = fieldChanges,
                        VersionFieldChangesId = fieldChanges.Id,
                        // EF Core Required Fields
                        EqfLevel = "3",
                        Level = "3",
                        Ssa = "1.1",
                        Status = "Active",
                        SubLevel = "N/A",
                        Type = "Type"
                    };

                    var discussion = new QualificationDiscussionHistory
                    {
                        Id = Guid.NewGuid(),
                        QualificationId = qId,
                        Notes = "Key Fields or Eligibility Changed - Re-review required",
                        ActionTypeId = ActionTypeDecision
                    };

                    // CRITICAL: tracker is NULL so funding is NOT copied
                    return new QualificationProcessorResult(newVersion, discussion, fieldChanges, null);
                });

            // Act
            await _service.ProcessQualificationsDataAsync();

            // Assert
            var insertedQualification = await _dbContext.Qualification.SingleAsync(w => w.Qan == qualificationNumber1, CancellationToken.None);

            var insertedVersion = await _dbContext.QualificationVersions
                                    .Include(i => i.ProcessStatus)
                                    .Include(i => i.LifecycleStage)
                                    .OrderByDescending(o => o.Version)
                                    .FirstAsync(w => w.QualificationId == insertedQualification.Id, CancellationToken.None);

            Assert.NotNull(insertedVersion);
            Assert.Equal(2, insertedVersion.Version);
            Assert.Equal(Common.Enum.ProcessStatus.DecisionRequired, insertedVersion.ProcessStatus.Name);
            Assert.Equal(Common.Enum.LifeCycleStage.Changed, insertedVersion.LifecycleStage.Name);

            // Check funding offers have NOT been copied (Should be empty)
            var fundings = await _dbContext.QualificationFundings.Where(w => w.QualificationVersionId == insertedVersion.Id).ToListAsync(CancellationToken.None);
            Assert.Empty(fundings);

            // Check funding offers feedbacks have NOT been copied (Should be empty)
            var feedbacks = await _dbContext.QualificationFundingFeedbacks.Where(w => w.QualificationVersionId == insertedVersion.Id).ToListAsync(CancellationToken.None);
            Assert.Empty(feedbacks);
        }

        [Fact]
        public async Task OfqualImportService_ProcessQualificationsDataAsync_ExistingRecord_CopyFunding()
        {
            // Arrange
            var organisationId1 = 10001;
            var qualificationNumber1 = "qan1";
            var qualificationName1 = "Qual1";

            await PopulateDbWithReferenceData();
            // Creates Version 1 with 'On Hold' status
            await CreateQualificationRecordSet(organisationId1, qualificationNumber1, qualificationName1, processStatus: ProcessStageHold);
            var _service = CreateImportServiceWithDb();

            var qualification = await _dbContext.Qualification
                                        .Include(i => i.QualificationVersions)
                                        .SingleAsync(w => w.Qan == qualificationNumber1, CancellationToken.None);

            var oldVersion = qualification.QualificationVersions.First();
            await CreateFundingOffers(qualification.QualificationVersions.ToList());

            var importRecord = this.CreateImportRecord(organisationId1, qualificationNumber1, qualificationName1);
            var importRecords = new List<QualificationDTO>() { importRecord };

            _qualificationsServiceMock
                .SetupSequence(s => s.GetStagedQualificationsBatchAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(importRecords) // Call 1
                .ReturnsAsync(new List<QualificationDTO>()); // Call 2 (Breaks the loop)

            // Mock the Processor for an existing record update where funding should be tracked/copied
            _qualificationProcessorMock.Setup(p => p.Process(
                It.IsAny<QualificationDTO>(),                             // 1. importRecord
                It.Is<QualificationVersions>(v => v.Id == oldVersion.Id), // 2. existingVersion
                qualification.Id,                                         // 3. qualificationId
                It.IsAny<Guid>(),                                         // 4. organisationId
                It.IsAny<bool>(),                                         // 5. hasActiveApps
                It.IsAny<bool>()                                          // 6. hasActiveFunding
                ))
                .Returns((QualificationDTO dto, QualificationVersions v, Guid qId, Guid oId, bool b1, bool b2) =>
                {
                    var fieldChanges = new VersionFieldChanges
                    {
                        Id = Guid.NewGuid(),
                        QualificationVersionNumber = 2,
                        ChangedFieldNames = "Column1"
                    };

                    var newVersion = new QualificationVersions
                    {
                        Id = Guid.NewGuid(),
                        QualificationId = qId,
                        AwardingOrganisationId = oId,
                        Version = 2,
                        LifecycleStageId = v.LifecycleStageId,
                        ProcessStatusId = v.ProcessStatusId,
                        VersionFieldChanges = fieldChanges,
                        VersionFieldChangesId = fieldChanges.Id,
                        EqfLevel = "3",
                        Level = "3",
                        Ssa = "1.1",
                        Status = "Active",
                        SubLevel = "N/A",
                        Type = "Type",
                        Name = dto.Title,
                        FundingEligibilityFailedFields = string.Empty
                    };

                    var discussion = new QualificationDiscussionHistory
                    {
                        Id = Guid.NewGuid(),
                        QualificationId = qId,
                        Notes = "Update to Record In-Review (Major) | Changes: Column1",
                        ActionTypeId = ActionTypeDecision
                    };

                    var tracker = new QualificationFundingTracker
                    {
                        OldVersionId = v.Id,
                        NewVersionId = newVersion.Id
                    };

                    return new QualificationProcessorResult(newVersion, discussion, fieldChanges, tracker);
                });

            // Act
            await _service.ProcessQualificationsDataAsync();

            // Assert
            var insertedQualification = await _dbContext.Qualification.SingleAsync(w => w.Qan == qualificationNumber1, CancellationToken.None);

            var insertedVersion = await _dbContext.QualificationVersions
                                    .Include(i => i.ProcessStatus)
                                    .Include(i => i.LifecycleStage)
                                    .OrderByDescending(o => o.Version)
                                    .FirstAsync(w => w.QualificationId == insertedQualification.Id, CancellationToken.None);

            Assert.NotNull(insertedVersion);
            Assert.Equal(2, insertedVersion.Version);

            var originalVersion = await _dbContext.QualificationVersions
                                    .Include(i => i.ProcessStatus)
                                    .Include(i => i.LifecycleStage)
                                    .SingleAsync(w => w.QualificationId == insertedQualification.Id && w.Version == 1, CancellationToken.None);

            // Verify Status and Stage were preserved as requested
            Assert.Equal(originalVersion.ProcessStatus.Name, insertedVersion.ProcessStatus.Name);
            Assert.Equal(originalVersion.LifecycleStage.Name, insertedVersion.LifecycleStage.Name);

            // Check funding offers have been copied
            var fundings = await _dbContext.QualificationFundings.Where(w => w.QualificationVersionId == insertedVersion.Id).ToListAsync(CancellationToken.None);
            Assert.NotNull(fundings);
            Assert.Equal(2, fundings.Count);

            // Check funding offers feedbacks have been copied
            var feedbacks = await _dbContext.QualificationFundingFeedbacks.Where(w => w.QualificationVersionId == insertedVersion.Id).ToListAsync(CancellationToken.None);
            Assert.NotNull(feedbacks);
            Assert.Single(feedbacks);
        }

        [Fact]
        public async Task OfqualImportService_Should_Update_AwardingOrganisation_When_Details_Change()
        {
            // Arrange
            await PopulateDbWithReferenceData();

            var organisationId = 10001;
            var qan = "qan1";

            await CreateQualificationRecordSet(
                organisationId,
                qan,
                "Qual1",
                ProcessStageNoAction);

            var service = CreateImportServiceWithDb();

            var importRecord = CreateImportRecord(organisationId, qan, "Qual1");

            // simulate change in organisation fields
            importRecord.OrganisationName = "Updated Name";
            importRecord.OrganisationAcronym = "NEW";
            importRecord.OrganisationRecognitionNumber = "RN-999";

            var importRecords = new List<QualificationDTO> { importRecord };

            ApplyMockBehaviour(
                importRecord,
                importRecords,
                currentlyEligible: false,
                previouslyEligible: false,
                [],
                changesPresent: true,
                keyFieldsChanged: true,
                []);

            // Act
            await service.ProcessQualificationsDataAsync();

            // Assert
            var org = await _dbContext.AwardingOrganisation
                .SingleAsync(x => x.Ukprn == organisationId);

            Assert.Equal("Updated Name", org.NameOfqual);
            Assert.Equal("NEW", org.Acronym);
            Assert.Equal("RN-999", org.RecognitionNumber);
        }



        [Fact]
        public async Task OfqualImportService_Should_Not_Create_Duplicate_AwardingOrganisation_When_Updated()
        {
            // Arrange
            await PopulateDbWithReferenceData();

            var organisationId = 10001;

            await CreateQualificationRecordSet(
                organisationId,
                "qan1",
                "Qual1",
                ProcessStageNoAction);

            var service = CreateImportServiceWithDb();

            var importRecord = CreateImportRecord(organisationId, "qan1", "Qual1");
            importRecord.OrganisationName = "Updated Name";
            var importRecords = new List<QualificationDTO> { importRecord };

            ApplyMockBehaviour(
                importRecord,
                importRecords,
                currentlyEligible: false,
                previouslyEligible: false,
                [],
                changesPresent: true,
                keyFieldsChanged: false,
                []);

            // Act
            await service.ProcessQualificationsDataAsync();

            // Assert
            var count = await _dbContext.AwardingOrganisation
                .CountAsync(x => x.Ukprn == organisationId);

            Assert.Equal(1, count);
        }

        [Fact]
        public async Task OfqualImportService_Should_Not_Update_AwardingOrganisation_When_No_Changes_Detected()
        {
            // Arrange
            await PopulateDbWithReferenceData();

            var organisationId = 10001;

            await CreateQualificationRecordSet(
                organisationId,
                "qan1",
                "Qual1",
                ProcessStageNoAction);

            var service = CreateImportServiceWithDb();

            var importRecord = CreateImportRecord(organisationId, "qan1", "Qual1");

            var importRecords = new List<QualificationDTO> { importRecord };

            ApplyMockBehaviour(
                importRecord,
                importRecords,
                currentlyEligible: false,
                previouslyEligible: false,
                [],
                changesPresent: false,
                keyFieldsChanged: false,
                []);

            var original = await _dbContext.AwardingOrganisation
                .SingleAsync(x => x.Ukprn == organisationId);

            // Act
            await service.ProcessQualificationsDataAsync();

            // Assert
            var updated = await _dbContext.AwardingOrganisation
                .SingleAsync(x => x.Ukprn == organisationId);

            Assert.Equal(original.NameOfqual, updated.NameOfqual);
            Assert.Equal(original.Acronym, updated.Acronym);
            Assert.Equal(original.RecognitionNumber, updated.RecognitionNumber);
        }

        [Fact]
        public async Task OfqualImportService_Should_Update_AwardingOrganisation_When_Only_Acronym_Changes()
        {
            // Arrange
            await PopulateDbWithReferenceData();

            var organisationId = 10001;

            await CreateQualificationRecordSet(
                organisationId,
                "qan1",
                "Qual1",
                ProcessStageNoAction);

            var service = CreateImportServiceWithDb();

            var importRecord = CreateImportRecord(organisationId, "qan1", "Qual1");

            importRecord.OrganisationAcronym = "NEW-ACR";
            var importRecords = new List<QualificationDTO> { importRecord };

            ApplyMockBehaviour(
                importRecord,
                importRecords,
                currentlyEligible: false,
                previouslyEligible: false,
                [],
                changesPresent: true,
                keyFieldsChanged: true,
                []);

            // Act
            await service.ProcessQualificationsDataAsync();

            // Assert
            var org = await _dbContext.AwardingOrganisation
                .SingleAsync(x => x.Ukprn == organisationId);

            Assert.Equal("NEW-ACR", org.Acronym);
        }

        [Fact]
        public async Task OfqualImportService_Should_Update_AwardingOrganisation_When_Only_RecognitionNumber_Changes()
        {
            // Arrange
            await PopulateDbWithReferenceData();

            var organisationId = 10001;

            await CreateQualificationRecordSet(
                organisationId,
                "qan1",
                "Qual1",
                ProcessStageNoAction);

            var service = CreateImportServiceWithDb();

            var importRecord = CreateImportRecord(organisationId, "qan1", "Qual1");

            importRecord.OrganisationRecognitionNumber = "RN-UPDATED";

            var importRecords = new List<QualificationDTO> { importRecord };

            ApplyMockBehaviour(
                importRecord,
                importRecords,
                currentlyEligible: false,
                previouslyEligible: false,
                [],
                changesPresent: true,
                keyFieldsChanged: true,
                []);

            // Act
            await service.ProcessQualificationsDataAsync();

            // Assert
            var org = await _dbContext.AwardingOrganisation
                .SingleAsync(x => x.Ukprn == organisationId);

            Assert.Equal("RN-UPDATED", org.RecognitionNumber);
        }

        [Fact]
        public async Task OfqualImportService_Should_Not_Update_Anything_When_No_Changes()
        {
            // Arrange
            await PopulateDbWithReferenceData();

            var organisationId = 10001;
            var qan = "qan1";
            var name = "Qual1";

            await CreateQualificationRecordSet(
                organisationId,
                qan,
                name,
                processStatus: ProcessStageNoAction);

            var service = CreateImportServiceWithDb();

            var importRecord = CreateImportRecord(organisationId, qan, name);
            var importRecords = new List<QualificationDTO> { importRecord };

            ApplyMockBehaviour(
                importRecord,
                importRecords,
                currentlyEligible: false,
                previouslyEligible: false,
                [],
                changesPresent: false,
                keyFieldsChanged: false,
    []);

            var versionCountBefore = await _dbContext.QualificationVersions.CountAsync();
            var discussionCountBefore = await _dbContext.QualificationDiscussionHistory.CountAsync();

            // Act
            await service.ProcessQualificationsDataAsync();

            // Assert (only observable behaviour)
            var versionCountAfter = await _dbContext.QualificationVersions.CountAsync();
            var discussionCountAfter = await _dbContext.QualificationDiscussionHistory.CountAsync();

            Assert.Equal(versionCountBefore, versionCountAfter);
            Assert.Equal(discussionCountBefore, discussionCountAfter);

            var qualification = await _dbContext.Qualification.SingleAsync(x => x.Qan == qan);
            Assert.Equal(name, qualification.QualificationName);
        }

        private async Task CreateFundingOffers(List<QualificationVersions> qualificationVersions)
        {
            var qualificationVersion = qualificationVersions.OrderByDescending(o => o.Version).First();
           await _dbContext.QualificationFundings.AddAsync(new QualificationFunding()
            {
                Id = Guid.NewGuid(),
                QualificationVersionId = qualificationVersion.Id,
                FundingOfferId = FundingOfferId1,
                StartDate = new DateOnly(2015, 06,01),
                EndDate = new DateOnly(2030, 01,02),
                Comments = "TestFunding1"
            });

            await _dbContext.QualificationFundings.AddAsync(new QualificationFunding()
            {
                Id = Guid.NewGuid(),
                QualificationVersionId = qualificationVersion.Id,
                FundingOfferId = FundingOfferId2,
                StartDate = new DateOnly(2020, 03, 01),
                EndDate = new DateOnly(2029, 04, 02),
                Comments = "TestFunding2"
            });

            await _dbContext.QualificationFundingFeedbacks.AddAsync(new QualificationFundingFeedback()
            {
                Approved = true,
                Id = Guid.NewGuid(),
                QualificationVersionId = qualificationVersion.Id,
                Comments = "TestFeedback"
            });
            await _dbContext.SaveChangesAsync();
        }

        private OfqualImportService CreateImportServiceWithMocks()
        {            
            return new OfqualImportService(
                _loggerMock.Object,
                _configurationMock.Object,
                _dbContextMock.Object,
                _apiClientMock.Object,
                _ofqualRegisterServiceMock.Object,
                _qualificationsServiceMock.Object,
                _qualificationProcessorMock.Object,
                _clockServiceMock.Object
            );
        }

        private OfqualImportService CreateImportServiceWithDb()
        {
            return new OfqualImportService(
                _loggerMock.Object,
                _configurationMock.Object,
                _dbContext,
                _apiClientMock.Object,
                _ofqualRegisterServiceMock.Object,
                _qualificationsServiceMock.Object,
                _qualificationProcessorMock.Object,
                _clockServiceMock.Object
            );
        }

        private async Task CreateOrganisation(int organisationId)
        {
            var qan1_organisation = _fixture.Build<AwardingOrganisation>()                
                .With(w => w.Qualifications, new List<Qualifications>())
                .With(w => w.QualificationVersions, new List<QualificationVersions>())
                .With(w => w.Ukprn, organisationId)
                .Create();           

            var organisations = new List<AwardingOrganisation>() { qan1_organisation };

            await _dbContext.AddRangeAsync(organisations);
            await _dbContext.SaveChangesAsync();
        }

        private async Task CreateQualificationRecordSet(int organisationId, string qualificationNumber, string qualificationName, Guid processStatus)
        {
            var orgId = Guid.NewGuid();
            var qan1_organisation = _fixture.Build<AwardingOrganisation>()
                .Without(w => w.Qualifications)
                .Without(w => w.QualificationVersions)
                .With(w => w.Ukprn, organisationId)
                .With(w => w.Id, orgId)
                .Create();

            var qan1_qualification = _fixture.Build<Qualification>()                                
                .Without(w => w.Qualifications)
                .Without(w => w.QualificationDiscussionHistories)
                .Without(w => w.QualificationVersions)
                .With(w => w.QualificationName, qualificationName)
                .With(w => w.Qan, qualificationNumber)
                .Create();

            var qan1_qualificationVersionFieldChange1 = _fixture.Build<VersionFieldChanges>()
                .Without(w => w.QualificationVersions)
                .With(w => w.QualificationVersionNumber, 1)
                .With(w => w.ChangedFieldNames, "Glh, Status")
                .Create();

            var qan1_qualificationVersion1 = _fixture.Build<QualificationVersions>()
                .Without(w => w.Qualification)
                .Without(w => w.Organisation)
                .Without(w => w.LifecycleStage)
                .Without(w => w.ProcessStatus)
                .With(w => w.VersionFieldChanges, qan1_qualificationVersionFieldChange1)
                .With(w => w.Version, 1)
                .With(w => w.QualificationId, qan1_qualification.Id)
                .With(w => w.AwardingOrganisationId, qan1_organisation.Id)
                .With(w => w.OfferedInEngland, true)
                .With(w => w.Glh, 5)
                .With(w => w.Tqt, 10)
                .With(w => w.LifecycleStageId, LifeCycleStageNew)
                .With(w => w.ProcessStatusId, processStatus)
                .Create();            

            var organisations = new List<AwardingOrganisation>() { qan1_organisation };
            var qualifications = new List<Qualification>() { qan1_qualification };
            var qualificationVersions = new List<QualificationVersions>() { qan1_qualificationVersion1 };
            var qualificationVersionFieldChanges = new List<VersionFieldChanges>() { qan1_qualificationVersionFieldChange1 };

            await _dbContext.AddRangeAsync(organisations);            
            await _dbContext.AddRangeAsync(qualifications);            
            await _dbContext.AddRangeAsync(qualificationVersions);
            await _dbContext.AddRangeAsync(qualificationVersionFieldChanges);
            await _dbContext.SaveChangesAsync();
        }       

        private QualificationDTO CreateImportRecord(int organisationId, string qan, string qualificationName)
        {
            var qualificationDTO = _fixture.Build<QualificationDTO>()      
                .With(w => w.OrganisationId, organisationId)
                .With(w => w.Title, qualificationName)
                .With(w => w.QualificationNumberNoObliques, qan)
                .With(w => w.OfferedInEngland, true)
                .With(w => w.Glh, 5)
                .With(w => w.Tqt, 10)
                .With(w => w.AssessmentMethods, [])
                .Create();

            return qualificationDTO;
        }

        private async Task PopulateDbWithReferenceData()
        {
            var actionType1 = new ActionType() { Description = "No Action Required", Id = ActionTypeNoAction };
            var actionType2 = new ActionType() { Description = "Action Required", Id = ActionTypeDecision };
            var actionType3 = new ActionType() { Description = "Ignore", Id = Guid.NewGuid() };
            await _dbContext.AddRangeAsync(new List<ActionType>() { actionType1, actionType2, actionType3});
            await _dbContext.SaveChangesAsync();

            var processStatus1 = new Data.Entities.ProcessStatus() { Name = Common.Enum.ProcessStatus.DecisionRequired, Id = ProcessStageDecision };
            var processStatus2 = new Data.Entities.ProcessStatus() { Name = Common.Enum.ProcessStatus.NoActionRequired, Id = ProcessStageNoAction };
            var processStatus3 = new Data.Entities.ProcessStatus() { Name = Common.Enum.ProcessStatus.OnHold, Id = ProcessStageHold };
            var processStatus4 = new Data.Entities.ProcessStatus() { Name = Common.Enum.ProcessStatus.Rejected, Id = ProcessStageRejected };
            var processStatus5 = new Data.Entities.ProcessStatus() { Name = Common.Enum.ProcessStatus.Approved, Id = ProcessStageApproved };
            await _dbContext.AddRangeAsync(new List<Data.Entities.ProcessStatus>() { processStatus1, processStatus2, processStatus3, processStatus4, processStatus5 });
            await _dbContext.SaveChangesAsync();

            var lifecycle1 = new Data.Entities.LifecycleStage() { Name = Common.Enum.LifeCycleStage.New, Id = LifeCycleStageNew };
            var lifecycle2 = new Data.Entities.LifecycleStage() { Name = Common.Enum.LifeCycleStage.Changed, Id = LifeCycleStageChanged };
            await _dbContext.AddRangeAsync(new List<Data.Entities.LifecycleStage>() { lifecycle1, lifecycle2 });
            await _dbContext.SaveChangesAsync();

            var fundingOffer1 = new Data.Entities.FundingOffer() { Id = FundingOfferId1, Name = FundingOffer1 };
            var fundingOffer2 = new Data.Entities.FundingOffer() { Id = FundingOfferId2, Name = FundingOffer2 };
            var fundingOffer3 = new Data.Entities.FundingOffer() { Id = FundingOfferId3, Name = FundingOffer3 };
            await _dbContext.AddRangeAsync(new List<Data.Entities.FundingOffer>() { fundingOffer1, fundingOffer2, fundingOffer3 });
            await _dbContext.SaveChangesAsync();
        }
    }

}


