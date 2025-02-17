﻿namespace SFA.DAS.AODP.Models.Qualification;

public class QualificationsQueryParameters
{
    public int Page { get; set; }
    public int Limit { get; set; }
    public string? Title { get; set; }
    public string? AssessmentMethods { get; set; }
    public string? GradingTypes { get; set; }
    public string? AwardingOrganisations { get; set; }
    public string? Availability { get; set; }
    public string? QualificationTypes { get; set; }
    public string? QualificationLevels { get; set; }
    public string? NationalAvailability { get; set; }
    public string? SectorSubjectAreas { get; set; }
    public int? MinTotalQualificationTime { get; set; }
    public int? MaxTotalQualificationTime { get; set; }
    public int? MinGuidedLearningHours { get; set; }
    public int? MaxGuidedLearningHours { get; set; }
}

