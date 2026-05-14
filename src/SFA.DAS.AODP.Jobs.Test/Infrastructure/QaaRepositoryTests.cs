using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SFA.DAS.AODP.Infrastructure.Context;

namespace SFA.DAS.AODP.Jobs.UnitTests.Infrastructure;

public class QaaRepositoryTests
{
    private readonly DateTime _snapshotDate = new(2024, 02, 15);

    [Fact]
    public async Task ImportQaaQualificationsAsync_WhenAimCodeExists_UpdatesExistingRowWithoutDeleting()
    {
        var (context, repository) = CreateRepository();
        var existing = CreateExistingQualification("Z1234567", changeVersion: 5);
        context.RegulatedQaaQualification.Add(existing);
        await context.SaveChangesAsync();

        await repository.ImportQaaQualificationsAsync(
            [CreateResponse("Z1234567", title: "Updated title")],
            _snapshotDate,
            CancellationToken.None);

        context.ChangeTracker.Clear();
        var stored = await context.RegulatedQaaQualification.SingleAsync();
        Assert.Equal(existing.Id, stored.Id);
        Assert.Equal("Updated title", stored.QualificationTitle);
        Assert.Equal(5, stored.ChangeVersion);
        Assert.Equal(QaaImportComparisonOutcome.Unchanged, stored.LatestImportComparisonOutcome);
    }

    [Fact]
    public async Task ImportQaaQualificationsAsync_WhenAimCodeIsNew_InsertsQualification()
    {
        var (context, repository) = CreateRepository();

        await repository.ImportQaaQualificationsAsync(
            [CreateResponse("Z1234567")],
            _snapshotDate,
            CancellationToken.None);

        context.ChangeTracker.Clear();
        var stored = await context.RegulatedQaaQualification.SingleAsync();
        Assert.Equal("Z1234567", stored.AimCode);
        Assert.Equal(1, stored.ChangeVersion);
        Assert.Equal(_snapshotDate, stored.LastChangedAt);
        Assert.Equal(QaaImportComparisonOutcome.New, stored.LatestImportComparisonOutcome);
        Assert.Equal(QaaPublicationStatus.PendingNew, stored.PublicationStatus);
        Assert.Equal(QaaLastDateForRegistrationChangeType.NotChanged, stored.LastDateForRegistrationChangeType);

        var history = await context.RegulatedQaaQualificationVersion.SingleAsync();
        Assert.Equal(stored.Id, history.QaaQualificationId);
        Assert.Equal(stored.ChangeVersion, history.ChangeVersion);
    }

    [Fact]
    public async Task ImportQaaQualificationsAsync_WhenMaterialFieldsAreUnchanged_DoesNotBumpChangeVersion()
    {
        var (context, repository) = CreateRepository();
        var existing = CreateExistingQualification("Z1234567", changeVersion: 5);
        context.RegulatedQaaQualification.Add(existing);
        await context.SaveChangesAsync();
        var originalContentHash = existing.ContentHash;

        await repository.ImportQaaQualificationsAsync(
            [CreateResponse("Z1234567", title: "Updated title")],
            _snapshotDate,
            CancellationToken.None);

        context.ChangeTracker.Clear();
        var stored = await context.RegulatedQaaQualification.SingleAsync();
        Assert.Equal(5, stored.ChangeVersion);
        Assert.Equal(originalContentHash, stored.ContentHash);
        Assert.Equal(QaaImportComparisonOutcome.Unchanged, stored.LatestImportComparisonOutcome);
        Assert.Equal(QaaPublicationStatus.PendingNew, stored.PublicationStatus);
        Assert.Empty(await context.RegulatedQaaQualificationVersion.ToListAsync());
    }

    [Fact]
    public async Task ImportQaaQualificationsAsync_WhenLastDateForRegistrationChanges_BumpsChangeVersion()
    {
        var (context, repository) = CreateRepository();
        context.RegulatedQaaQualification.Add(CreateExistingQualification("Z1234567", changeVersion: 5));
        await context.SaveChangesAsync();

        await repository.ImportQaaQualificationsAsync(
            [CreateResponse("Z1234567", lastDateForRegistration: new DateOnly(2026, 08, 31))],
            _snapshotDate,
            CancellationToken.None);

        context.ChangeTracker.Clear();
        var stored = await context.RegulatedQaaQualification.SingleAsync();
        Assert.Equal(6, stored.ChangeVersion);
        Assert.Equal(new DateOnly(2026, 08, 31), stored.LastDateForRegistration);
        Assert.Equal(QaaImportComparisonOutcome.MaterialChanged, stored.LatestImportComparisonOutcome);
        Assert.Equal(QaaPublicationStatus.PendingNew, stored.PublicationStatus);
        Assert.Equal(QaaLastDateForRegistrationChangeType.Extended, stored.LastDateForRegistrationChangeType);
        Assert.True(stored.IsRegistrationDateExtended);
        Assert.False(stored.IsRegistrationDateBroughtForward);
        Assert.Single(await context.RegulatedQaaQualificationVersion.ToListAsync());
    }

    [Fact]
    public async Task ImportQaaQualificationsAsync_WhenDiscontinuedStateChanges_BumpsChangeVersion()
    {
        var (context, repository) = CreateRepository();
        context.RegulatedQaaQualification.Add(CreateExistingQualification("Z1234567", changeVersion: 5));
        await context.SaveChangesAsync();

        await repository.ImportQaaQualificationsAsync(
            [CreateResponse("Z1234567", discontinuedDate: new DateOnly(2024, 01, 31))],
            _snapshotDate,
            CancellationToken.None);

        context.ChangeTracker.Clear();
        var stored = await context.RegulatedQaaQualification.SingleAsync();
        Assert.Equal(6, stored.ChangeVersion);
        Assert.True(stored.IsDiscontinued);
        Assert.Equal(new DateOnly(2024, 01, 31), stored.DiscontinuedDate);
        Assert.Equal(QaaImportComparisonOutcome.MaterialChanged, stored.LatestImportComparisonOutcome);
        Assert.Equal(QaaLastDateForRegistrationChangeType.NotChanged, stored.LastDateForRegistrationChangeType);
    }

    [Fact]
    public async Task ImportQaaQualificationsAsync_WhenExistingRowsAreEmpty_UsesInitialChangeVersion()
    {
        var (context, repository) = CreateRepository();

        await repository.ImportQaaQualificationsAsync(
            [CreateResponse("Z1234567"), CreateResponse("Z1234568")],
            _snapshotDate,
            CancellationToken.None);

        context.ChangeTracker.Clear();
        var stored = await context.RegulatedQaaQualification
            .OrderBy(qualification => qualification.AimCode)
            .ToListAsync();
        Assert.Equal(1, stored[0].ChangeVersion);
        Assert.Equal(2, stored[1].ChangeVersion);
        Assert.Equal(2, await context.RegulatedQaaQualificationVersion.CountAsync());
    }

    [Fact]
    public async Task ImportQaaQualificationsAsync_WhenMaterialChangeAfterPublished_SetsPendingChange()
    {
        var (context, repository) = CreateRepository();
        var existing = CreateExistingQualification("Z1234567", changeVersion: 5);
        existing.MarkAsPublished(new DateTime(2024, 01, 20));
        context.RegulatedQaaQualification.Add(existing);
        await context.SaveChangesAsync();

        await repository.ImportQaaQualificationsAsync(
            [CreateResponse("Z1234567", lastDateForRegistration: new DateOnly(2024, 08, 31))],
            _snapshotDate,
            CancellationToken.None);

        context.ChangeTracker.Clear();
        var stored = await context.RegulatedQaaQualification.SingleAsync();
        Assert.Equal(QaaPublicationStatus.PendingChange, stored.PublicationStatus);
        Assert.Equal(QaaLastDateForRegistrationChangeType.BroughtForward, stored.LastDateForRegistrationChangeType);
        Assert.False(stored.IsRegistrationDateExtended);
        Assert.True(stored.IsRegistrationDateBroughtForward);
        Assert.Single(await context.RegulatedQaaQualificationVersion.ToListAsync());
    }

    [Fact]
    public async Task ImportQaaQualificationsAsync_WhenUnchangedAfterPendingChange_KeepsPendingChange()
    {
        var (context, repository) = CreateRepository();
        var existing = CreateExistingQualification("Z1234567", changeVersion: 5);
        existing.MarkAsPublished(new DateTime(2024, 01, 20));
        existing.ApplyImportedQaaData(
            new DateTime(2024, 01, 21),
            "Access to Higher Education Diploma (Science)",
            "Test Awarding Body",
            new DateOnly(2023, 09, 01),
            new DateOnly(2026, 08, 31),
            null,
            SectorSubjectArea.Science,
            6,
            new DateTime(2024, 01, 21));
        context.RegulatedQaaQualification.Add(existing);
        await context.SaveChangesAsync();

        await repository.ImportQaaQualificationsAsync(
            [CreateResponse("Z1234567", lastDateForRegistration: new DateOnly(2026, 08, 31))],
            _snapshotDate,
            CancellationToken.None);

        context.ChangeTracker.Clear();
        var stored = await context.RegulatedQaaQualification.SingleAsync();
        Assert.Equal(QaaImportComparisonOutcome.Unchanged, stored.LatestImportComparisonOutcome);
        Assert.Equal(QaaPublicationStatus.PendingChange, stored.PublicationStatus);
        Assert.Empty(await context.RegulatedQaaQualificationVersion.ToListAsync());
    }

    private static (ApplicationDbContext Context, QaaRepository Repository) CreateRepository()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        var repository = new QaaRepository(new TestDbContextFactory(options));

        return (context, repository);
    }

    private static RegulatedQaaQualification CreateExistingQualification(
        string aimCode,
        long changeVersion,
        bool isDiscontinued = false)
    {
        return RegulatedQaaQualification.Create(
            new DateTime(2024, 01, 15),
            aimCode,
            "Access to Higher Education Diploma (Science)",
            "Test Awarding Body",
            new DateOnly(2023, 09, 01),
            new DateOnly(2025, 08, 31),
            SectorSubjectArea.Science,
            isDiscontinued ? new DateOnly(2024, 01, 31) : null,
            changeVersion,
            new DateTime(2024, 01, 15));
    }

    private static QaaQualificationResponse CreateResponse(
        string aimCode,
        string title = "Access to Higher Education Diploma (Science)",
        DateOnly? lastDateForRegistration = null,
        DateOnly? discontinuedDate = null)
    {
        return new QaaQualificationResponse
        {
            AimCode = aimCode,
            DiplomaTitle = title,
            AwardingBody = "Test Awarding Body",
            SsaTier1 = "2",
            SsaTier2 = "1",
            StartDateOfQualification = new DateOnly(2023, 09, 01),
            LastDateForRegistrations = lastDateForRegistration ?? new DateOnly(2025, 08, 31),
            DiscontinuedDate = discontinuedDate
        };
    }

    private sealed class TestDbContextFactory(DbContextOptions<ApplicationDbContext> options)
        : IDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext()
        {
            return new ApplicationDbContext(options);
        }
    }
}
