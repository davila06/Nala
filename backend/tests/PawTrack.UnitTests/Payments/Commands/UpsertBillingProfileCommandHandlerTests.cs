using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Payments.Commands.UpsertBillingProfile;
using PawTrack.Application.Payments.Interfaces;
using PawTrack.Domain.Payments;

namespace PawTrack.UnitTests.Payments.Commands;

public sealed class UpsertBillingProfileCommandHandlerTests
{
    private readonly IUserBillingProfileRepository _profileRepo = Substitute.For<IUserBillingProfileRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private UpsertBillingProfileCommandHandler CreateSut() =>
        new(_profileRepo, _unitOfWork);

    [Fact]
    public async Task Handle_WhenNoExistingProfile_CreatesAndSavesNewProfile()
    {
        var userId = Guid.NewGuid();
        _profileRepo.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((UserBillingProfile?)null);

        var sut = CreateSut();
        var result = await sut.Handle(
            new UpsertBillingProfileCommand(
                UserId: userId,
                IdentificationType: TaxIdentificationType.Juridica,
                IdentificationNumber: "3-101-999888",
                LegalName: "Veterinaria El Bosque S.A.",
                BillingEmail: "facturas@elbosque.cr",
                RequiresInvoice: true),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IdentificationNumber.Should().Be("3101999888");
        result.Value.LegalName.Should().Be("VETERINARIA EL BOSQUE S.A.");
        result.Value.RequiresInvoice.Should().BeTrue();

        await _profileRepo.Received(1).AddAsync(Arg.Any<UserBillingProfile>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenExistingProfile_UpdatesExistingProfile()
    {
        var userId = Guid.NewGuid();
        var existing = UserBillingProfile.Create(
            userId, TaxIdentificationType.Fisica, "101110222", "Carlos Perez", "carlos@test.cr");

        _profileRepo.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(existing);

        var sut = CreateSut();
        var result = await sut.Handle(
            new UpsertBillingProfileCommand(
                UserId: userId,
                IdentificationType: TaxIdentificationType.Juridica,
                IdentificationNumber: "3101555666",
                LegalName: "Carlos Perez S.A.",
                BillingEmail: "contabilidad@perez.cr",
                RequiresInvoice: false),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        existing.IdentificationType.Should().Be(TaxIdentificationType.Juridica);
        existing.LegalName.Should().Be("CARLOS PEREZ S.A.");
        existing.RequiresInvoice.Should().BeFalse();
        _profileRepo.Received(1).Update(existing);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
