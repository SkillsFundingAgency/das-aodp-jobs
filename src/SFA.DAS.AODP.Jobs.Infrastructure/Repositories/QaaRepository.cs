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
        DateOnly snapshotTakenAt,
        CancellationToken cancellationToken)
    {
        await using var context = await _applicationDbContext.CreateDbContextAsync(cancellationToken);

        var currentQualifications = await ReadCurrentQualificationsByAimCodeAsync(context, cancellationToken);
        var nextChangeVersion = WorkOutNextChangeVersion(currentQualifications);

        foreach (var proposedQaaQualification in proposedQualifications.Select(ReadQaaQualification))
        {
            nextChangeVersion = await AddOrRefreshCurrentQaaQualificationAsync(
                context,
                currentQualifications,
                proposedQaaQualification,
                snapshotTakenAt,
                nextChangeVersion,
                cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);

        return proposedQualifications.Count;
    }

    private static async Task<Dictionary<string, RegulatedQaaQualification>> ReadCurrentQualificationsByAimCodeAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        return await context.RegulatedQaaQualification
            .ToDictionaryAsync(qualification => qualification.AimCode, cancellationToken);
    }

    private static long WorkOutNextChangeVersion(
        IReadOnlyDictionary<string, RegulatedQaaQualification> currentQualifications)
    {
        return currentQualifications.Count == 0
            ? 1
            : currentQualifications.Values.Max(qualification => qualification.ChangeVersion) + 1;
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

    private static async Task<long> AddOrRefreshCurrentQaaQualificationAsync(
        ApplicationDbContext context,
        IDictionary<string, RegulatedQaaQualification> currentQualifications,
        ProposedQaaQualification proposedQaaQualification,
        DateOnly snapshotTakenAt,
        long nextChangeVersion,
        CancellationToken cancellationToken)
    {
        if (!currentQualifications.TryGetValue(proposedQaaQualification.AimCode, out var currentQualification))
        {
            var newQualification = CreateCurrentQaaQualification(
                proposedQaaQualification,
                snapshotTakenAt,
                nextChangeVersion);

            await context.RegulatedQaaQualification.AddAsync(newQualification, cancellationToken);
            await context.RegulatedQaaQualificationVersion.AddAsync(
                RegulatedQaaQualificationVersion.Create(
                    newQualification,
                    QaaLastDateForRegistrationChangeType.NotChanged),
                cancellationToken);
            currentQualifications.Add(proposedQaaQualification.AimCode, newQualification);

            return nextChangeVersion + 1;
        }

        return RefreshCurrentQaaQualification(
            context,
            currentQualification,
            proposedQaaQualification,
            snapshotTakenAt,
            nextChangeVersion);
    }

    private static RegulatedQaaQualification CreateCurrentQaaQualification(
        ProposedQaaQualification proposedQaaQualification,
        DateOnly snapshotTakenAt,
        long changeVersion)
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
            changeVersion);
    }

    private static long RefreshCurrentQaaQualification(
        ApplicationDbContext context,
        RegulatedQaaQualification currentQualification,
        ProposedQaaQualification proposedQaaQualification,
        DateOnly snapshotTakenAt,
        long nextChangeVersion)
    {
        var hasMaterialChange = currentQualification.HasMaterialQaaChange(
            proposedQaaQualification.RegistrationClosesOn,
            proposedQaaQualification.HasBeenDiscontinuedByQaa);

        currentQualification.ApplyImportedQaaData(
            snapshotTakenAt,
            proposedQaaQualification.Title,
            proposedQaaQualification.AwardingBodyName,
            proposedQaaQualification.RegistrationOpenedOn,
            proposedQaaQualification.RegistrationClosesOn,
            proposedQaaQualification.DiscontinuedDate,
            proposedQaaQualification.SectorSubjectArea,
            hasMaterialChange ? nextChangeVersion : null);

        if (hasMaterialChange)
        {
            context.RegulatedQaaQualificationVersion.Add(
                RegulatedQaaQualificationVersion.Create(
                    currentQualification,
                    currentQualification.LastDateForRegistrationChangeType));
        }

        return hasMaterialChange
            ? nextChangeVersion + 1
            : nextChangeVersion;
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
