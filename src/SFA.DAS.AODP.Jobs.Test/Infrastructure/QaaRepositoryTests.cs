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
            isDiscontinued,
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
