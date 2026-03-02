namespace SFA.DAS.AODP.Jobs.UnitTests.Entities;

public class RegulatedQaaQualificationTests
{
    private const string TestAimCode = "Z1234567";
    private const string TestQualificationTitle = "Access to Higher Education Diploma (Science)";
    private const string TestAwardingBody = "Test Awarding Body";
    private readonly DateTime _testSnapshot = new(2024, 02, 15);
    private readonly DateOnly _testStartDate = new(2023, 09, 01);
    private readonly DateOnly _testLastRegistrationDate = new(2025, 08, 31);
    private readonly SectorSubjectArea _testSectorSubjectArea = SectorSubjectArea.FromTiers("1", "1");

    [Fact]
    public void Create_WithValidParameters_ReturnsInstanceWithCorrectValuesAndDefaults()
    {
        // Act
        var qualification = RegulatedQaaQualification.Create(
            _testSnapshot,
            TestAimCode,
            TestQualificationTitle,
            TestAwardingBody,
            _testStartDate,
            _testLastRegistrationDate,
            _testSectorSubjectArea);

        // Assert
        Assert.Equal(_testSnapshot, qualification.DateOfDataSnapshot);
        Assert.Equal(TestAimCode, qualification.AimCode);
        Assert.Equal(TestQualificationTitle, qualification.QualificationTitle);
        Assert.Equal(TestAwardingBody, qualification.AwardingBody);
        Assert.Equal(_testStartDate, qualification.StartDate);
        Assert.Equal(_testLastRegistrationDate, qualification.LastDateForRegistration);
        Assert.Same(_testSectorSubjectArea, qualification.SectorSubjectArea);
        Assert.Equal("Level 3", qualification.Level);
        Assert.Equal("Access to HE", qualification.Type);
        Assert.Equal("Approved", qualification.Status);
        Assert.Null(qualification.LastFundingApprovalEndDate);
    }

    [Fact]
    public void Create_WithVaryingInputs_MapsPropertiesCorrectly()
    {
        // Arrange
        var qualifications = new[]
        {
            ("Z1234567", "Diploma 1", "Body 1"),
            ("Z7654321", "Diploma 2", "Body 2"),
            ("Z1111111", "Diploma 3", "Body 3")
        };

        // Act
        var createdQualifications = qualifications
            .Select(q => RegulatedQaaQualification.Create(
                _testSnapshot,
                q.Item1,
                q.Item2,
                q.Item3,
                _testStartDate,
                _testLastRegistrationDate,
                _testSectorSubjectArea))
            .ToList();

        // Assert
        Assert.Equal(3, createdQualifications.Count);
        Assert.Equal("Z1234567", createdQualifications[0].AimCode);
        Assert.Equal("Diploma 1", createdQualifications[0].QualificationTitle);
        Assert.Equal("Body 1", createdQualifications[0].AwardingBody);

        Assert.Equal("Z7654321", createdQualifications[1].AimCode);
        Assert.Equal("Diploma 2", createdQualifications[1].QualificationTitle);
        Assert.Equal("Body 2", createdQualifications[1].AwardingBody);

        Assert.Equal("Z1111111", createdQualifications[2].AimCode);
        Assert.Equal("Diploma 3", createdQualifications[2].QualificationTitle);
        Assert.Equal("Body 3", createdQualifications[2].AwardingBody);
    }

    [Fact]
    public void Create_WithDifferentSnapshots_EachInstancePreservesCorrectDate()
    {
        // Arrange
        var snapshot1 = new DateTime(2024, 02, 15);
        var snapshot2 = new DateTime(2024, 03, 15);
        var snapshot3 = new DateTime(2024, 04, 15);

        // Act
        var qualification1 = RegulatedQaaQualification.Create(
            snapshot1, TestAimCode, TestQualificationTitle, TestAwardingBody,
            _testStartDate, _testLastRegistrationDate, _testSectorSubjectArea);

        var qualification2 = RegulatedQaaQualification.Create(
            snapshot2, TestAimCode, TestQualificationTitle, TestAwardingBody,
            _testStartDate, _testLastRegistrationDate, _testSectorSubjectArea);

        var qualification3 = RegulatedQaaQualification.Create(
            snapshot3, TestAimCode, TestQualificationTitle, TestAwardingBody,
            _testStartDate, _testLastRegistrationDate, _testSectorSubjectArea);

        // Assert
        Assert.Equal(snapshot1, qualification1.DateOfDataSnapshot);
        Assert.Equal(snapshot2, qualification2.DateOfDataSnapshot);
        Assert.Equal(snapshot3, qualification3.DateOfDataSnapshot);
    }
}