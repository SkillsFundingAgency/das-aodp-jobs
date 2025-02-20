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
using SFA.DAS.AODP.Jobs.Enum;
using Microsoft.AspNetCore.Components;
using System.Text.Json;
using Newtonsoft.Json.Linq;

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
        private readonly FunctionContext _functionContext;
        private ApplicationDbContext _dbContext;
        private JobsRepository _repository;
        private Fixture _fixture;

        public OfqualImportServiceTests()
        {
            _loggerMock = new Mock<ILogger<OfqualImportService>>();
            _configurationMock = new Mock<IConfiguration>();
            _dbContextMock = new Mock<IApplicationDbContext>();
            _apiClientMock = new Mock<IOfqualRegisterApi>();
            _ofqualRegisterServiceMock = new Mock<IOfqualRegisterService>();
            _qualificationsServiceMock = new Mock<IQualificationsService>();            
            _fundingEligibilityService = new Mock<IFundingEligibilityService>();
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
            _qualificationsServiceMock.Setup(s => s.SaveQualificationsStagingAsync(It.IsAny<List<string>>()))
                .Returns(Task.CompletedTask);


            _dbContextMock.Setup(db => db.TruncateTable<QualificationImportStaging>()).Returns(Task.CompletedTask);

            await _service.ImportApiData(requestMock.Object);

            _dbContextMock.Verify(db => db.TruncateTable<QualificationImportStaging>(), Times.Once);
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
            _qualificationsServiceMock.Setup(s => s.SaveQualificationsStagingAsync(It.IsAny<List<string>>()))
                .Returns(Task.CompletedTask);

            await _service.ImportApiData(requestMock.Object);

            _qualificationsServiceMock.Verify(s => s.SaveQualificationsStagingAsync(It.IsAny<List<string>>()), Times.Once);
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

            _qualificationsServiceMock.Setup(s => s.SaveQualificationsStagingAsync(It.IsAny<List<string>>()))
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

            _qualificationsServiceMock.Setup(s => s.SaveQualificationsStagingAsync(It.IsAny<List<string>>()))
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
            Assert.Equal(Enum.ProcessStatus.NoActionRequired, insertedVersion.ProcessStatus.Name);
            Assert.Equal(Enum.LifeCycleStage.New, insertedVersion.LifecycleStage.Name);

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
            Assert.Equal(Enum.ProcessStatus.DecisionRequired, insertedVersion.ProcessStatus.Name);
            Assert.Equal(Enum.LifeCycleStage.New, insertedVersion.LifecycleStage.Name);

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
            Assert.Equal(Enum.ProcessStatus.NoActionRequired, insertedVersion.ProcessStatus.Name);
            Assert.Equal(Enum.LifeCycleStage.New, insertedVersion.LifecycleStage.Name);

            // new qualification discussion
            var insertedDiscussion = _dbContext.QualificationDiscussionHistory
                                    .Include(i => i.ActionType)
                                    .Where(w => w.QualificationId == insertedQualification.Id).First();
            Assert.NotNull(insertedDiscussion);
            Assert.Equal("No Action Required", insertedDiscussion.ActionType.Description);
            Assert.Equal(ImportReason.NoAction, insertedDiscussion.Notes);
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
                _fundingEligibilityService.Object
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
                _fundingEligibilityService.Object
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

        private async Task CreateQualificationRecordSet(int organisationId, string qualificationNumber, string qualificationName)
        {
            var qan1_organisation = _fixture.Build<AwardingOrganisation>()
                .With(w => w.Ukprn, organisationId)
                .With(w => w.Qualifications, new List<Qualifications>())
                .With(w => w.QualificationVersions, new List<QualificationVersions>())
                .Create();

            var qan1_qualification = _fixture.Build<Qualification>()
                .With(w => w.Qan, qualificationNumber)
                .With(w => w.QualificationName, qualificationName)
                .With(w => w.QualificationVersions, new List<QualificationVersions>())
                .Create();

            var qan1_qualificationVersion1 = _fixture.Build<QualificationVersions>()
                .With(w => w.Version, 1)
                .With(w => w.QualificationId, qan1_qualification.Id)
                .With(w => w.AwardingOrganisationId, qan1_organisation.Id)
                .With(w => w.OfferedInEngland, true)
                .With(w => w.Glh, 5)
                .With(w => w.Tqt, 10)
                .With(w => w.OperationalStartDate, QualificationReference.MinOperationalDate)
                .Create();

            var organisations = new List<AwardingOrganisation>() { qan1_organisation };
            var qualifications = new List<Qualification>() { qan1_qualification };
            var qualificationVersions = new List<QualificationVersions>() { qan1_qualificationVersion1 };
            
            await _dbContext.AddRangeAsync(organisations);
            await _dbContext.SaveChangesAsync();
            await _dbContext.AddRangeAsync(qualifications);
            await _dbContext.SaveChangesAsync();
            await _dbContext.AddRangeAsync(qualificationVersions);
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
            var actionType1 = new ActionType() { Description = "No Action Required", Id = Guid.NewGuid() };
            var actionType2 = new ActionType() { Description = "Action Required", Id = Guid.NewGuid() };
            var actionType3 = new ActionType() { Description = "Ignore", Id = Guid.NewGuid() };
            await _dbContext.AddRangeAsync(new List<ActionType>() { actionType1, actionType2, actionType3});
            await _dbContext.SaveChangesAsync();

            var processStatus1 = new Data.Entities.ProcessStatus() { Name = Enum.ProcessStatus.DecisionRequired, Id = Guid.NewGuid() };
            var processStatus2 = new Data.Entities.ProcessStatus() { Name = Enum.ProcessStatus.NoActionRequired, Id = Guid.NewGuid() };
            var processStatus3 = new Data.Entities.ProcessStatus() { Name = Enum.ProcessStatus.Hold, Id = Guid.NewGuid() };
            var processStatus4 = new Data.Entities.ProcessStatus() { Name = Enum.ProcessStatus.Rejected, Id = Guid.NewGuid() };
            var processStatus5 = new Data.Entities.ProcessStatus() { Name = Enum.ProcessStatus.Approved, Id = Guid.NewGuid() };
            await _dbContext.AddRangeAsync(new List<Data.Entities.ProcessStatus>() { processStatus1, processStatus2, processStatus3, processStatus4, processStatus5 });
            await _dbContext.SaveChangesAsync();

            var lifecycle1 = new Data.Entities.LifecycleStage() { Name = Enum.LifeCycleStage.New, Id = Guid.NewGuid() };
            var lifecycle2 = new Data.Entities.LifecycleStage() { Name = Enum.LifeCycleStage.Changed, Id = Guid.NewGuid() };
            await _dbContext.AddRangeAsync(new List<Data.Entities.LifecycleStage>() { lifecycle1, lifecycle2 });
            await _dbContext.SaveChangesAsync();            
        }
    }

}


