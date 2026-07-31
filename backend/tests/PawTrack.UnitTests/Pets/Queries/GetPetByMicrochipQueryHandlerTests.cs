using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Pets.Queries.GetPetByMicrochip;
using PawTrack.Domain.Pets;

namespace PawTrack.UnitTests.Pets.Queries;

public sealed class GetPetByMicrochipQueryHandlerTests
{
    private readonly IPetRepository _petRepo = Substitute.For<IPetRepository>();
    private readonly GetPetByMicrochipQueryHandler _sut;

    public GetPetByMicrochipQueryHandlerTests()
    {
        _sut = new GetPetByMicrochipQueryHandler(_petRepo);
    }

    [Fact]
    public async Task Handle_RegisteredChip_ReturnsPetDto()
    {
        var ownerId = Guid.NewGuid();
        var pet = Pet.Create(ownerId, "Max", PetSpecies.Dog, null, null);
        pet.SetMicrochip("0006000123456");

        _petRepo.GetByMicrochipIdAsync("0006000123456", Arg.Any<CancellationToken>()).Returns(pet);

        var result = await _sut.Handle(new GetPetByMicrochipQuery("0006000123456"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Max");
        result.Value.MicrochipId.Should().Be("0006000123456");
    }

    [Fact]
    public async Task Handle_UnknownChip_ReturnsFailure()
    {
        _petRepo.GetByMicrochipIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((Pet?)null);

        var result = await _sut.Handle(new GetPetByMicrochipQuery("9999999999999"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Contains("registrado"));
    }

    [Fact]
    public async Task Handle_NormalizesChipIdToUppercase()
    {
        var ownerId = Guid.NewGuid();
        var pet = Pet.Create(ownerId, "Luna", PetSpecies.Cat, null, null);
        pet.SetMicrochip("ABCDEF123456789");

        // Simulate stored value is uppercase; input is lowercase
        _petRepo.GetByMicrochipIdAsync("ABCDEF123456789", Arg.Any<CancellationToken>()).Returns(pet);

        var result = await _sut.Handle(new GetPetByMicrochipQuery("abcdef123456789"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _petRepo.Received(1).GetByMicrochipIdAsync("ABCDEF123456789", Arg.Any<CancellationToken>());
    }
}
