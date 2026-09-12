using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Payments.Commands.SavePaymentProfile;
using PawTrack.Application.Payments.Interfaces;
using PawTrack.Domain.Payments;

namespace PawTrack.UnitTests.Payments.Commands;

public sealed class SavePaymentProfileCommandHandlerTests
{
    private readonly IUserPaymentProfileRepository _profileRepo = Substitute.For<IUserPaymentProfileRepository>();
    private readonly IPaymentGatewayService _gatewayService = Substitute.For<IPaymentGatewayService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private SavePaymentProfileCommandHandler CreateSut() =>
        new(_profileRepo, _gatewayService, _unitOfWork);

    [Fact]
    public async Task Handle_WhenGatewayTokenizationFails_ReturnsFailure()
    {
        _gatewayService.TokenizeTransientTokenAsync(Arg.Any<TokenizePaymentRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TokenizePaymentResult(false, null, null, null, null, null, null, "Tarjeta expirada"));

        var sut = CreateSut();
        var result = await sut.Handle(new SavePaymentProfileCommand(Guid.NewGuid(), "invalid_jwt"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Tarjeta expirada");
    }

    [Fact]
    public async Task Handle_WhenGatewayTokenizationSucceeds_SavesProfileAndReturnsDto()
    {
        var userId = Guid.NewGuid();
        _gatewayService.TokenizeTransientTokenAsync(Arg.Any<TokenizePaymentRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TokenizePaymentResult(true, "cust_123", "instr_456", "Mastercard", "8888", 10, 2030, null));

        _profileRepo.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new List<UserPaymentProfile>());

        var sut = CreateSut();
        var result = await sut.Handle(
            new SavePaymentProfileCommand(userId, "valid_jwt", "MARIA MORA", SetAsDefault: true),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CardBrand.Should().Be("Mastercard");
        result.Value.LastFourDigits.Should().Be("8888");
        result.Value.IsDefault.Should().BeTrue();
        await _profileRepo.Received(1).AddAsync(Arg.Any<UserPaymentProfile>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
