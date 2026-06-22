using Microsoft.EntityFrameworkCore;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Infrastructure.Context;
using SFA.DAS.AODP.Models.QaaQualification;

namespace SFA.DAS.AODP.Infrastructure.Repositories;

/// <summary>
/// Default implementation for <see cref="IQaaRepository"/>.
/// </summary>
/// <param name="dbContextFactory"></param>
public class QaaRepository(IDbContextFactory<ApplicationDbContext> dbContextFactory) : IQaaRepository
{
    private readonly IDbContextFactory<ApplicationDbContext> _applicationDbContext = dbContextFactory;

    /// <inheritdoc/>.
    public async Task<int> ImportQaaQualificationsAsync(
        IReadOnlyCollection<QaaQualificationResponse> proposedQualifications,
        DateTime snapshotTakenAt,
        CancellationToken cancellationToken)
    {
        await using var context = await _applicationDbContext.CreateDbContextAsync(cancellationToken);

        var currentQualifications = await context.RegulatedQaaQualification.ToDictionaryAsync(qualification => qualification.AimCode, cancellationToken);

        foreach (var proposedQaaQualification in proposedQualifications.Select(ReadQaaQualification))
        {
            await CreateOrUpdateQaaQualificationAsync(
                context,
                currentQualifications,
                proposedQaaQualification,
                snapshotTakenAt,
                cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);

        return proposedQualifications.Count;
    }

    private static ProposedQaaQualification ReadQaaQualification(QaaQualificationResponse qualification)
    {
        return new ProposedQaaQualification(
            qualification.AimCode,
            qualification.DiplomaTitle,
            qualification.AwardingBody,
            qualification.StartDateOfQualification,
            qualification.LastDateForRegistrations,
            qualification.DiscontinuedDate,
            SectorSubjectArea.FromTiers(qualification.SsaTier1, qualification.SsaTier2));
    }

    private static async Task CreateOrUpdateQaaQualificationAsync(
        ApplicationDbContext context,
        IDictionary<string, RegulatedQaaQualification> currentQualifications,
        ProposedQaaQualification proposedQaaQualification,
        DateTime snapshotTakenAt,
        CancellationToken cancellationToken)
    {
        if (!currentQualifications.TryGetValue(proposedQaaQualification.AimCode, out var currentQualification))
        {
            var newQualification = CreateQaaQualification(
                proposedQaaQualification,
                snapshotTakenAt);

            var history = RegulatedQaaQualificationHistory.Create(
                newQualification,
                QaaLastDateForRegistrationChangeType.NotChanged);

            newQualification.RecordHistory(history.Id);

            await context.RegulatedQaaQualification.AddAsync(newQualification, cancellationToken);
            await context.RegulatedQaaQualificationHistory.AddAsync(history, cancellationToken);

            currentQualifications.Add(proposedQaaQualification.AimCode, newQualification);

            return;
        }

        UpdateQaaQualification(
            context,
            currentQualification,
            proposedQaaQualification,
            snapshotTakenAt);
    }

    private static RegulatedQaaQualification CreateQaaQualification(
        ProposedQaaQualification proposedQaaQualification,
        DateTime snapshotTakenAt)
    {
        return RegulatedQaaQualification.Create(
            snapshotTakenAt,
            proposedQaaQualification.AimCode,
            proposedQaaQualification.Title,
            proposedQaaQualification.AwardingBodyName,
            proposedQaaQualification.RegistrationOpenedOn,
            proposedQaaQualification.RegistrationClosesOn,
            proposedQaaQualification.SectorSubjectArea,
            proposedQaaQualification.DiscontinuedDate,
            snapshotTakenAt);
    }

    private static void UpdateQaaQualification(
        ApplicationDbContext context,
        RegulatedQaaQualification currentQualification,
        ProposedQaaQualification proposedQaaQualification,
        DateTime snapshotTakenAt)
    {
        EnsureCurrentQualificationHasHistory(context, currentQualification);

        var changed = currentQualification.Update(
            snapshotTakenAt,
            proposedQaaQualification.Title,
            proposedQaaQualification.AwardingBodyName,
            proposedQaaQualification.RegistrationOpenedOn,
            proposedQaaQualification.RegistrationClosesOn,
            proposedQaaQualification.DiscontinuedDate,
            proposedQaaQualification.SectorSubjectArea,
            snapshotTakenAt);

        if (changed)
        {
            var history = RegulatedQaaQualificationHistory.Create(
                currentQualification,
                currentQualification.LastDateForRegistrationChangeType);

            currentQualification.RecordHistory(history.Id);
            context.RegulatedQaaQualificationHistory.Add(history);
        }
    }

    private static void EnsureCurrentQualificationHasHistory(
        ApplicationDbContext context,
        RegulatedQaaQualification currentQualification)
    {
        if (currentQualification.LatestQaaQualificationHistoryId.HasValue)
        {
            return;
        }

        var history = RegulatedQaaQualificationHistory.Create(
            currentQualification,
            currentQualification.LastDateForRegistrationChangeType);
        
        currentQualification.RecordHistory(history.Id);

        context.RegulatedQaaQualificationHistory.Add(history);
    }

    private sealed record ProposedQaaQualification(
        string AimCode,
        string Title,
        string AwardingBodyName,
        DateOnly RegistrationOpenedOn,
        DateOnly RegistrationClosesOn,
        DateOnly? DiscontinuedDate,
        SectorSubjectArea SectorSubjectArea)
    {
        public bool HasBeenDiscontinuedByQaa => DiscontinuedDate.HasValue;
    }
}
