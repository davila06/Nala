using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Pets.Commands.CreatePet;
using PawTrack.Domain.Pets;

namespace PawTrack.UnitTests.Pets.Handlers;

public sealed class CreatePetCommandHandlerTests
{
    private readonly IPetRepository _petRepo = Substitute.For<IPetRepository>();
    private readonly IBlobStorageService _blobStorage = Substitute.For<IBlobStorageService>();
    private readonly IImageProcessor _imageProcessor = Substitute.For<IImageProcessor>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private readonly CreatePetCommandHandler _sut;

    public CreatePetCommandHandlerTests()
    {
        _sut = new CreatePetCommandHandler(_petRepo, _blobStorage, _imageProcessor, _uow);
    }

    [Fact]
    public async Task Handle_WithoutPhoto_CreatesPetWithNullPhotoUrl()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var cmd = new CreatePetCommand(
            ownerId, "Firulais", PetSpecies.Dog, "Labrador",
            new DateOnly(2022, 1, 15),
            PhotoBytes: null, PhotoContentType: null, PhotoFileName: null);

        _uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrEmpty();

        await _petRepo.Received(1).AddAsync(
            Arg.Is<Pet>(p => p.OwnerId == ownerId && p.Name == "Firulais"),
            Arg.Any<CancellationToken>());

        await _blobStorage.DidNotReceive().UploadAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(),
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithPhoto_UploadsResizedImageAndSetsPhotoUrl()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var photoBytes = new byte[] { 0xFF, 0xD8 }; // minimal fake JPEG header
        var resizedBytes = new byte[] { 0x01, 0x02 };
        const string expectedUrl = "https://storage.blob.core.windows.net/pet-photos/img.jpg";

        var cmd = new CreatePetCommand(
            ownerId, "Luna", PetSpecies.Cat, null, null,
            photoBytes, "image/jpeg", "luna.jpg");

        _imageProcessor.ResizeAsync(photoBytes, 800, Arg.Any<CancellationToken>())
            .Returns(resizedBytes);

        _blobStorage.UploadAsync(
            "pet-photos", Arg.Any<string>(), Arg.Any<Stream>(), "image/jpeg",
            Arg.Any<CancellationToken>())
            .Returns(expectedUrl);

        _uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        await _imageProcessor.Received(1).ResizeAsync(photoBytes, 800, Arg.Any<CancellationToken>());

        await _petRepo.Received(1).AddAsync(
            Arg.Is<Pet>(p => p.PhotoUrl == expectedUrl),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NullPhotoBytes_DoesNotCallImageProcessor()
    {
        // Arrange
        var cmd = new CreatePetCommand(
            Guid.NewGuid(), "Coco", PetSpecies.Bird, null, null,
            null, null, null);

        _uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        // Act
        await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await _imageProcessor.DidNotReceive().ResizeAsync(
            Arg.Any<byte[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }
}
