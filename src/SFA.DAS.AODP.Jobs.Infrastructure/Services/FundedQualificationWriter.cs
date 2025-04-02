using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SFA.DAS.AODP.Common.Enum;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Infrastructure.Context;
using SFA.DAS.AODP.Infrastructure.Interfaces;
using SFA.DAS.AODP.Models.Qualification;

namespace SFA.DAS.AODP.Infrastructure.Services
{
    public class FundedQualificationWriter : IFundedQualificationWriter
    {
        private readonly ILogger<FundedQualificationWriter> _logger;
        private readonly IMapper _mapper;
        private readonly IApplicationDbContext _applicationDbContext;

        public FundedQualificationWriter(ILogger<FundedQualificationWriter> logger, IApplicationDbContext applicationDbContext, IMapper mapper)
        {
            _logger = logger;
            _mapper = mapper;
            _applicationDbContext = applicationDbContext;
        }

        public async Task<bool> WriteQualifications(List<FundedQualificationDTO> qualifications)
        {
            var success = true;

            try
            {
                const int _batchSize = 1000;
                _logger.LogInformation("Writing funded qualifications to db");
                for (int i = 0; i < qualifications.Count; i += _batchSize)
                {
                    var batch = qualifications
                        .Skip(i)
                        .Take(_batchSize)
                        .ToList();

                    var entities = _mapper.Map<List<Qualifications>>(batch);

                    await _applicationDbContext.FundedQualifications.AddRangeAsync(entities);
                }

                await _applicationDbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while trying to save batch to db: {ex.Message}");
                success = false;
            }

            return success;
        }

        public async Task<bool> SeedFundingData()
        {
            var success = true;

            try
            {
                var offerTypeLookup = await _applicationDbContext.FundingOffers.ToDictionaryAsync(r => r.Name, r => r.Id);
                var actionLookup = await _applicationDbContext.ActionType.ToDictionaryAsync(r => r.Description ?? "", r => r.Id);
                var noActionNeededId = actionLookup[ActionTypeEnum.NoActionRequired];

                // Funding offers from imported data
                var importedOffers = await _applicationDbContext.FundedQualifications
                                        .Include(i => i.QualificationOffers)
                                        .Where(w => w.QualificationId.HasValue
                                                && w.AwardingOrganisationId.HasValue
                                                && w.QualificationOffers.Any(a => a.FundingAvailable ?? false))
                                        .ToListAsync();
                var importedOfferIds = importedOffers.Select(s => s.QualificationId.Value).ToList();

                // Funding offers created by users
                var userCreatedOffers = await _applicationDbContext.QualificationFundings
                                        .Include(i => i.FundingOffer)
                                        .Include(i => i.QualificationVersion)
                                        .ThenInclude(t => t.Qualification)
                                        .ToListAsync();
                var userCreatedOfferIds = userCreatedOffers.Select(s => s.QualificationVersion.QualificationId).ToList();

                // Funding offers not to be updated as they have been approved
                var noTouchyList = await _applicationDbContext.QualificationFundingFeedbacks
                                        .Include(i => i.QualificationVersion)
                                        .Where(w => w.Approved ?? false)
                                        .Select(s => s.QualificationVersion.QualificationId)
                                        .ToListAsync();

                // These are quals that have imported offers, but no user created offers
                var qualsMissingFunding = importedOfferIds.Except(userCreatedOfferIds).Except(noTouchyList).ToList();
                _logger.LogInformation($"SeedFundingData -> Found {qualsMissingFunding.Count} quals missing offers");

                // These are quals that have imported offers and user created offers
                var qualsNeedUpdating = importedOfferIds.Except(qualsMissingFunding).Except(noTouchyList).ToList();
                _logger.LogInformation($"SeedFundingData -> Found {qualsNeedUpdating.Count} that might need updating");

                var importRun = DateTime.Now;
                if (qualsMissingFunding.Any())
                {
                    _logger.LogInformation($"SeedFundingData -> Adding missing offers to funded.QualificationFundings");
                    foreach (var id in qualsMissingFunding)
                    {
                        // Exists in FundedQualifications, Create new QualificationFunding record
                        var latestVersion = await _applicationDbContext.QualificationVersions
                                                    .Include(i => i.Qualification)
                                                    .ThenInclude(t => t.QualificationDiscussionHistories)
                                                    .Where(w => w.QualificationId == id)
                                                    .OrderByDescending(o => o.Version)
                                                    .FirstOrDefaultAsync();

                        if (latestVersion == null)
                        {
                            _logger.LogError($"SeedFundingData -> Unable to process Qual Id {id} as it has no qualification versions");
                            return false;
                        }

                        var fundedQualToBeAdded = importedOffers
                                                    .Where(w => w.QualificationId == id && w.QualificationOffers.Any(a => a.FundingAvailable ?? false))
                                                    .FirstOrDefault();

                        if (fundedQualToBeAdded != null)
                        {
                            var offers = fundedQualToBeAdded.QualificationOffers.Where(w => w.FundingAvailable ?? false).ToList();
                            var newRecords = new List<QualificationFunding>();
                            int added = 0;
                            foreach (var offer in offers)
                            {
                                if (offerTypeLookup.ContainsKey(offer.Name))
                                {
                                    var offerTypeId = offerTypeLookup[offer.Name];
                                    var startDate = new DateOnly();
                                    if (offer.FundingApprovalStartDate.HasValue)
                                    {
                                        startDate = new DateOnly(offer.FundingApprovalStartDate.Value.Year, offer.FundingApprovalStartDate.Value.Month, offer.FundingApprovalStartDate.Value.Day);
                                    }
                                    var endDate = new DateOnly();
                                    if (offer.FundingApprovalEndDate.HasValue)
                                    {
                                        endDate = new DateOnly(offer.FundingApprovalEndDate.Value.Year, offer.FundingApprovalEndDate.Value.Month, offer.FundingApprovalEndDate.Value.Day);
                                    }

                                    var newRecord = new QualificationFunding()
                                    {
                                        Id = Guid.NewGuid(),
                                        QualificationVersionId = latestVersion.Id,
                                        FundingOfferId = offerTypeId,
                                        StartDate = startDate,
                                        EndDate = endDate,
                                        Comments = $"Imported from Funded CSV on {importRun.ToShortDateString()}"
                                    };
                                    newRecords.Add(newRecord);
                                    added++;
                                }
                            }
                            _logger.LogInformation($"Found {added} missing offers for {latestVersion.Name}");
                            var previousActionTypeId = latestVersion.Qualification.QualificationDiscussionHistories
                                                        .OrderByDescending(o => o.Timestamp)
                                                        .FirstOrDefault()?.ActionTypeId ?? noActionNeededId;

                            await _applicationDbContext.QualificationDiscussionHistory.AddAsync(new QualificationDiscussionHistory()
                            {
                                Id = Guid.NewGuid(),
                                QualificationId = id,
                                Timestamp = importRun,
                                UserDisplayName = "FundedImport",
                                Notes = $"Funded Import inserted {added} new offers",
                                ActionTypeId = previousActionTypeId
                            });
                            await _applicationDbContext.QualificationFundings.AddRangeAsync(newRecords);
                            await _applicationDbContext.SaveChangesAsync();
                        }
                        else
                        {
                            _logger.LogInformation($"SeedFundingData -> Qual Id {id} has no offers with funding available");
                        }
                    }
                }

                if (qualsNeedUpdating.Any())
                {
                    foreach (var id in qualsNeedUpdating)
                    {
                        
                        var latestVersion = await _applicationDbContext.QualificationVersions
                                                    .Include(i => i.Qualification)
                                                    .Where(w => w.QualificationId == id)
                                                    .OrderByDescending(o => o.Version)
                                                    .FirstAsync();
                        if (latestVersion == null)
                        {
                            _logger.LogError($"SeedFundingData -> Unable to process Qual Id {id} as it has no qualification versions");
                            return false;
                        }

                        var importedOfferToBeUpdated = importedOffers
                                                    .Where(w => w.QualificationId == id && w.QualificationOffers.Any(a => a.FundingAvailable ?? false))
                                                    .FirstOrDefault();
                        if (importedOfferToBeUpdated != null)
                        {
                            var newRecords = new List<QualificationFunding>();
                            var existingUserOffers = userCreatedOffers.Where(w => w.QualificationVersion.QualificationId == id).ToList();
                            var offers = importedOfferToBeUpdated.QualificationOffers.Where(w => w.FundingAvailable ?? false).ToList();
                            int added = 0;
                            int updated = 0;
                            foreach (var offer in offers)
                            {
                                if (offerTypeLookup.ContainsKey(offer.Name))
                                {
                                    var offerTypeId = offerTypeLookup[offer.Name];
                                    var startDate = new DateOnly();
                                    if (offer.FundingApprovalStartDate.HasValue)
                                    {
                                        startDate = new DateOnly(offer.FundingApprovalStartDate.Value.Year, offer.FundingApprovalStartDate.Value.Month, offer.FundingApprovalStartDate.Value.Day);
                                    }
                                    var endDate = new DateOnly();
                                    if (offer.FundingApprovalEndDate.HasValue)
                                    {
                                        endDate = new DateOnly(offer.FundingApprovalEndDate.Value.Year, offer.FundingApprovalEndDate.Value.Month, offer.FundingApprovalEndDate.Value.Day);
                                    }

                                    var matchingUserOffer = existingUserOffers.Where(w => w.FundingOffer.Name == offer.Name).FirstOrDefault();
                                    if (matchingUserOffer != null)
                                    {
                                        // update offer
                                        if (matchingUserOffer.StartDate.HasValue && matchingUserOffer.StartDate.Value != startDate
                                            || matchingUserOffer.EndDate.HasValue && matchingUserOffer.EndDate.Value != endDate)
                                        {
                                            matchingUserOffer.StartDate = startDate;
                                            matchingUserOffer.EndDate = endDate;
                                            matchingUserOffer.Comments = $"{matchingUserOffer.Comments}, updated by import on {importRun.ToShortDateString()}";
                                            updated++;
                                        }
                                    }
                                    else
                                    {
                                        // create missing offer
                                        var newRecord = new QualificationFunding()
                                        {
                                            Id = Guid.NewGuid(),
                                            QualificationVersionId = latestVersion.Id,
                                            FundingOfferId = offerTypeId,
                                            StartDate = startDate,
                                            EndDate = endDate,
                                            Comments = $"Imported from Funded CSV on {importRun.ToShortDateString()}"
                                        };
                                        newRecords.Add(newRecord);
                                        added++;
                                    }
                                }
                            }
                            _logger.LogInformation($"Added {added} and updated {updated} offers for {latestVersion.Name}");
                            var previousActionTypeId = latestVersion.Qualification.QualificationDiscussionHistories
                                                        .OrderByDescending(o => o.Timestamp)
                                                        .FirstOrDefault()?.ActionTypeId ?? noActionNeededId;

                            await _applicationDbContext.QualificationDiscussionHistory.AddAsync(new QualificationDiscussionHistory()
                            {
                                Id = Guid.NewGuid(),
                                QualificationId = id,
                                Timestamp = importRun,
                                UserDisplayName = "FundedImport",
                                Notes = $"Funded Import added {added} and updated {updated} offers",
                                ActionTypeId = previousActionTypeId
                            });
                            await _applicationDbContext.QualificationFundings.AddRangeAsync(newRecords);
                            await _applicationDbContext.SaveChangesAsync();
                        }
                        else
                        {
                            _logger.LogInformation($"SeedFundingData -> Qual Id {id} has no offers with funding available");
                        }
                    }                    
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while trying to save batch to db: {ex.Message}");
                success = false;
            }

            _logger.LogInformation($"Funding Offers seed complete");
            return success;
        }
    }
}

