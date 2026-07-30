using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Auth.Commands.DeleteAccount;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Auth;
using PawTrack.Domain.Pets;

namespace PawTrack.UnitTests.Auth.Handlers;

public sealed class DeleteAccountCommandHandlerTests
{
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IPetRepository _petRepo = Substitute.For<IPetRepository>();
    private readonly IBlobStorageService _blob = Substitute.For<IBlobStorageService>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly DeleteAccountCommandHandler _sut;

    public DeleteAccountCommandHandlerTests()
    {
        _sut = new DeleteAccountCommandHandler(_userRepo, _petRepo, _blob, _hasher, _uow);
    }

    [Fact]
    public async Task Handle_CorrectPassword_SoftDeletesUserAndDeletesPets()
    {
        var (user, token) = User.Create("owner@test.com", "correct-hash", "Owner");
        user.VerifyEmail(token);

        var pet = Pet.Create(user.Id, "Max", PetSpecies.Dog, null, null);
        pet.SetPhoto("https://blob/pet-photos/max.jpg");

        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Hash("correct").Returns("correct-hash");
        _petRepo.GetByOwnerIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Pet> { pet }.AsReadOnly() as IReadOnlyList<Pet>);
        _uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var result = await _sut.Handle(
            new DeleteAccountCommand(user.Id, "correct"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.IsDeleted.Should().BeTrue();
        _petRepo.Received(1).Delete(pet);
        await _blob.Received(1).DeleteAsync("https://blob/pet-photos/max.jpg", Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WrongPassword_ReturnsFailure()
    {
        var (user, token) = User.Create("owner@test.com", "correct-hash", "Owner");
        user.VerifyEmail(token);

        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Hash("wrong").Returns("wrong-hash");

        var result = await _sut.Handle(
            new DeleteAccountCommand(user.Id, "wrong"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        user.IsDeleted.Should().BeFalse();
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
