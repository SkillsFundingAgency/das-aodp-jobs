using Microsoft.EntityFrameworkCore;
using SFA.DAS.AODP.Infrastructure.Context;

namespace SFA.DAS.AODP.Jobs.UnitTests.Infrastructure;

public class QaaRepositoryTests
{
    private readonly DateTime _snapshotDate = new(2024, 02, 15);

    [Fact]
    public async Task ImportQaaQualificationsAsync_WhenAimCodeExists_UpdatesExistingRowWithoutDeleting()
    {
        var (context, repository) = CreateRepository();
        var existing = CreateExistingQualification("Z1234567");
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
        Assert.Equal(QaaImportComparisonOutcome.NotChanged, stored.LatestImportComparisonOutcome);
    }

    [Fact]
    public async Task ImportQaaQualificationsAsync_WhenAimCodeIsNew_InsertsQualificationAndHistory()
    {
        var (context, repository) = CreateRepository();

        await repository.ImportQaaQualificationsAsync(
            [CreateResponse("Z1234567")],
            _snapshotDate,
            CancellationToken.None);

        context.ChangeTracker.Clear();
        var stored = await context.RegulatedQaaQualification.SingleAsync();
        var history = await context.RegulatedQaaQualificationHistory.SingleAsync();

        Assert.Equal("Z1234567", stored.AimCode);
        Assert.Equal(_snapshotDate, stored.LastChangedAt);
        Assert.Equal(_snapshotDate, stored.FirstSeenAt);
        Assert.Equal(QaaImportComparisonOutcome.New, stored.LatestImportComparisonOutcome);
        Assert.Equal(QaaLastDateForRegistrationChangeType.NotChanged, stored.LastDateForRegistrationChangeType);
        Assert.Equal(history.Id, stored.LatestQaaQualificationHistoryId);
        Assert.Equal(stored.Id, history.QaaQualificationId);
    }

    [Fact]
    public async Task ImportQaaQualificationsAsync_WhenMaterialFieldsAreUnchanged_DoesNotCreateHistory()
    {
        var (context, repository) = CreateRepository();
        var existingHistoryId = Guid.NewGuid();
        var existing = CreateExistingQualification("Z1234567", latestHistoryId: existingHistoryId);
        context.RegulatedQaaQualification.Add(existing);
        await context.SaveChangesAsync();
        
        await repository.ImportQaaQualificationsAsync(
            [CreateResponse("Z1234567", title: "Updated title")],
            _snapshotDate,
            CancellationToken.None);

        context.ChangeTracker.Clear();
        var stored = await context.RegulatedQaaQualification.SingleAsync();
        Assert.Equal(existingHistoryId, stored.LatestQaaQualificationHistoryId);
        Assert.Equal(QaaImportComparisonOutcome.NotChanged, stored.LatestImportComparisonOutcome);
        Assert.Empty(await context.RegulatedQaaQualificationHistory.ToListAsync());
    }

    [Fact]
    public async Task ImportQaaQualificationsAsync_WhenLastDateForRegistrationChanges_CreatesHistory()
    {
        var (context, repository) = CreateRepository();
        var existingHistoryId = Guid.NewGuid();
        context.RegulatedQaaQualification.Add(CreateExistingQualification("Z1234567", latestHistoryId: existingHistoryId));
        await context.SaveChangesAsync();

        await repository.ImportQaaQualificationsAsync(
            [CreateResponse("Z1234567", lastDateForRegistration: new DateOnly(2026, 08, 31))],
            _snapshotDate,
            CancellationToken.None);

        context.ChangeTracker.Clear();
        var stored = await context.RegulatedQaaQualification.SingleAsync();
        var history = await context.RegulatedQaaQualificationHistory.SingleAsync();

        Assert.Equal(new DateOnly(2026, 08, 31), stored.LastDateForRegistration);
        Assert.Equal(QaaImportComparisonOutcome.LastDateForRegistrationChanged, stored.LatestImportComparisonOutcome);
        Assert.Equal(QaaLastDateForRegistrationChangeType.Extended, stored.LastDateForRegistrationChangeType);
        Assert.Equal(history.Id, stored.LatestQaaQualificationHistoryId);
        Assert.NotEqual(existingHistoryId, stored.LatestQaaQualificationHistoryId);
    }

    [Fact]
    public async Task ImportQaaQualificationsAsync_WhenAimCodeIsNew_PersistsLastDateForCertifications()
    {
        var (context, repository) = CreateRepository();

        await repository.ImportQaaQualificationsAsync(
            [CreateResponse("Z1234567", lastDateForCertifications: new DateOnly(2029, 12, 31))],
            _snapshotDate,
            CancellationToken.None);

        context.ChangeTracker.Clear();
        var stored = await context.RegulatedQaaQualification.SingleAsync();

        Assert.Equal(new DateOnly(2029, 12, 31), stored.LastDateForCertifications);
    }

    [Fact]
    public async Task ImportQaaQualificationsAsync_WhenAimCodeExists_UpdatesLastDateForCertifications()
    {
        var (context, repository) = CreateRepository();
        var existing = CreateExistingQualification("Z1234567");
        context.RegulatedQaaQualification.Add(existing);
        await context.SaveChangesAsync();

        await repository.ImportQaaQualificationsAsync(
            [CreateResponse("Z1234567", lastDateForCertifications: new DateOnly(2030, 06, 30))],
            _snapshotDate,
            CancellationToken.None);

        context.ChangeTracker.Clear();
        var stored = await context.RegulatedQaaQualification.SingleAsync();

        Assert.Equal(new DateOnly(2030, 06, 30), stored.LastDateForCertifications);
    }

    [Fact]
    public async Task ImportQaaQualificationsAsync_WhenExistingRowsAreEmpty_CreatesHistoryForEachNewQualification()
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
        Assert.All(stored, qualification => Assert.NotNull(qualification.LatestQaaQualificationHistoryId));
        Assert.Equal(2, await context.RegulatedQaaQualificationHistory.CountAsync());
    }

    [Fact]
    public async Task ImportQaaQualificationsAsync_WhenMaterialChangeAfterSeededHistory_UpdatesLatestHistory()
    {
        var (context, repository) = CreateRepository();
        var existingHistoryId = Guid.NewGuid();
        var existing = CreateExistingQualification("Z1234567", latestHistoryId: existingHistoryId);
        context.RegulatedQaaQualification.Add(existing);
        await context.SaveChangesAsync();

        await repository.ImportQaaQualificationsAsync(
            [CreateResponse("Z1234567", lastDateForRegistration: new DateOnly(2024, 08, 31))],
            _snapshotDate,
            CancellationToken.None);

        context.ChangeTracker.Clear();
        var stored = await context.RegulatedQaaQualification.SingleAsync();
        var history = await context.RegulatedQaaQualificationHistory.SingleAsync();
        Assert.Equal(QaaImportComparisonOutcome.LastDateForRegistrationChanged, stored.LatestImportComparisonOutcome);
        Assert.Equal(QaaLastDateForRegistrationChangeType.BroughtForward, stored.LastDateForRegistrationChangeType);
        Assert.Equal(history.Id, stored.LatestQaaQualificationHistoryId);
        Assert.NotEqual(existingHistoryId, stored.LatestQaaQualificationHistoryId);
    }

    [Fact]
    public async Task ImportQaaQualificationsAsync_WhenUnchangedAfterMaterialChange_KeepsLatestHistory()
    {
        var (context, repository) = CreateRepository();
        var latestHistoryId = Guid.NewGuid();
        var existing = CreateExistingQualification("Z1234567", latestHistoryId: latestHistoryId);
        existing.Update(
            new DateTime(2024, 01, 21),
            "Access to Higher Education Diploma (Science)",
            "Test Awarding Body",
            new DateOnly(2023, 09, 01),
            new DateOnly(2026, 08, 31),
            null,
            SectorSubjectArea.Science,
            new DateTime(2024, 01, 21));
        existing.RecordHistory(latestHistoryId);
        context.RegulatedQaaQualification.Add(existing);
        await context.SaveChangesAsync();

        await repository.ImportQaaQualificationsAsync(
            [CreateResponse("Z1234567", lastDateForRegistration: new DateOnly(2026, 08, 31))],
            _snapshotDate,
            CancellationToken.None);

        context.ChangeTracker.Clear();
        var stored = await context.RegulatedQaaQualification.SingleAsync();
        Assert.Equal(QaaImportComparisonOutcome.NotChanged, stored.LatestImportComparisonOutcome);
        Assert.Equal(latestHistoryId, stored.LatestQaaQualificationHistoryId);
        Assert.Empty(await context.RegulatedQaaQualificationHistory.ToListAsync());
    }

    [Fact]
    public async Task ImportQaaQualificationsAsync_WhenSeededExistingRowHasNoHistory_BackfillsInitialHistory()
    {
        var (context, repository) = CreateRepository();
        var existing = CreateExistingQualification("Z1234567");
        context.RegulatedQaaQualification.Add(existing);
        await context.SaveChangesAsync();

        await repository.ImportQaaQualificationsAsync(
            [CreateResponse("Z1234567")],
            _snapshotDate,
            CancellationToken.None);

        context.ChangeTracker.Clear();
        var stored = await context.RegulatedQaaQualification.SingleAsync();
        var history = await context.RegulatedQaaQualificationHistory.SingleAsync();

        Assert.Equal(QaaImportComparisonOutcome.NotChanged, stored.LatestImportComparisonOutcome);
        Assert.Equal(history.Id, stored.LatestQaaQualificationHistoryId);
        Assert.Equal(stored.Id, history.QaaQualificationId);
        Assert.Equal(QaaLastDateForRegistrationChangeType.NotChanged, history.LastDateForRegistrationChangeType);
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
        Guid? latestHistoryId = null,
        bool isDiscontinued = false)
    {
        var qualification = RegulatedQaaQualification.Create(
            new DateTime(2024, 01, 15),
            aimCode,
            "Access to Higher Education Diploma (Science)",
            "Test Awarding Body",
            new DateOnly(2023, 09, 01),
            new DateOnly(2025, 08, 31),
            SectorSubjectArea.Science,
            isDiscontinued ? new DateOnly(2024, 01, 31) : null,
            new DateTime(2024, 01, 15));

        if (latestHistoryId.HasValue)
        {
            qualification.RecordHistory(latestHistoryId.Value);
        }

        return qualification;
    }

    private static QaaQualificationResponse CreateResponse(
        string aimCode,
        string title = "Access to Higher Education Diploma (Science)",
        DateOnly? lastDateForRegistration = null,
        DateOnly? discontinuedDate = null,
        DateOnly? lastDateForCertifications = null)
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
            DiscontinuedDate = discontinuedDate,
            LastDateForCertifications = lastDateForCertifications ?? new DateOnly(2027, 12, 31)
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
