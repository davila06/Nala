using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Allies.Queries.GetMyAllyAlerts;
using PawTrack.Application.Allies.Queries.GetPendingAllies;
using PawTrack.Application.Clinics.Queries.GetPendingClinics;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Pets.Queries.GetMyPets;
using PawTrack.Domain.Allies;
using PawTrack.Domain.Clinics;
using PawTrack.Domain.Notifications;
using PawTrack.Domain.Pets;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// SEC-02 (R108) — GetMyPetsQueryHandler must cap results at 100 to prevent DoS via memory exhaustion.
/// SEC-03a (R109) — GetPendingAlliesQueryHandler must cap results at 200.
/// SEC-03b (R110) — GetPendingClinicsQueryHandler must cap results at 200.
/// SEC-04 (R111) — GetMyAllyAlertsQueryHandler must cap results at 200.
///
/// Risk: A bot account or adversarially large queue can trigger unbounded DB → memory load
/// when all rows are materialised into a List before the response is serialised.
/// The fix caps results at a constant in the handler layer, preventing OOM conditions.
/// </summary>
public sealed class Round108To111SecurityRegressionTests
{
    // ── R108: GetMyPets — cap at 100 ────────────────────────────────────────

    [Fact]
    public async Task R108_GetMyPets_WhenRepoReturnsMoreThanCap_HandlerReturnsAtMost100()
    {
        // Arrange — 125 pets exceeds the cap of 100
        var ownerId = Guid.NewGuid();
        var petRepo = Substitute.For<IPetRepository>();
        IReadOnlyList<Pet> oversized = Enumerable.Range(0, 125)
            .Select(_ => Pet.Create(ownerId, "Rex", PetSpecies.Dog, null, null))
            .ToList()
            .AsReadOnly();

        petRepo.GetByOwnerIdAsync(ownerId, Arg.Any<CancellationToken>())
               .Returns(oversized);

        var handler = new GetMyPetsQueryHandler(petRepo);

        // Act
        var result = await handler.Handle(new GetMyPetsQuery(ownerId), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Count.Should().BeLessThanOrEqualTo(100,
            because: "unbounded pet lists enable DoS via memory exhaustion; handler must cap at MaxResults=100");
    }

    [Fact]
    public async Task R108_GetMyPets_WhenRepoReturnsBelowCap_HandlerReturnsAllItems()
    {
        // Arrange — 5 pets, well under the cap
        var ownerId = Guid.NewGuid();
        var petRepo = Substitute.For<IPetRepository>();
        IReadOnlyList<Pet> small = Enumerable.Range(0, 5)
            .Select(_ => Pet.Create(ownerId, "Rex", PetSpecies.Dog, null, null))
            .ToList()
            .AsReadOnly();

        petRepo.GetByOwnerIdAsync(ownerId, Arg.Any<CancellationToken>())
               .Returns(small);

        var handler = new GetMyPetsQueryHandler(petRepo);

        // Act
        var result = await handler.Handle(new GetMyPetsQuery(ownerId), CancellationToken.None);

        // Assert — below cap, all items must come through
        result.IsSuccess.Should().BeTrue();
        result.Value!.Count.Should().Be(5);
    }

    // ── R109: GetPendingAllies — cap at 200 ─────────────────────────────────

    [Fact]
    public async Task R109_GetPendingAllies_WhenRepoReturnsMoreThanCap_HandlerReturnsAtMost200()
    {
        // Arrange — 250 pending allies exceeds the admin-queue cap of 200
        var allyRepo = Substitute.For<IAllyProfileRepository>();
        IReadOnlyList<AllyProfile> oversized = Enumerable.Range(0, 250)
            .Select(_ => AllyProfile.Create(
                Guid.NewGuid(), "Rescue CR", AllyType.Rescuer,
                "San José", 9.93, -84.08, 5_000))
            .ToList()
            .AsReadOnly();

        allyRepo.GetAllPendingAsync(Arg.Any<CancellationToken>())
                .Returns(oversized);

        var handler = new GetPendingAlliesQueryHandler(allyRepo);

        // Act
        var result = await handler.Handle(new GetPendingAlliesQuery(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Count.Should().BeLessThanOrEqualTo(200,
            because: "unbounded admin ally lists enable DoS via memory exhaustion; handler must cap at MaxResults=200");
    }

    // ── R110: GetPendingClinics — cap at 200 ────────────────────────────────

    [Fact]
    public async Task R110_GetPendingClinics_WhenRepoReturnsMoreThanCap_HandlerReturnsAtMost200()
    {
        // Arrange — 250 pending clinics exceeds the admin-queue cap of 200
        var clinicRepo = Substitute.For<IClinicRepository>();
        IReadOnlyList<Clinic> oversized = Enumerable.Range(0, 250)
            .Select(i => Clinic.Create(
                Guid.NewGuid(), $"Clínica {i}", $"LIC{i:D6}",
                "Av. Central, San José", 9.93m, -84.08m, $"clinic{i}@example.com"))
            .ToList()
            .AsReadOnly();

        clinicRepo.GetAllPendingAsync(Arg.Any<CancellationToken>())
                  .Returns(oversized);

        var handler = new GetPendingClinicsQueryHandler(clinicRepo);

        // Act
        var result = await handler.Handle(new GetPendingClinicsQuery(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Count.Should().BeLessThanOrEqualTo(200,
            because: "unbounded admin clinic lists enable DoS via memory exhaustion; handler must cap at MaxResults=200");
    }

    // ── R111: GetMyAllyAlerts — cap at 200 ──────────────────────────────────

    [Fact]
    public async Task R111_GetMyAllyAlerts_WhenRepoReturnsMoreThanCap_HandlerReturnsAtMost200()
    {
        // Arrange — 250 notifications exceeds the alert-inbox cap of 200
        var userId    = Guid.NewGuid();
        var allyRepo  = Substitute.For<IAllyProfileRepository>();
        var notifRepo = Substitute.For<INotificationRepository>();

        // GetVerifiedByUserIdAsync must return non-null to pass the verified-ally gate
        var verifiedProfile = AllyProfile.Create(
            userId, "Rescue CR", AllyType.Rescuer, "Heredia", 10.0, -84.1, 3_000);
        allyRepo.GetVerifiedByUserIdAsync(userId, Arg.Any<CancellationToken>())
                .Returns(verifiedProfile);

        IReadOnlyList<Notification> oversized = Enumerable.Range(0, 250)
            .Select(_ => Notification.Create(
                userId, NotificationType.VerifiedAllyAlert, "Alerta", "Mascota cerca"))
            .ToList()
            .AsReadOnly();

        notifRepo.GetByUserIdAndTypeAsync(
                     userId, NotificationType.VerifiedAllyAlert, Arg.Any<CancellationToken>())
                 .Returns(oversized);

        var handler = new GetMyAllyAlertsQueryHandler(allyRepo, notifRepo);

        // Act
        var result = await handler.Handle(new GetMyAllyAlertsQuery(userId), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Count.Should().BeLessThanOrEqualTo(200,
            because: "unbounded alert inboxes enable DoS via memory exhaustion; handler must cap at MaxResults=200");
    }

    [Fact]
    public async Task R111_GetMyAllyAlerts_WhenProfileIsNull_ReturnsForbiddenFailure()
    {
        // Arrange — no verified ally profile → gate blocks access
        var userId    = Guid.NewGuid();
        var allyRepo  = Substitute.For<IAllyProfileRepository>();
        var notifRepo = Substitute.For<INotificationRepository>();

        allyRepo.GetVerifiedByUserIdAsync(userId, Arg.Any<CancellationToken>())
                .Returns((AllyProfile?)null);

        var handler = new GetMyAllyAlertsQueryHandler(allyRepo, notifRepo);

        // Act
        var result = await handler.Handle(new GetMyAllyAlertsQuery(userId), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        await notifRepo.DidNotReceive()
            .GetByUserIdAndTypeAsync(
                Arg.Any<Guid>(), Arg.Any<NotificationType>(), Arg.Any<CancellationToken>());
    }
}
