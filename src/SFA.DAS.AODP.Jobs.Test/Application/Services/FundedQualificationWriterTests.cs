using AutoFixture;
using AutoMapper;
using Microsoft.AspNetCore.Components;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SFA.DAS.AODP.Common.Enum;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Infrastructure.Context;
using SFA.DAS.AODP.Infrastructure.Services;
using SFA.DAS.AODP.Models.Qualification;

namespace SFA.DAS.AODP.Jobs.Test.Application.Services
{
    public class FundedQualificationWriterTests
    {
        private readonly Mock<ILogger<FundedQualificationWriter>> _loggerMock;
        private readonly FunctionContext _functionContext;
        private ApplicationDbContext _dbContext;
        private Fixture _fixture;
        private readonly IMapper _mapper;
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

        public FundedQualificationWriterTests()
        {
            _loggerMock = new Mock<ILogger<FundedQualificationWriter>>();
            _functionContext = new Mock<FunctionContext>().Object;
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase("ApplicationDbContext" + Guid.NewGuid()).Options;
            _dbContext = new ApplicationDbContext(options);
            _fixture = new Fixture();
            _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
                    .ForEach(b => _fixture.Behaviors.Remove(b));
            _fixture.Behaviors.Add(new OmitOnRecursionBehavior(1));

            _fixture.Customizations.Add(
                new ElementsBuilder<FundedQualificationOfferDTO>(
                    new FundedQualificationOfferDTO() { Id = Guid.NewGuid(), Name = FundingOffer1, FundingAvailable = "True", FundingApprovalStartDate = DateTime.Now.AddYears(-1), FundingApprovalEndDate = DateTime.Now.AddYears(1)},
                    new FundedQualificationOfferDTO() { Id = Guid.NewGuid(), Name = FundingOffer2, FundingAvailable = "True", FundingApprovalStartDate = DateTime.Now.AddYears(-1), FundingApprovalEndDate = DateTime.Now.AddYears(1) },
                    new FundedQualificationOfferDTO() { Id = Guid.NewGuid(), Name = FundingOffer3, FundingAvailable = "True", FundingApprovalStartDate = DateTime.Now.AddYears(-1), FundingApprovalEndDate = DateTime.Now.AddYears(1) },
                    new FundedQualificationOfferDTO() { Id = Guid.NewGuid(), Name = FundingOffer1, FundingAvailable = "True", FundingApprovalStartDate = DateTime.Now.AddYears(-1), FundingApprovalEndDate = DateTime.Now.AddYears(1) }));

            var configuration = new MapperConfiguration(cfg => cfg.AddProfile(new MapperProfile()));
            _mapper = new Mapper(configuration);
        }

        [Fact]
        public async Task FundedQualificationWriter_Initialises()
        {
            //Arrange
            await PopulateDbWithReferenceData();
            var _service = CreateImportServiceWithDb();

            Assert.NotNull(_service);
        }

        [Fact]
        public async Task FundedQualificationWriter_ShouldWriteQualifications()
        {
            //Arrange
            await PopulateDbWithReferenceData();
            var _service = CreateImportServiceWithDb();

            var sets = new List<RecordSet>()
            { 
                new RecordSet() { OrgId = Guid.NewGuid(), OrganisationPrn = 1001, ProcessStatus = ProcessStageNoAction, QualId = Guid.NewGuid(), QualificationName = "QualName1", QualificationNumber = "9001" },
                new RecordSet() { OrgId = Guid.NewGuid(), OrganisationPrn = 1002, ProcessStatus = ProcessStageNoAction, QualId = Guid.NewGuid(), QualificationName = "QualName2", QualificationNumber = "9002" },
                new RecordSet() { OrgId = Guid.NewGuid(), OrganisationPrn = 1003, ProcessStatus = ProcessStageNoAction, QualId = Guid.NewGuid(), QualificationName = "QualName3", QualificationNumber = "9003" },
                new RecordSet() { OrgId = Guid.NewGuid(), OrganisationPrn = 1004, ProcessStatus = ProcessStageNoAction, QualId = Guid.NewGuid(), QualificationName = "QualName4", QualificationNumber = "9004" },
            };

            foreach (var set in sets)
            {
                this.CreateQualificationRecordSet(set);
            }

            List<FundedQualificationDTO> importedData = CreateImportedData(sets);

            //Act
            var result = await _service.WriteQualifications(importedData);

            //Assert
            Assert.True(result);
            var insertedOffers = await _dbContext.QualificationOffers.ToListAsync();
            Assert.Equal(4, insertedOffers.Count);    
            var qualId1 = insertedOffers[0].Qualification.QualificationId;            
            var matchingQual = await _dbContext.Qualification.Where(w => w.Id == qualId1).FirstOrDefaultAsync();
            Assert.NotNull(matchingQual);
        }

        [Fact]
        public async Task FundedQualificationWriter_ShouldSeedFundings_WhenOnlyNewOffers()
        {
            //Arrange
            await PopulateDbWithReferenceData();
            var _service = CreateImportServiceWithDb();

            var sets = new List<RecordSet>()
            {
                new RecordSet() { OrgId = Guid.NewGuid(), OrganisationPrn = 1001, ProcessStatus = ProcessStageNoAction, QualId = Guid.NewGuid(), QualificationName = "QualName1", QualificationNumber = "9001", VersionId = Guid.NewGuid() },
                new RecordSet() { OrgId = Guid.NewGuid(), OrganisationPrn = 1002, ProcessStatus = ProcessStageNoAction, QualId = Guid.NewGuid(), QualificationName = "QualName2", QualificationNumber = "9002", VersionId = Guid.NewGuid() },
                new RecordSet() { OrgId = Guid.NewGuid(), OrganisationPrn = 1003, ProcessStatus = ProcessStageNoAction, QualId = Guid.NewGuid(), QualificationName = "QualName3", QualificationNumber = "9003", VersionId = Guid.NewGuid() },
                new RecordSet() { OrgId = Guid.NewGuid(), OrganisationPrn = 1004, ProcessStatus = ProcessStageNoAction, QualId = Guid.NewGuid(), QualificationName = "QualName4", QualificationNumber = "9004", VersionId = Guid.NewGuid() },
            };

            foreach (var set in sets)
            {
                this.CreateQualificationRecordSet(set);
            }

            List<FundedQualificationDTO> importedData = CreateImportedData(sets);

            //Act
            var writeResult = await _service.WriteQualifications(importedData);

            //Assert
            Assert.True(writeResult);
            var insertedOffers = await _dbContext.QualificationOffers                                    
                                    .ToListAsync();
            Assert.Equal(4, insertedOffers.Count);

            var seedResult = await _service.SeedFundingData();
            Assert.True(seedResult);
            var insertedFundings = await _dbContext.QualificationFundings
                                        .Include(i => i.QualificationVersion)
                                            .ThenInclude(t => t.Qualification)
                                        .Include(i => i.FundingOffer)
                                        .ToListAsync();
            Assert.Equal(4, insertedFundings.Count);
            var matchingFunding = insertedFundings.Where(w => w.QualificationVersion.QualificationId == insertedOffers[0].Qualification.QualificationId).FirstOrDefault();
            Assert.NotNull(matchingFunding);
            Assert.Equal(insertedOffers[0].Name, matchingFunding.FundingOffer.Name);
            Assert.Equal(DateOnly.FromDateTime(insertedOffers[0].FundingApprovalStartDate.Value), matchingFunding.StartDate);
            Assert.Equal(DateOnly.FromDateTime(insertedOffers[0].FundingApprovalEndDate.Value), matchingFunding.EndDate);
        }

        [Fact]
        public async Task FundedQualificationWriter_ShouldNotSeedFundings_WhenNoFundingIsAvailable()
        {
            //Arrange
            await PopulateDbWithReferenceData();
            var _service = CreateImportServiceWithDb();

            var sets = new List<RecordSet>()
            {
                new RecordSet() { OrgId = Guid.NewGuid(), OrganisationPrn = 1001, ProcessStatus = ProcessStageNoAction, QualId = Guid.NewGuid(), QualificationName = "QualName1", QualificationNumber = "9001", VersionId = Guid.NewGuid() },
                new RecordSet() { OrgId = Guid.NewGuid(), OrganisationPrn = 1002, ProcessStatus = ProcessStageNoAction, QualId = Guid.NewGuid(), QualificationName = "QualName2", QualificationNumber = "9002", VersionId = Guid.NewGuid() },
                new RecordSet() { OrgId = Guid.NewGuid(), OrganisationPrn = 1003, ProcessStatus = ProcessStageNoAction, QualId = Guid.NewGuid(), QualificationName = "QualName3", QualificationNumber = "9003", VersionId = Guid.NewGuid() },
                new RecordSet() { OrgId = Guid.NewGuid(), OrganisationPrn = 1004, ProcessStatus = ProcessStageNoAction, QualId = Guid.NewGuid(), QualificationName = "QualName4", QualificationNumber = "9004", VersionId = Guid.NewGuid() },
            };

            foreach (var set in sets)
            {
                this.CreateQualificationRecordSet(set);
            }

            List<FundedQualificationDTO> importedData = CreateImportedData(sets, fundingAvailable: false);

            //Act
            var writeResult = await _service.WriteQualifications(importedData);

            //Assert
            Assert.True(writeResult);
            var insertedOffers = await _dbContext.QualificationOffers
                                    .ToListAsync();
            Assert.Equal(4, insertedOffers.Count);

            var seedResult = await _service.SeedFundingData();
            Assert.True(seedResult);
            var insertedFundings = await _dbContext.QualificationFundings
                                        .Include(i => i.QualificationVersion)
                                            .ThenInclude(t => t.Qualification)
                                        .Include(i => i.FundingOffer)
                                        .ToListAsync();
            Assert.Empty(insertedFundings);            
        }

        [Fact]
        public async Task FundedQualificationWriter_ShouldUpdateFundings_WhenExistingOffers()
        {
            //Arrange
            await PopulateDbWithReferenceData();
            var _service = CreateImportServiceWithDb();

            var sets = new List<RecordSet>()
            {
                new RecordSet() { OrgId = Guid.NewGuid(), OrganisationPrn = 1001, ProcessStatus = ProcessStageNoAction, QualId = Guid.NewGuid(), QualificationName = "QualName1", QualificationNumber = "9001", VersionId = Guid.NewGuid() },
                new RecordSet() { OrgId = Guid.NewGuid(), OrganisationPrn = 1002, ProcessStatus = ProcessStageNoAction, QualId = Guid.NewGuid(), QualificationName = "QualName2", QualificationNumber = "9002", VersionId = Guid.NewGuid() },
                new RecordSet() { OrgId = Guid.NewGuid(), OrganisationPrn = 1003, ProcessStatus = ProcessStageNoAction, QualId = Guid.NewGuid(), QualificationName = "QualName3", QualificationNumber = "9003", VersionId = Guid.NewGuid() },
                new RecordSet() { OrgId = Guid.NewGuid(), OrganisationPrn = 1004, ProcessStatus = ProcessStageNoAction, QualId = Guid.NewGuid(), QualificationName = "QualName4", QualificationNumber = "9004", VersionId = Guid.NewGuid() },
            };

            foreach (var set in sets)
            {
                this.CreateQualificationRecordSet(set);
            }

            List<FundedQualificationDTO> importedData = CreateImportedData(sets);

            await CreateFundingOffers(sets);

            //Act
            var writeResult = await _service.WriteQualifications(importedData);
            
            Assert.True(writeResult);
            var insertedOffers = await _dbContext.QualificationOffers
                                    .ToListAsync();
            Assert.Equal(sets.Count, insertedOffers.Count);

            var seedResult = await _service.SeedFundingData();

            //Assert
            Assert.True(seedResult);
            var insertedFundings = await _dbContext.QualificationFundings
                                        .Include(i => i.QualificationVersion)
                                            .ThenInclude(t => t.Qualification)
                                        .Include(i => i.FundingOffer)
                                        .ToListAsync();
            Assert.Equal(6, insertedFundings.Count);
            var matchingFunding = insertedFundings.Where(w => w.QualificationVersion.QualificationId == insertedOffers[0].Qualification.QualificationId
                                                            && w.FundingOffer.Name == insertedOffers[0].Name).FirstOrDefault();
            Assert.NotNull(matchingFunding);
            Assert.Equal(insertedOffers[0].Name, matchingFunding.FundingOffer.Name);
            Assert.Equal(DateOnly.FromDateTime(insertedOffers[0].FundingApprovalStartDate.Value), matchingFunding.StartDate);
            Assert.Equal(DateOnly.FromDateTime(insertedOffers[0].FundingApprovalEndDate.Value), matchingFunding.EndDate);
        }

        //[Fact]
        //public async Task FundedQualificationWriter_ShouldNotUpdateFundings_WhenApproved()
        //{
        //    //Arrange
        //    await PopulateDbWithReferenceData();
        //    var _service = CreateImportServiceWithDb();

        //    var sets = new List<RecordSet>()
        //    {
        //        new RecordSet() { OrgId = Guid.NewGuid(), OrganisationPrn = 1001, ProcessStatus = ProcessStageNoAction, QualId = Guid.NewGuid(), QualificationName = "QualName1", QualificationNumber = "9001", VersionId = Guid.NewGuid() },
        //        new RecordSet() { OrgId = Guid.NewGuid(), OrganisationPrn = 1002, ProcessStatus = ProcessStageNoAction, QualId = Guid.NewGuid(), QualificationName = "QualName2", QualificationNumber = "9002", VersionId = Guid.NewGuid() },
        //        new RecordSet() { OrgId = Guid.NewGuid(), OrganisationPrn = 1003, ProcessStatus = ProcessStageNoAction, QualId = Guid.NewGuid(), QualificationName = "QualName3", QualificationNumber = "9003", VersionId = Guid.NewGuid() },
        //        new RecordSet() { OrgId = Guid.NewGuid(), OrganisationPrn = 1004, ProcessStatus = ProcessStageNoAction, QualId = Guid.NewGuid(), QualificationName = "QualName4", QualificationNumber = "9004", VersionId = Guid.NewGuid() },
        //    };

        //    foreach (var set in sets)
        //    {
        //        this.CreateQualificationRecordSet(set);
        //    }

        //    List<FundedQualificationDTO> importedData = CreateImportedData(sets);

        //    await CreateFundingOffers(sets);

        //    //Act
        //    var writeResult = await _service.WriteQualifications(importedData);

        //    Assert.True(writeResult);
        //    var insertedOffers = await _dbContext.QualificationOffers
        //                            .ToListAsync();

        //    await ApproveFundingOffers(sets);

        //    var seedResult = await _service.SeedFundingData();

        //    //Assert
        //    Assert.True(seedResult);
        //    var currentFundings = await _dbContext.QualificationFundings
        //                                .Include(i => i.QualificationVersion)
        //                                    .ThenInclude(t => t.Qualification)
        //                                .Include(i => i.FundingOffer)
        //                                .ToListAsync();
        //    Assert.Equal(4, currentFundings.Count);
        //    var matchingFunding = currentFundings.Where(w => w.QualificationVersion.QualificationId == insertedOffers[0].Qualification.QualificationId).FirstOrDefault();
        //    Assert.NotNull(matchingFunding);            
        //    Assert.NotEqual(DateOnly.FromDateTime(insertedOffers[0].FundingApprovalStartDate.Value), matchingFunding.StartDate);
        //    Assert.NotEqual(DateOnly.FromDateTime(insertedOffers[0].FundingApprovalEndDate.Value), matchingFunding.EndDate);
        //}

        private List<FundedQualificationDTO> CreateImportedData(List<RecordSet> sets, bool fundingAvailable = true)
        {
            var importedData = new List<FundedQualificationDTO>();
            foreach (var set in sets)
            {
                var offer = _fixture.Create<FundedQualificationOfferDTO>();
                offer.QualificationId = set.QualId;
                offer.FundingAvailable = fundingAvailable.ToString();
                var importRecord = _fixture.Build<FundedQualificationDTO>()
                                    .With(w => w.AwardingOrganisationId, set.OrgId)
                                    .With(w => w.Id, Guid.NewGuid())
                                    .With(w => w.QualificationId, set.QualId)
                                    .With(w => w.Offers, new List<FundedQualificationOfferDTO>() { offer })
                                    .Create();
                importedData.Add(importRecord);
            }

            return importedData;
        }

        private async Task CreateFundingOffers(List<RecordSet> sets)
        {
            var existingFundingRecords = new List<QualificationFunding>();
            foreach (var set in sets)
            {
                // Add existing Funding offers
                var newRecord = new QualificationFunding()
                {
                    Id = Guid.NewGuid(),
                    QualificationVersionId = set.VersionId,
                    FundingOfferId = FundingOfferId1,
                    StartDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-60)),
                    EndDate = DateOnly.FromDateTime(DateTime.Now.AddDays(30)),
                    Comments = $"Imported from Funded CSV on {DateTime.Now.ToShortDateString()}"
                };
                existingFundingRecords.Add(newRecord);
            }
            await _dbContext.AddRangeAsync(existingFundingRecords);
            await _dbContext.SaveChangesAsync();
        }

        private async Task ApproveFundingOffers(List<RecordSet> sets)
        {
            var approvals = new List<QualificationFundingFeedback>();
            foreach (var set in sets)
            {               
                var newRecord = new QualificationFundingFeedback()
                {
                    Id = Guid.NewGuid(),
                    QualificationVersionId = set.VersionId,
                    Approved = true,                      
                    Comments = $"Approved on {DateTime.Now.ToShortDateString()}"
                };
                approvals.Add(newRecord);
            }
            await _dbContext.AddRangeAsync(approvals);
            await _dbContext.SaveChangesAsync();
        }


        private FundedQualificationWriter CreateImportServiceWithDb()
        {
            return new FundedQualificationWriter(
                _loggerMock.Object,
                _dbContext,
                _mapper
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

        private async Task CreateQualificationRecordSet(RecordSet record)
        {            
            var versionId = Guid.NewGuid();
            var qan1_organisation = _fixture.Build<AwardingOrganisation>()
                .Without(w => w.Qualifications)
                .Without(w => w.QualificationVersions)
                .With(w => w.Ukprn, record.OrganisationPrn)
                .With(w => w.Id, record.OrgId)
                .Create();

            var qan1_qualification = _fixture.Build<Qualification>()
                .Without(w => w.Qualifications)
                .Without(w => w.QualificationDiscussionHistories)
                .Without(w => w.QualificationVersions)
                .With(w => w.QualificationName, record.QualificationName)
                .With(w => w.Qan, record.QualificationNumber)
                .With(w => w.Id, record.QualId)
                .Create();

            var qan1_qualificationVersionFieldChange1 = _fixture.Build<VersionFieldChanges>()
                .Without(w => w.QualificationVersions)
                .With(w => w.QualificationVersionNumber, 1)
                .With(w => w.ChangedFieldNames, "Glh, Status")       
                .With(w => w.Id, versionId)
                .Create();

            var qan1_qualificationVersion1 = _fixture.Build<QualificationVersions>()
                .Without(w => w.Qualification)
                .Without(w => w.Organisation)
                .Without(w => w.LifecycleStage)
                .Without(w => w.ProcessStatus)                
                .With(w => w.Version, 1)
                .With(w => w.QualificationId, qan1_qualification.Id)
                .With(w => w.AwardingOrganisationId, qan1_organisation.Id)
                .With(w => w.OfferedInEngland, true)
                .With(w => w.Glh, 5)
                .With(w => w.Tqt, 10)
                .With(w => w.OperationalStartDate, QualificationReference.MinOperationalDate)
                .With(w => w.LifecycleStageId, LifeCycleStageNew)
                .With(w => w.ProcessStatusId, record.ProcessStatus)
                .With(w => w.QualificationId, record.QualId)
                .With(w => w.VersionFieldChangesId, versionId)
                .With(w => w.Id, record.VersionId)
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

        private async Task PopulateDbWithReferenceData()
        {
            var actionType1 = new ActionType() { Description = "No Action Required", Id = ActionTypeNoAction };
            var actionType2 = new ActionType() { Description = "Action Required", Id = ActionTypeDecision };
            var actionType3 = new ActionType() { Description = "Ignore", Id = Guid.NewGuid() };
            await _dbContext.AddRangeAsync(new List<ActionType>() { actionType1, actionType2, actionType3 });
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

        private async Task PopulateDbWithOrganisations(Dictionary<string, Guid> organisations)
        {
            var orgs = new List<AwardingOrganisation>();

            foreach (var kv in organisations)
            {
                var org = _fixture.Build<AwardingOrganisation>()
                    .With(a => a.NameOfqual, kv.Key)
                    .With(a => a.Id, kv.Value)
                    .Create();
                orgs.Add(org);
            }
            await _dbContext.AddRangeAsync(orgs);
            await _dbContext.SaveChangesAsync();
        }

    }

    public class RecordSet
    {
        public Guid OrgId;
        public int OrganisationPrn;
        public Guid QualId;
        public string QualificationNumber;
        public string QualificationName;
        public Guid ProcessStatus;
        public Guid VersionId;
    }
}


