using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.ServiceProviders;
using PawTrack.Domain.Auth;
using PawTrack.Domain.Common;
using PawTrack.Domain.ServiceProviders;

namespace PawTrack.UnitTests.ServiceProviders;

public sealed class RegisterServiceProviderCommandHandlerTests
{
    [Fact]
    public async Task Handle_NewEmail_CreatesPendingProviderWithProviderRole()
    {
        var users = Substitute.For<IUserRepository>();
        var providers = Substitute.For<IServiceProviderRepository>();
        var passwordHasher = Substitute.For<IPasswordHasher>();
        var emailSender = Substitute.For<IEmailSender>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        passwordHasher.Hash("SecurePass1!").Returns("hash");
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = new RegisterServiceProviderCommandHandler(
            users,
            providers,
            passwordHasher,
            emailSender,
            unitOfWork,
            NullLogger<RegisterServiceProviderCommandHandler>.Instance);

        var result = await handler.Handle(new RegisterServiceProviderCommand(
            "Escuela Canina CR",
            "Adiestramiento positivo",
            ServiceProviderCategory.Trainer,
            "San Jose",
            9.9347m,
            -84.0875m,
            "hola@ejemplo.cr",
            "SecurePass1!"), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Pending");
        await users.Received(1).AddAsync(
            Arg.Is<User>(user => user.Role == UserRole.ServiceProvider), Arg.Any<CancellationToken>());
        await providers.Received(1).AddAsync(
            Arg.Is<ServiceProvider>(provider =>
                provider.Category == ServiceProviderCategory.Trainer &&
                provider.Status == ServiceProviderStatus.Pending),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}