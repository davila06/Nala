using FluentAssertions;
using PawTrack.Domain.LostPets;
using PawTrack.Domain.LostPets.Events;
using PawTrack.Domain.Pets;

namespace PawTrack.UnitTests.LostPets.Domain;

public sealed class LostPetEventStateMachineTests
{
    private static LostPetEvent CreateActive()
        => LostPetEvent.Create(
            Guid.NewGuid(), Guid.NewGuid(),
            "Seen near the park",
            9.9281, -84.0907,
            DateTimeOffset.UtcNow.AddHours(-3));

    [Fact]
    public void Create_NewReport_IsActiveWithDomainEvent()
    {
        var report = CreateActive();

        report.Status.Should().Be(LostPetStatus.Active);
        report.ResolvedAt.Should().BeNull();
        report.DomainEvents.Should().ContainSingle(
            e => e is LostPetReportedDomainEvent);
    }

    [Fact]
    public void Resolve_ActiveToReunited_Succeeds()
    {
        var report = CreateActive();

        var result = report.Resolve(LostPetStatus.Reunited);

        result.IsSuccess.Should().BeTrue();
        report.Status.Should().Be(LostPetStatus.Reunited);
        report.ResolvedAt.Should().NotBeNull();
        report.DomainEvents.Should().Contain(e => e is PetReunitedDomainEvent);
    }

    [Fact]
    public void ResolveAsReunited_WithRecoveryMetrics_PersistsMetadata()
    {
        // Arrange
        var report = LostPetEvent.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Seen near school",
            9.9362,
            -84.0514,
            DateTimeOffset.UtcNow.AddHours(-5));

        var reunitedAt = DateTimeOffset.UtcNow;
        const double reunionLat = 9.9380;
        const double reunionLng = -84.0480;

        // Act
        var result = report.ResolveAsReunited(
            reunitedAt,
            reunionLat,
            reunionLng,
            "Montes de Oca");

        // Assert
        result.IsSuccess.Should().BeTrue();
        report.Status.Should().Be(LostPetStatus.Reunited);
        report.ReunionLat.Should().Be(reunionLat);
        report.ReunionLng.Should().Be(reunionLng);
        report.CantonName.Should().Be("Montes de Oca");
        report.RecoveryDistanceMeters.Should().NotBeNull();
        report.RecoveryDistanceMeters.Should().BeGreaterThan(0);
        report.RecoveryTime.Should().NotBeNull();
        report.RecoveryTime.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public void ResolveAsReunited_WithoutCoordinates_StillPersistsRecoveryTime()
    {
        // Arrange
        var report = LostPetEvent.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Near park",
            null,
            null,
            DateTimeOffset.UtcNow.AddHours(-2));

        // Act
        var result = report.ResolveAsReunited(
            DateTimeOffset.UtcNow,
            null,
            null,
            null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        report.RecoveryTime.Should().NotBeNull();
        report.RecoveryDistanceMeters.Should().BeNull();
        report.ReunionLat.Should().BeNull();
        report.ReunionLng.Should().BeNull();
        report.CantonName.Should().BeNull();
    }

    [Fact]
    public void Resolve_ActiveToCancelled_SucceedsWithoutReunitedEvent()
    {
        var report = CreateActive();

        var result = report.Resolve(LostPetStatus.Cancelled);

        result.IsSuccess.Should().BeTrue();
        report.Status.Should().Be(LostPetStatus.Cancelled);
        report.DomainEvents.Should().NotContain(e => e is PetReunitedDomainEvent);
    }

    [Theory]
    [InlineData(LostPetStatus.Reunited)]
    [InlineData(LostPetStatus.Cancelled)]
    public void Resolve_AlreadyResolved_ReturnsFailure(LostPetStatus firstTransition)
    {
        var report = CreateActive();
        report.Resolve(firstTransition);

        var result = report.Resolve(LostPetStatus.Cancelled);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Only active reports can be resolved.");
    }

    [Fact]
    public void Resolve_ActiveToActive_ReturnsFailure()
    {
        var report = CreateActive();

        var result = report.Resolve(LostPetStatus.Active);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Cannot transition back to Active.");
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllEvents()
    {
        var report = CreateActive();
        report.DomainEvents.Should().NotBeEmpty();

        report.ClearDomainEvents();

        report.DomainEvents.Should().BeEmpty();
    }
}
