using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Clinics.Commands.ManageApiKey;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Clinics;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.UnitTests.Clinics;

public sealed class CreateClinicApiKeyCommandHandlerTests
{
    [Fact]
    public async Task Handle_ActiveApiKeyLimitReached_ReturnsFailureWithoutCreatingKey()
    {
        var userId = Guid.NewGuid();
        var clinic = Clinic.Create(userId, "VetSalud", "SEN-123", "Heredia", 10m, -84.1m, "vet@x.com");
        var clinics = Substitute.For<IClinicRepository>();
        var keys = Substitute.For<IClinicApiKeyRepository>();
        var subscriptions = Substitute.For<ISubscriptionRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        subscriptions.GetActiveForClinicAsync(clinic.Id, Arg.Any<CancellationToken>())
            .Returns(Subscription.CreateForClinic(clinic.Id, userId, SubscriptionTier.ClinicPartner, "PARTNER2", 35000m));
        var existingKeys = Enumerable.Range(1, 10)
            .Select(index => ClinicApiKey.Create(clinic.Id, $"hash-{index}", $"key-{index}"))
            .ToList();
        keys.GetForClinicAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(existingKeys);

        var result = await new CreateClinicApiKeyCommandHandler(clinics, keys, subscriptions, unitOfWork)
            .Handle(new CreateClinicApiKeyCommand(clinic.Id, userId, "new-key"), default);

        result.IsFailure.Should().BeTrue();
        await keys.DidNotReceive().AddAsync(Arg.Any<ClinicApiKey>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownScope_ReturnsFailureWithoutCreatingKey()
    {
        var userId = Guid.NewGuid();
        var clinic = Clinic.Create(userId, "VetSalud", "SEN-123", "Heredia", 10m, -84.1m, "vet@x.com");
        var clinics = Substitute.For<IClinicRepository>();
        var keys = Substitute.For<IClinicApiKeyRepository>();
        var subscriptions = Substitute.For<ISubscriptionRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        subscriptions.GetActiveForClinicAsync(clinic.Id, Arg.Any<CancellationToken>())
            .Returns(Subscription.CreateForClinic(clinic.Id, userId, SubscriptionTier.ClinicPartner, "PARTNER1", 35000m));

        var result = await new CreateClinicApiKeyCommandHandler(clinics, keys, subscriptions, unitOfWork)
            .Handle(new CreateClinicApiKeyCommand(clinic.Id, userId, "Invalid", ["medical:read", "admin:all"]), default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("scope", StringComparison.OrdinalIgnoreCase));
        await keys.DidNotReceive().AddAsync(Arg.Any<ClinicApiKey>(), Arg.Any<CancellationToken>());
    }
}
