using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PawTrack.Application.Auth.Commands.ResetPassword;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Application.Collars.Services;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.ServiceProviders;
using PawTrack.Application.Stores;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Auth;
using PawTrack.Domain.Collars;
using PawTrack.Domain.Common;
using PawTrack.Domain.Notifications;
using PawTrack.Domain.Pets;
using PawTrack.Domain.ServiceProviders;
using PawTrack.Domain.Stores;

namespace PawTrack.UnitTests.Notifications;

public sealed class EnterpriseEmailHandlersTests
{
    [Fact]
    public async Task ReviewStore_WhenApproved_DispatchesWelcomeEmail()
    {
        var storeRepo = Substitute.For<IStoreRepository>();
        var auditLog = Substitute.For<IAuditLogRepository>();
        var emailSender = Substitute.For<IEmailSender>();
        var uow = Substitute.For<IUnitOfWork>();

        var store = Store.Create(Guid.NewGuid(), "VetShop CR", "Tienda", "San José", 9.9m, -84.0m, "tienda@vetshop.cr");
        storeRepo.GetByIdAsync(store.Id, Arg.Any<CancellationToken>()).Returns(store);

        var sut = new ReviewStoreCommandHandler(storeRepo, auditLog, emailSender, uow);
        var result = await sut.Handle(new ReviewStoreCommand(store.Id, Approve: true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await emailSender.Received(1).SendStoreReviewedNoticeAsync(
            "tienda@vetshop.cr", "VetShop CR", true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReviewServiceProvider_WhenApproved_DispatchesEmail()
    {
        var providerRepo = Substitute.For<IServiceProviderRepository>();
        var auditLog = Substitute.For<IAuditLogRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        var emailSender = Substitute.For<IEmailSender>();

        var provider = ServiceProvider.Create(
            Guid.NewGuid(), "Grooming Express", "Estética canina", ServiceProviderCategory.Groomer,
            "Escazú", 9.9m, -84.1m, "contacto@groomingexpress.cr");
        providerRepo.GetByIdAsync(provider.Id, Arg.Any<CancellationToken>()).Returns(provider);

        var sut = new ReviewServiceProviderCommandHandler(providerRepo, auditLog, uow, emailSender);
        var result = await sut.Handle(new ReviewServiceProviderCommand(Guid.NewGuid(), provider.Id, Approve: true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await emailSender.Received(1).SendServiceProviderReviewedNoticeAsync(
            "contacto@groomingexpress.cr", "Grooming Express", true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResetPassword_WhenSuccessful_DispatchesConfirmationEmail()
    {
        var userRepo = Substitute.For<IUserRepository>();
        var passwordHasher = Substitute.For<IPasswordHasher>();
        var uow = Substitute.For<IUnitOfWork>();
        var emailSender = Substitute.For<IEmailSender>();

        var (user, rawToken) = User.Create("user@test.cr", "hash", "Maria");
        var resetToken = user.IssuePasswordResetToken();

        userRepo.GetByPasswordResetTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);
        passwordHasher.Hash("NewSecret123!").Returns("new-hash");

        var sut = new ResetPasswordCommandHandler(userRepo, passwordHasher, uow, emailSender);
        var result = await sut.Handle(new ResetPasswordCommand(resetToken, "NewSecret123!"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await emailSender.Received(1).SendPasswordResetSuccessAsync(
            "user@test.cr", "Maria", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CollarSafeZoneBreach_DispatchesEmailToOwner()
    {
        var safeZoneRepo = Substitute.For<ICollarSafeZoneRepository>();
        var petRepo = Substitute.For<IPetRepository>();
        var notifRepo = Substitute.For<INotificationRepository>();
        var pushService = Substitute.For<IPushNotificationService>();
        var logger = Substitute.For<ILogger<CollarSafeZoneEvaluationService>>();
        var userRepo = Substitute.For<IUserRepository>();
        var emailSender = Substitute.For<IEmailSender>();

        var (ownerUser, _) = User.Create("owner@test.cr", "hash", "Fernando");
        userRepo.GetByIdAsync(ownerUser.Id, Arg.Any<CancellationToken>()).Returns(ownerUser);

        var pet = Pet.Create(ownerUser.Id, "Tobias", PetSpecies.Dog, "Poodle", null);
        petRepo.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);

        var collar = Collar.Register(pet.Id, ownerUser.Id, CollarProvider.Tractive, "COL-123");
        var polygonJson = "[{\"lat\":0,\"lng\":0},{\"lat\":0,\"lng\":20},{\"lat\":20,\"lng\":20},{\"lat\":20,\"lng\":0}]";
        var zone = CollarSafeZone.Create(collar.Id, "Casa Escazú", polygonJson);

        // First fix inside polygon to establish baseline (LastKnownInside = true)
        zone.Evaluate(10.0, 10.0);

        safeZoneRepo.GetEnabledByCollarIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns([zone]);

        var sut = new CollarSafeZoneEvaluationService(
            safeZoneRepo, petRepo, notifRepo, pushService, logger, userRepository: userRepo, emailSender: emailSender);

        // Evaluate location outside polygon (breach transition!)
        await sut.EvaluateAsync(collar, 50.0, 50.0, CancellationToken.None);

        await notifRepo.Received(1).AddAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
        await emailSender.Received(1).SendCollarSafeZoneBreachAsync(
            "owner@test.cr", "Fernando", "Tobias", "Casa Escazú", Arg.Any<CancellationToken>());
    }
}
