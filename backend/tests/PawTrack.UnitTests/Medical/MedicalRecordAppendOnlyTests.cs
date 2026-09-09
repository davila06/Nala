using FluentAssertions;
using PawTrack.Domain.Medical;
using PawTrack.Domain.Pets;

namespace PawTrack.UnitTests.Medical;

public sealed class MedicalRecordAppendOnlyTests
{
    [Fact]
    public void Record_CapturesImmutableVersionOneSnapshot()
    {
        var record = MedicalRecord.Create(
            Guid.NewGuid(), Guid.NewGuid(), MedicalRecordType.Checkup,
            new DateOnly(2026, 9, 9), "Initial note", "Dr. Rivera", "Vet CR", null);

        record.Version.Should().Be(1);
        record.IsSuperseded.Should().BeFalse();
        record.Description.Should().Be("Initial note");
    }

    [Fact]
    public void Record_CanBeSupersededWithoutMutatingTheOriginalContent()
    {
        var record = MedicalRecord.Create(
            Guid.NewGuid(), Guid.NewGuid(), MedicalRecordType.Checkup,
            new DateOnly(2026, 9, 9), "Initial note", null, null, null);

        record.Supersede(Guid.NewGuid(), "Correction requested by owner");

        record.IsSuperseded.Should().BeTrue();
        record.SupersededAt.Should().NotBeNull();
        record.Description.Should().Be("Initial note");
    }
}