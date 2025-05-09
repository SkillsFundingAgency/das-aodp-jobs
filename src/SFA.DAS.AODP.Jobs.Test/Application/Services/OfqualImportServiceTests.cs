﻿using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using SFA.DAS.AODP.Jobs.Services;
using SFA.DAS.AODP.Jobs.Interfaces;
using SFA.DAS.AODP.Infrastructure.Context;
using Microsoft.Azure.Functions.Worker.Http;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Jobs.Client;
using SFA.DAS.AODP.Models.Qualification;
using SFA.DAS.AODP.Data;
using System.Collections.Specialized;
using Microsoft.Azure.Functions.Worker;
using AutoFixture;
using RestEase;
using SFA.DAS.AODP.Data.Repositories.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components;
using System.Text.Json;
using SFA.DAS.AODP.Infrastructure.Interfaces;
using SFA.DAS.AODP.Common.Enum;
using SFA.DAS.AODP.Infrastructure.Services;

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
        private IReferenceDataService _actionTypeServiceMock;
        private readonly Mock<IFundingEligibilityService> _fundingEligibilityService;
        private readonly Mock<IChangeDetectionService> _changeDetectionServiceMock;
        private readonly FunctionContext _functionContext;
        private ApplicationDbContext _dbContext;
        private JobsRepository _repository;
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
            _fundingEligibilityService = new Mock<IFundingEligibilityService>();
            _changeDetectionServiceMock = new Mock<IChangeDetectionService>();
            _functionContext = new Mock<FunctionContext>().Object;
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

        [Fact]
        public async Task OfqualImportService_ProcessQualificationsDataAsync_NewRecord_FailFundingTest()
        {
            //Arrange
            await PopulateDbWithReferenceData();
            var _service = CreateImportServiceWithDb();

            var organisationId1 = 10001;
            var qualificationNumber1 = "qan1";
            var qualificationName1 = "Qual1";
           
            var importRecord = this.CreateImportRecord(organisationId1, qualificationNumber1, qualificationName1);
            var importRecords = new List<QualificationDTO>() { importRecord };

            _qualificationsServiceMock.Setup(s => s.GetStagedQualificationsBatchAsync(It.IsAny<int>(), 0)).ReturnsAsync(importRecords);
            _qualificationsServiceMock.Setup(s => s.GetStagedQualificationsBatchAsync(It.IsAny<int>(), 1)).ReturnsAsync(new List<QualificationDTO>());
            _fundingEligibilityService.Setup(s => s.EligibleForFunding(It.Is<QualificationDTO>(q => q.QualificationNumberNoObliques == importRecord.QualificationNumberNoObliques)))
                                        .Returns(false);
            _fundingEligibilityService.Setup(s => s.DetermineFailureReason(It.Is<QualificationDTO>(q => q.QualificationNumberNoObliques == importRecord.QualificationNumberNoObliques)))
                                        .Returns(ImportReason.NoAction);
            
            //Act
            await _service.ProcessQualificationsDataAsync();

            //Assert
            // new qualification
            var insertedQualification = _dbContext.Qualification.Where(w => w.Qan == qualificationNumber1).Single();     
            Assert.NotNull(insertedQualification);
            Assert.Equal(qualificationNumber1, insertedQualification.Qan);
            Assert.Equal(qualificationName1, insertedQualification.QualificationName);

            // new organisation
            var insertedAwardingOrganisation = _dbContext.AwardingOrganisation.Where(w => w.Ukprn == organisationId1).Single();
            Assert.NotNull (insertedAwardingOrganisation);

            // new qualification version
            var insertedVersion = _dbContext.QualificationVersions
                                    .Include(i => i.ProcessStatus)
                                    .Where(w => w.QualificationId == insertedQualification.Id).Single();
            Assert.NotNull(insertedVersion);
            Assert.Equal(Common.Enum.ProcessStatus.NoActionRequired, insertedVersion.ProcessStatus.Name);
            Assert.Equal(Common.Enum.LifeCycleStage.New, insertedVersion.LifecycleStage.Name);

            // new qualification discussion
            var insertedDiscussion = _dbContext.QualificationDiscussionHistory
                                    .Include(i => i.ActionType)                                    
                                    .Where(w => w.QualificationId == insertedQualification.Id).Single();
            Assert.NotNull(insertedDiscussion);
            Assert.Equal("No Action Required", insertedDiscussion.ActionType.Description);
            Assert.Equal(ImportReason.NoAction, insertedDiscussion.Notes);
        }

        [Fact]
        public async Task OfqualImportService_ProcessQualificationsDataAsync_NewRecord_PassFundingTest()
        {
            //Arrange
            await PopulateDbWithReferenceData();
            var _service = CreateImportServiceWithDb();

            var organisationId1 = 10001;
            var qualificationNumber1 = "qan1";
            var qualificationName1 = "Qual1";

            var importRecord = this.CreateImportRecord(organisationId1, qualificationNumber1, qualificationName1);
            var importRecords = new List<QualificationDTO>() { importRecord };

            _qualificationsServiceMock.Setup(s => s.GetStagedQualificationsBatchAsync(It.IsAny<int>(), 0)).ReturnsAsync(importRecords);
            _qualificationsServiceMock.Setup(s => s.GetStagedQualificationsBatchAsync(It.IsAny<int>(), 1)).ReturnsAsync(new List<QualificationDTO>());
            _fundingEligibilityService.Setup(s => s.EligibleForFunding(It.Is<QualificationDTO>(q => q.QualificationNumberNoObliques == importRecord.QualificationNumberNoObliques)))
                                        .Returns(true);
            _fundingEligibilityService.Setup(s => s.DetermineFailureReason(It.Is<QualificationDTO>(q => q.QualificationNumberNoObliques == importRecord.QualificationNumberNoObliques)))
                                        .Returns(ImportReason.NoAction);

            //Act
            await _service.ProcessQualificationsDataAsync();

            //Assert
            // new qualification
            var insertedQualification = _dbContext.Qualification.Where(w => w.Qan == qualificationNumber1).First();
            Assert.NotNull(insertedQualification);
            Assert.Equal(qualificationNumber1, insertedQualification.Qan);
            Assert.Equal(qualificationName1, insertedQualification.QualificationName);

            // new organisation
            var insertedAwardingOrganisation = _dbContext.AwardingOrganisation.Where(w => w.Ukprn == organisationId1).Single();
            Assert.NotNull(insertedAwardingOrganisation);

            // new qualification version
            var insertedVersion = _dbContext.QualificationVersions
                                    .Include(i => i.ProcessStatus)
                                    .Where(w => w.QualificationId == insertedQualification.Id).Single();
            Assert.NotNull(insertedVersion);
            Assert.Equal(Common.Enum.ProcessStatus.DecisionRequired, insertedVersion.ProcessStatus.Name);
            Assert.Equal(Common.Enum.LifeCycleStage.New, insertedVersion.LifecycleStage.Name);

            // new qualification discussion
            var insertedDiscussion = _dbContext.QualificationDiscussionHistory
                                    .Include(i => i.ActionType)
                                    .Where(w => w.QualificationId == insertedQualification.Id).Single();
            Assert.NotNull(insertedDiscussion);
            Assert.Equal("Action Required", insertedDiscussion.ActionType.Description);
            Assert.Equal(ImportReason.DecisionRequired, insertedDiscussion.Notes);
        }

        [Fact]
        public async Task OfqualImportService_ProcessQualificationsDataAsync_NewRecord_ExistingOrganisation()
        {
            //Arrange
            var organisationId1 = 10001;
            var qualificationNumber1 = "qan1";
            var qualificationName1 = "Qual1";

            await PopulateDbWithReferenceData();
            await CreateOrganisation(organisationId1);
            var _service = CreateImportServiceWithDb();            

            var importRecord = this.CreateImportRecord(organisationId1, qualificationNumber1, qualificationName1);
            var importRecords = new List<QualificationDTO>() { importRecord };

            _qualificationsServiceMock.Setup(s => s.GetStagedQualificationsBatchAsync(It.IsAny<int>(), 0)).ReturnsAsync(importRecords);
            _qualificationsServiceMock.Setup(s => s.GetStagedQualificationsBatchAsync(It.IsAny<int>(), 1)).ReturnsAsync(new List<QualificationDTO>());
            _fundingEligibilityService.Setup(s => s.EligibleForFunding(It.Is<QualificationDTO>(q => q.QualificationNumberNoObliques == importRecord.QualificationNumberNoObliques)))
                                        .Returns(false);
            _fundingEligibilityService.Setup(s => s.DetermineFailureReason(It.Is<QualificationDTO>(q => q.QualificationNumberNoObliques == importRecord.QualificationNumberNoObliques)))
                                        .Returns(ImportReason.NoAction);

            //Act
            await _service.ProcessQualificationsDataAsync();

            //Assert
            // new qualification
            var insertedQualification = _dbContext.Qualification.Where(w => w.Qan == qualificationNumber1).First();
            Assert.NotNull(insertedQualification);
            Assert.Equal(qualificationNumber1, insertedQualification.Qan);
            Assert.Equal(qualificationName1, insertedQualification.QualificationName);

            // No new organisations
            var awardingOrganisations = _dbContext.AwardingOrganisation.Where(w => w.Ukprn == organisationId1).ToList();
            Assert.Single(awardingOrganisations);

            // new qualification version
            var insertedVersion = _dbContext.QualificationVersions
                                    .Include(i => i.ProcessStatus)
                                    .Where(w => w.QualificationId == insertedQualification.Id).First();
            Assert.NotNull(insertedVersion);
            Assert.Equal(Common.Enum.ProcessStatus.NoActionRequired, insertedVersion.ProcessStatus.Name);
            Assert.Equal(Common.Enum.LifeCycleStage.New, insertedVersion.LifecycleStage.Name);

            // new qualification discussion
            var insertedDiscussion = _dbContext.QualificationDiscussionHistory
                                    .Include(i => i.ActionType)
                                    .Where(w => w.QualificationId == insertedQualification.Id).First();
            Assert.NotNull(insertedDiscussion);
            Assert.Equal("No Action Required", insertedDiscussion.ActionType.Description);
            Assert.Equal(ImportReason.NoAction, insertedDiscussion.Notes);
        }

        [Fact]
        public async Task OfqualImportService_ProcessQualificationsDataAsync_ExistingRecord_NotEligibleForFunding()
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
            ApplyMockBehaviour(importRecord, importRecords, eligibleForFunding: false, changesPresent: true, keyFieldsChanged: false);

            //Act
            await _service.ProcessQualificationsDataAsync();

            //Assert
            // existing qualification
            var insertedQualification = await _dbContext.Qualification.Where(w => w.Qan == qualificationNumber1).SingleAsync();
            Assert.Equal(qualificationNumber1, insertedQualification.Qan);
            Assert.Equal(qualificationName1, insertedQualification.QualificationName);

            // No new organisations
            var awardingOrganisations = await _dbContext.AwardingOrganisation.Where(w => w.Ukprn == organisationId1).ToListAsync();
            Assert.Single(awardingOrganisations);

            // new qualification version
            var insertedVersion = await _dbContext.QualificationVersions
                                    .Include(i => i.ProcessStatus)
                                    .OrderByDescending(o => o.Version)
                                    .Where(w => w.QualificationId == insertedQualification.Id)
                                    .FirstAsync();
            Assert.NotNull(insertedVersion);
            Assert.Equal(2, insertedVersion.Version);
            Assert.Equal(Common.Enum.ProcessStatus.NoActionRequired, insertedVersion.ProcessStatus.Name);
            Assert.Equal(Common.Enum.LifeCycleStage.Changed, insertedVersion.LifecycleStage.Name);

            // new qualification discussion
            var insertedDiscussion = await _dbContext.QualificationDiscussionHistory
                                    .Include(i => i.ActionType)
                                    .OrderByDescending(o => o.Timestamp)
                                    .Where(w => w.QualificationId == insertedQualification.Id)
                                    .FirstAsync();
            Assert.NotNull(insertedDiscussion);
            Assert.Equal("No Action Required", insertedDiscussion.ActionType.Description);
            Assert.Equal("No Action required - Changed Qualification (Funding Criteria)", insertedDiscussion.Notes);
        }

        private void ApplyMockBehaviour(QualificationDTO importRecord, List<QualificationDTO> importRecords, bool eligibleForFunding, bool changesPresent, bool keyFieldsChanged, string failureReason = ImportReason.NoAction)
        {
            _qualificationsServiceMock.Setup(s => s.GetStagedQualificationsBatchAsync(It.IsAny<int>(), 0)).ReturnsAsync(importRecords);
            _qualificationsServiceMock.Setup(s => s.GetStagedQualificationsBatchAsync(It.IsAny<int>(), 1)).ReturnsAsync(new List<QualificationDTO>());
            _fundingEligibilityService.Setup(s => s.EligibleForFunding(It.Is<QualificationDTO>(q => q.QualificationNumberNoObliques == importRecord.QualificationNumberNoObliques)))
                                        .Returns(eligibleForFunding);
            _fundingEligibilityService.Setup(s => s.DetermineFailureReason(It.Is<QualificationDTO>(q => q.QualificationNumberNoObliques == importRecord.QualificationNumberNoObliques)))
                                        .Returns(failureReason);
            _changeDetectionServiceMock.Setup(s => s.DetectChanges(It.IsAny<QualificationDTO>(), It.IsAny<QualificationVersions>(), It.IsAny<AwardingOrganisation>(), It.IsAny<Qualification>()))
                                        .Returns(new ChangeDetectionService.DetectionResults() { ChangesPresent = changesPresent, Fields = new List<string>() { "Glh", "Status" }, KeyFieldsChanged = keyFieldsChanged });
        }

        [Fact]
        public async Task ProcessQualificationsDataAsync_ExistingRecord_EligibleForFunding_Unprocessed_FieldsChanged()
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
          
            ApplyMockBehaviour(importRecord, importRecords, eligibleForFunding: true, changesPresent: true, keyFieldsChanged: false);

            //Act
            await _service.ProcessQualificationsDataAsync();

            //Assert
            // existing qualification
            var insertedQualification = await _dbContext.Qualification.Where(w => w.Qan == qualificationNumber1).SingleAsync();
            Assert.Equal(qualificationNumber1, insertedQualification.Qan);
            Assert.Equal(qualificationName1, insertedQualification.QualificationName);

            // No new organisations
            var awardingOrganisations = await _dbContext.AwardingOrganisation.Where(w => w.Ukprn == organisationId1).ToListAsync();
            Assert.Single(awardingOrganisations);

            // new qualification version
            var insertedVersion = await _dbContext.QualificationVersions
                                    .Include(i => i.ProcessStatus)
                                    .OrderByDescending(o => o.Version)
                                    .Where(w => w.QualificationId == insertedQualification.Id)
                                    .FirstAsync();
            Assert.NotNull(insertedVersion);
            Assert.Equal(2, insertedVersion.Version);
            Assert.Equal(Common.Enum.ProcessStatus.DecisionRequired, insertedVersion.ProcessStatus.Name);
            Assert.Equal(Common.Enum.LifeCycleStage.Changed, insertedVersion.LifecycleStage.Name);

            // new qualification discussion
            var insertedDiscussion = await _dbContext.QualificationDiscussionHistory
                                    .Include(i => i.ActionType)
                                    .OrderByDescending(o => o.Timestamp)
                                    .Where(w => w.QualificationId == insertedQualification.Id)
                                    .FirstAsync();
            Assert.NotNull(insertedDiscussion);
            Assert.Equal("Action Required", insertedDiscussion.ActionType.Description);
            Assert.Equal("Decision Required - Changed Qualification", insertedDiscussion.Notes);
        }

        [Fact]
        public async Task ProcessQualificationsDataAsync_ExistingRecord_EligibleForFunding_Approved_KeyFieldsChanged()
        {
            //Arrange
            var organisationId1 = 10001;
            var qualificationNumber1 = "qan1";
            var qualificationName1 = "Qual1";

            await PopulateDbWithReferenceData();
            await CreateQualificationRecordSet(organisationId1, qualificationNumber1, qualificationName1, processStatus: ProcessStageApproved);
            var _service = CreateImportServiceWithDb();

            var importRecord = this.CreateImportRecord(organisationId1, qualificationNumber1, qualificationName1);
            var importRecords = new List<QualificationDTO>() { importRecord };

            ApplyMockBehaviour(importRecord, importRecords, eligibleForFunding: true, changesPresent: true, keyFieldsChanged: true);

            //Act
            await _service.ProcessQualificationsDataAsync();

            //Assert
            // existing qualification
            var insertedQualification = await _dbContext.Qualification.Where(w => w.Qan == qualificationNumber1).SingleAsync();
            Assert.Equal(qualificationNumber1, insertedQualification.Qan);
            Assert.Equal(qualificationName1, insertedQualification.QualificationName);

            // No new organisations
            var awardingOrganisations = await _dbContext.AwardingOrganisation.Where(w => w.Ukprn == organisationId1).ToListAsync();
            Assert.Single(awardingOrganisations);

            // new qualification version
            var insertedVersion = await _dbContext.QualificationVersions
                                    .Include(i => i.ProcessStatus)
                                    .OrderByDescending(o => o.Version)
                                    .Where(w => w.QualificationId == insertedQualification.Id)
                                    .FirstAsync();
            Assert.NotNull(insertedVersion);
            Assert.Equal(2, insertedVersion.Version);
            Assert.Equal(Common.Enum.ProcessStatus.DecisionRequired, insertedVersion.ProcessStatus.Name);
            Assert.Equal(Common.Enum.LifeCycleStage.Changed, insertedVersion.LifecycleStage.Name);

            // new qualification discussion
            var insertedDiscussion = await _dbContext.QualificationDiscussionHistory
                                    .Include(i => i.ActionType)
                                    .OrderByDescending(o => o.Timestamp)
                                    .Where(w => w.QualificationId == insertedQualification.Id)
                                    .FirstAsync();
            Assert.NotNull(insertedDiscussion);
            Assert.Equal("Action Required", insertedDiscussion.ActionType.Description);
            Assert.Equal("Decision Required - Changed Qualification (Key Fields)", insertedDiscussion.Notes);
        }

        [Fact]
        public async Task ProcessQualificationsDataAsync_ExistingRecord_EligibleForFunding_Approved_MinorFieldsChanged()
        {
            //Arrange
            var organisationId1 = 10001;
            var qualificationNumber1 = "qan1";
            var qualificationName1 = "Qual1";

            await PopulateDbWithReferenceData();
            await CreateQualificationRecordSet(organisationId1, qualificationNumber1, qualificationName1, processStatus: ProcessStageApproved);       

            var _service = CreateImportServiceWithDb();

            var importRecord = this.CreateImportRecord(organisationId1, qualificationNumber1, qualificationName1);
            var importRecords = new List<QualificationDTO>() { importRecord };

            ApplyMockBehaviour(importRecord, importRecords, eligibleForFunding: true, changesPresent: true, keyFieldsChanged: false);

            //Act
            await _service.ProcessQualificationsDataAsync();

            //Assert
            // existing qualification
            var insertedQualification = await _dbContext.Qualification.Where(w => w.Qan == qualificationNumber1).SingleAsync();
            Assert.Equal(qualificationNumber1, insertedQualification.Qan);
            Assert.Equal(qualificationName1, insertedQualification.QualificationName);

            // No new organisations
            var awardingOrganisations = await _dbContext.AwardingOrganisation.Where(w => w.Ukprn == organisationId1).ToListAsync();
            Assert.Single(awardingOrganisations);

            // new qualification version
            var insertedVersion = await _dbContext.QualificationVersions
                                    .Include(i => i.ProcessStatus)
                                    .OrderByDescending(o => o.Version)
                                    .Where(w => w.QualificationId == insertedQualification.Id)
                                    .FirstAsync();
            Assert.NotNull(insertedVersion);
            Assert.Equal(2, insertedVersion.Version);

            // Process status remains the same as the old version
            var oldVersion = await _dbContext.QualificationVersions
                                    .Include(i => i.ProcessStatus)                                    
                                    .Where(w => w.QualificationId == insertedQualification.Id && w.Version == 1)
                                    .FirstAsync();
            Assert.Equal(insertedVersion.ProcessStatus.Name, oldVersion.ProcessStatus.Name);
            Assert.Equal(Common.Enum.LifeCycleStage.Changed, insertedVersion.LifecycleStage.Name);

            // new qualification discussion
            var insertedDiscussion = await _dbContext.QualificationDiscussionHistory
                                    .Include(i => i.ActionType)
                                    .OrderByDescending(o => o.Timestamp)
                                    .Where(w => w.QualificationId == insertedQualification.Id)
                                    .FirstAsync();
            Assert.NotNull(insertedDiscussion);
            Assert.Equal("Action Required", insertedDiscussion.ActionType.Description);
            Assert.Equal("Decision Required - Changed Qualification (Minor Fields)", insertedDiscussion.Notes);
        }

        [Fact]
        public async Task ProcessQualificationsDataAsync_ExistingRecord_EligibleForFunding_OnHold_KeyFieldsChanged()
        {
            //Arrange
            var organisationId1 = 10001;
            var qualificationNumber1 = "qan1";
            var qualificationName1 = "Qual1";

            await PopulateDbWithReferenceData();
            await CreateQualificationRecordSet(organisationId1, qualificationNumber1, qualificationName1, processStatus: ProcessStageHold);
            var _service = CreateImportServiceWithDb();

            var importRecord = this.CreateImportRecord(organisationId1, qualificationNumber1, qualificationName1);
            var importRecords = new List<QualificationDTO>() { importRecord };

            ApplyMockBehaviour(importRecord, importRecords, eligibleForFunding: true, changesPresent: true, keyFieldsChanged: true);

            //Act
            await _service.ProcessQualificationsDataAsync();

            //Assert
            // existing qualification
            var insertedQualification = await _dbContext.Qualification.Where(w => w.Qan == qualificationNumber1).SingleAsync();
            Assert.Equal(qualificationNumber1, insertedQualification.Qan);
            Assert.Equal(qualificationName1, insertedQualification.QualificationName);

            // No new organisations
            var awardingOrganisations = await _dbContext.AwardingOrganisation.Where(w => w.Ukprn == organisationId1).ToListAsync();
            Assert.Single(awardingOrganisations);

            // new qualification version
            var insertedVersion = await _dbContext.QualificationVersions
                                    .Include(i => i.ProcessStatus)
                                    .OrderByDescending(o => o.Version)
                                    .Where(w => w.QualificationId == insertedQualification.Id)
                                    .FirstAsync();
            Assert.NotNull(insertedVersion);
            Assert.Equal(2, insertedVersion.Version);
            // Process status and lifecycle status remains the same as the old version
            var oldVersion = await _dbContext.QualificationVersions
                                    .Include(i => i.ProcessStatus)
                                    .Include(i => i.LifecycleStage)
                                    .Where(w => w.QualificationId == insertedQualification.Id && w.Version == 1)
                                    .FirstAsync();
            Assert.Equal(oldVersion.ProcessStatus.Name, insertedVersion.ProcessStatus.Name);
            Assert.Equal(oldVersion.LifecycleStage.Name, insertedVersion.LifecycleStage.Name);

            // new qualification discussion
            var insertedDiscussion = await _dbContext.QualificationDiscussionHistory
                                    .Include(i => i.ActionType)
                                    .OrderByDescending(o => o.Timestamp)
                                    .Where(w => w.QualificationId == insertedQualification.Id)
                                    .FirstAsync();
            Assert.NotNull(insertedDiscussion);
            Assert.Equal("Action Required", insertedDiscussion.ActionType.Description);
            Assert.Equal("On Hold - Changed Qualification (Key Fields)", insertedDiscussion.Notes);
        }

        [Fact]
        public async Task ProcessQualificationsDataAsync_ExistingRecord_EligibleForFunding_OnHold_MinorFieldsChanged()
        {
            //Arrange
            var organisationId1 = 10001;
            var qualificationNumber1 = "qan1";
            var qualificationName1 = "Qual1";

            await PopulateDbWithReferenceData();
            await CreateQualificationRecordSet(organisationId1, qualificationNumber1, qualificationName1, processStatus: ProcessStageHold);
            var _service = CreateImportServiceWithDb();

            var importRecord = this.CreateImportRecord(organisationId1, qualificationNumber1, qualificationName1);
            var importRecords = new List<QualificationDTO>() { importRecord };

            ApplyMockBehaviour(importRecord, importRecords, eligibleForFunding: true, changesPresent: true, keyFieldsChanged: false);

            //Act
            await _service.ProcessQualificationsDataAsync();

            //Assert
            // existing qualification
            var insertedQualification = await _dbContext.Qualification.Where(w => w.Qan == qualificationNumber1).SingleAsync();
            Assert.Equal(qualificationNumber1, insertedQualification.Qan);
            Assert.Equal(qualificationName1, insertedQualification.QualificationName);

            // No new organisations
            var awardingOrganisations = await _dbContext.AwardingOrganisation.Where(w => w.Ukprn == organisationId1).ToListAsync();
            Assert.Single(awardingOrganisations);

            // new qualification version
            var insertedVersion = await _dbContext.QualificationVersions
                                    .Include(i => i.ProcessStatus)
                                    .OrderByDescending(o => o.Version)
                                    .Where(w => w.QualificationId == insertedQualification.Id)
                                    .FirstAsync();
            Assert.NotNull(insertedVersion);
            Assert.Equal(2, insertedVersion.Version);
            // Process status remains the same as the old version
            var oldVersion = await _dbContext.QualificationVersions
                                    .Include(i => i.ProcessStatus)
                                    .Include(i => i.LifecycleStage)
                                    .Where(w => w.QualificationId == insertedQualification.Id && w.Version == 1)
                                    .FirstAsync();
            Assert.Equal(oldVersion.ProcessStatus.Name, insertedVersion.ProcessStatus.Name);
            Assert.Equal(oldVersion.LifecycleStage.Name, insertedVersion.LifecycleStage.Name);

            // new qualification discussion
            var insertedDiscussion = await _dbContext.QualificationDiscussionHistory
                                    .Include(i => i.ActionType)
                                    .OrderByDescending(o => o.Timestamp)
                                    .Where(w => w.QualificationId == insertedQualification.Id)
                                    .FirstAsync();
            Assert.NotNull(insertedDiscussion);
            Assert.Equal("Action Required", insertedDiscussion.ActionType.Description);
            Assert.Equal("Decision Required - Changed Qualification (Minor Fields)", insertedDiscussion.Notes);
        }

        [Fact]
        public async Task OfqualImportService_ProcessQualificationsDataAsync_UpdatesQualificationTitle_WhenTitleChanged()
        {
            //Arrange
            var organisationId = 10001;
            var qualificationNumber = "qan1";
            var originalTitle = "Original Qualification Title";
            var updatedTitle = "Updated Qualification Title";

            await PopulateDbWithReferenceData();
            await CreateQualificationRecordSet(organisationId, qualificationNumber, originalTitle, processStatus: ProcessStageNoAction);
            var _service = CreateImportServiceWithDb();

            // Create import record with the updated title
            var importRecord = this.CreateImportRecord(organisationId, qualificationNumber, updatedTitle);
            var importRecords = new List<QualificationDTO>() { importRecord };

            _qualificationsServiceMock.Setup(s => s.GetStagedQualificationsBatchAsync(It.IsAny<int>(), 0))
                                       .ReturnsAsync(importRecords);
            _qualificationsServiceMock.Setup(s => s.GetStagedQualificationsBatchAsync(It.IsAny<int>(), 1))
                                       .ReturnsAsync(new List<QualificationDTO>());

            // Set up funding eligibility (can be either eligible or not eligible)
            _fundingEligibilityService.Setup(s => s.EligibleForFunding(It.Is<QualificationDTO>(q =>
                                            q.QualificationNumberNoObliques == importRecord.QualificationNumberNoObliques)))
                                      .Returns(false);

            _fundingEligibilityService.Setup(s => s.DetermineFailureReason(It.Is<QualificationDTO>(q =>
                                            q.QualificationNumberNoObliques == importRecord.QualificationNumberNoObliques)))
                                      .Returns(ImportReason.NoAction);

            // Important: Setup change detection to include "Title" in the changed fields
            _changeDetectionServiceMock.Setup(s => s.DetectChanges(
                                            It.IsAny<QualificationDTO>(),
                                            It.IsAny<QualificationVersions>(),
                                            It.IsAny<AwardingOrganisation>(),
                                            It.IsAny<Qualification>()))
                                      .Returns(new ChangeDetectionService.DetectionResults()
                                      {
                                          ChangesPresent = true,
                                          Fields = new List<string>() { "Title", "Glh", "Status" }
                                      });

            //Act
            await _service.ProcessQualificationsDataAsync();

            //Assert
            // Check that qualification title was updated
            var updatedQualification = await _dbContext.Qualification.Where(w => w.Qan == qualificationNumber).SingleAsync();
            Assert.Equal(qualificationNumber, updatedQualification.Qan);
            Assert.Equal(updatedTitle, updatedQualification.QualificationName);
            Assert.NotEqual(originalTitle, updatedQualification.QualificationName);

            // Verify a new qualification version was created
            var insertedVersion = await _dbContext.QualificationVersions
                                    .Include(i => i.ProcessStatus)
                                    .OrderByDescending(o => o.Version)
                                    .Where(w => w.QualificationId == updatedQualification.Id)
                                    .FirstAsync();
            Assert.NotNull(insertedVersion);
            Assert.Equal(2, insertedVersion.Version);

            // Verify version field changes contain Title
            var versionFieldChange = await _dbContext.VersionFieldChanges
                                    .Where(w => w.QualificationVersionNumber == insertedVersion.Version)
                                    .FirstAsync();
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
                        
            ApplyMockBehaviour(importRecord, importRecords, eligibleForFunding: false, changesPresent: false, keyFieldsChanged: false, failureReason: ImportReason.NoAction);
            var initialVersionCount = await _dbContext.QualificationVersions.CountAsync();
            var initialDiscussionCount = await _dbContext.QualificationDiscussionHistory.CountAsync();

            //Act
            await _service.ProcessQualificationsDataAsync();

            //Assert
            // Existing qualification should remain unchanged
            var qualification = await _dbContext.Qualification.Where(w => w.Qan == qualificationNumber1).SingleAsync();
            Assert.Equal(qualificationNumber1, qualification.Qan);
            Assert.Equal(qualificationName1, qualification.QualificationName);

            // No new organisations
            var awardingOrganisations = await _dbContext.AwardingOrganisation.Where(w => w.Ukprn == organisationId1).ToListAsync();
            Assert.Single(awardingOrganisations);

            // No new qualification versions should be created
            var finalVersionCount = await _dbContext.QualificationVersions.CountAsync();
            Assert.Equal(initialVersionCount, finalVersionCount);

            // No new discussion entries should be created
            var finalDiscussionCount = await _dbContext.QualificationDiscussionHistory.CountAsync();
            Assert.Equal(initialDiscussionCount, finalDiscussionCount);

            // The original version should still be the latest
            var latestVersion = await _dbContext.QualificationVersions
                                    .Include(i => i.ProcessStatus)
                                    .OrderByDescending(o => o.Version)
                                    .Where(w => w.QualificationId == qualification.Id)
                                    .FirstAsync();
            Assert.Equal(1, latestVersion.Version);
        }

        [Fact]
        public async Task ProcessQualificationsDataAsync_ExistingRecord_CopyFunding()
        {
            //Arrange
            var organisationId1 = 10001;
            var qualificationNumber1 = "qan1";
            var qualificationName1 = "Qual1";

            await PopulateDbWithReferenceData();
            await CreateQualificationRecordSet(organisationId1, qualificationNumber1, qualificationName1, processStatus: ProcessStageHold);
            var _service = CreateImportServiceWithDb();

            var qualification = await _dbContext.Qualification.Where(w => w.Qan == qualificationNumber1)
                                        .Include(i => i.QualificationVersions)
                                .SingleAsync();

            await CreateFundingOffers(qualification.QualificationVersions.ToList());

            var importRecord = this.CreateImportRecord(organisationId1, qualificationNumber1, qualificationName1);
            var importRecords = new List<QualificationDTO>() { importRecord };

            ApplyMockBehaviour(importRecord, importRecords, eligibleForFunding: true, changesPresent: true, keyFieldsChanged: true);

            //Act
            await _service.ProcessQualificationsDataAsync();

            //Assert
            // existing qualification
            var insertedQualification = await _dbContext.Qualification.Where(w => w.Qan == qualificationNumber1).SingleAsync();            

            // new qualification version
            var insertedVersion = await _dbContext.QualificationVersions
                                    .Include(i => i.ProcessStatus)
                                    .OrderByDescending(o => o.Version)
                                    .Where(w => w.QualificationId == insertedQualification.Id)
                                    .FirstAsync();
            Assert.NotNull(insertedVersion);
            Assert.Equal(2, insertedVersion.Version);
            
            var oldVersion = await _dbContext.QualificationVersions
                                    .Include(i => i.ProcessStatus)
                                    .Include(i => i.LifecycleStage)
                                    .Where(w => w.QualificationId == insertedQualification.Id && w.Version == 1)
                                    .FirstAsync();
            Assert.Equal(oldVersion.ProcessStatus.Name, insertedVersion.ProcessStatus.Name);
            Assert.Equal(oldVersion.LifecycleStage.Name, insertedVersion.LifecycleStage.Name);

            //Check funding offers have been copied
            var fundings = await _dbContext.QualificationFundings.Where(w => w.QualificationVersionId == insertedVersion.Id).ToListAsync();
            Assert.NotNull(fundings);
            Assert.Equal(2, fundings.Count);

            //Check funding offers feedbacks have been copied
            var feedbacks = await _dbContext.QualificationFundingFeedbacks.Where(w => w.QualificationVersionId == insertedVersion.Id).ToListAsync();
            Assert.NotNull(feedbacks);
            Assert.Single(feedbacks);
        }

        [Fact]
        public async Task ProcessQualificationsDataAsync_ExistingRecord_FundingsNotCopied_WhenApproved()
        {
            //Arrange
            var organisationId1 = 10001;
            var qualificationNumber1 = "qan1";
            var qualificationName1 = "Qual1";

            await PopulateDbWithReferenceData();
            await CreateQualificationRecordSet(organisationId1, qualificationNumber1, qualificationName1, processStatus: ProcessStageApproved);
            var _service = CreateImportServiceWithDb();

            var qualification = await _dbContext.Qualification.Where(w => w.Qan == qualificationNumber1)
                                        .Include(i => i.QualificationVersions)
                                .SingleAsync();
            await CreateFundingOffers(qualification.QualificationVersions.ToList());

            var importRecord = this.CreateImportRecord(organisationId1, qualificationNumber1, qualificationName1);
            var importRecords = new List<QualificationDTO>() { importRecord };

            ApplyMockBehaviour(importRecord, importRecords, eligibleForFunding: true, changesPresent: true, keyFieldsChanged: true);

            //Act
            await _service.ProcessQualificationsDataAsync();

            //Assert
            // existing qualification
            var insertedQualification = await _dbContext.Qualification.Where(w => w.Qan == qualificationNumber1).SingleAsync();

            // new qualification version
            var insertedVersion = await _dbContext.QualificationVersions
                                    .Include(i => i.ProcessStatus)
                                    .OrderByDescending(o => o.Version)
                                    .Where(w => w.QualificationId == insertedQualification.Id)
                                    .FirstAsync();
            Assert.NotNull(insertedVersion);
            Assert.Equal(2, insertedVersion.Version);
            Assert.Equal(Common.Enum.ProcessStatus.DecisionRequired, insertedVersion.ProcessStatus.Name);
            Assert.Equal(Common.Enum.LifeCycleStage.Changed, insertedVersion.LifecycleStage.Name);

            //Check funding offers have NOT been copied
            var fundings = await _dbContext.QualificationFundings.Where(w => w.QualificationVersionId == insertedVersion.Id).ToListAsync();
            Assert.Empty(fundings);       

            //Check funding offers feedbacks have NOT been copied
            var feedbacks = await _dbContext.QualificationFundingFeedbacks.Where(w => w.QualificationVersionId == insertedVersion.Id).ToListAsync();
            Assert.Empty(feedbacks);         
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
            _actionTypeServiceMock = new Mock<IReferenceDataService>().Object;

            return new OfqualImportService(
                _loggerMock.Object,
                _configurationMock.Object,
                _dbContextMock.Object,
                _apiClientMock.Object,
                _ofqualRegisterServiceMock.Object,
                _qualificationsServiceMock.Object,
                _actionTypeServiceMock,
                _fundingEligibilityService.Object,
                _changeDetectionServiceMock.Object
            );
        }

        private OfqualImportService CreateImportServiceWithDb()
        {
            var refdatalogger = new Mock<ILogger<ReferenceDataService>>();
            _actionTypeServiceMock = new ReferenceDataService(refdatalogger.Object, _dbContext);

            return new OfqualImportService(
                _loggerMock.Object,
                _configurationMock.Object,
                _dbContext,
                _apiClientMock.Object,
                _ofqualRegisterServiceMock.Object,
                _qualificationsServiceMock.Object,
                _actionTypeServiceMock,
                _fundingEligibilityService.Object,
                _changeDetectionServiceMock.Object
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
                .With(w => w.OperationalStartDate, QualificationReference.MinOperationalDate)
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
            var doc = JsonDocument.Parse("null");
            var root = doc.RootElement;

            var qualificationDTO = _fixture.Build<QualificationDTO>()      
                .With(w => w.OrganisationId, organisationId)
                .With(w => w.Title, qualificationName)
                .With(w => w.QualificationNumberNoObliques, qan)
                .With(w => w.OfferedInEngland, true)
                .With(w => w.Glh, 5)
                .With(w => w.Tqt, 10)
                .With(w => w.AssessmentMethods, root)
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


