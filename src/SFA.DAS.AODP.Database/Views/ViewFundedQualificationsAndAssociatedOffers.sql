CREATE VIEW [dbo].[ViewFundedQualificationsAndAssociatedOffers]
AS
SELECT   dbo.FundedQualifications.DateOfOfqualDataSnapshot, dbo.FundedQualifications.QualificationNumber, dbo.FundedQualifications.QualificationName, 
                         dbo.FundedQualifications.AwardingOrganisation, dbo.FundedQualifications.[Level], dbo.FundedQualifications.QualificationType, dbo.FundedQualifications.Subcategory, 
                         dbo.FundedQualifications.SectorSubjectArea, dbo.FundedQualifications.Status, dbo.FundedQualifications.AwardingOrganisationURL, dbo.FundedQualifications.ImportDate, 
                         dbo.FundedQualificationOffers.Name, dbo.FundedQualificationOffers.FundingAvailable, dbo.FundedQualificationOffers.FundingApprovalStartDate, 
                         dbo.FundedQualificationOffers.FundingApprovalEndDate, dbo.FundedQualificationOffers.Notes
FROM         dbo.FundedQualifications INNER JOIN
                         dbo.FundedQualificationOffers ON dbo.FundedQualifications.Id = dbo.FundedQualificationOffers.FundedQualificationId


