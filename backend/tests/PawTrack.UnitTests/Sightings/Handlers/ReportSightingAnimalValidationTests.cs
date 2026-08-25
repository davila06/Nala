using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Common.Settings;
using PawTrack.Application.Sightings.Commands.ReportSighting;
using PawTrack.Domain.LostPets;
using PawTrack.Domain.Pets;
using Microsoft.Extensions.Options;

namespace PawTrack.UnitTests.Sightings.Handlers;

/// <summary>
/// Tests for the animal photo validation gate inside ReportSightingCommandHandler.
/// </summary>
public sealed class ReportSightingAnimalValidationTests
{
    private readonly ISightingRepository _sightingRepo = Substitute.For<ISightingRepository>();
    private readonly IPetRepository _petRepo = Substitute.For<IPetRepository>();
    private readonly ILostPetRepository _lostPetRepo = Substitute.For<ILostPetRepository>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IUserLocationRepository _userLocationRepo = Substitute.For<IUserLocationRepository>();
    private readonly INotificationRepository _notificationRepo = Substitute.For<INotificationRepository>();
    private readonly IBlobStorageService _blobService = Substitute.For<IBlobStorageService>();
    private readonly IImageProcessor _imageProcessor = Substitute.For<IImageProcessor>();
    private readonly IPiiScrubber _piiScrubber = Substitute.For<IPiiScrubber>();
    private readonly INotificationDispatcher _dispatcher = Substitute.For<INotificationDispatcher>();
    private readonly IAnimalPhotoValidator _validator = Substitute.For<IAnimalPhotoValidator>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private readonly Pet _pet;

    public ReportSightingAnimalValidationTests()
    {
        _pet = Pet.Create(Guid.NewGuid(), "Rex", PetSpecies.Dog, null, null);
        _petRepo.GetByIdAsync(_pet.Id, Arg.Any<CancellationToken>()).Returns(_pet);
        _lostPetRepo.GetActiveByPetIdAsync(_pet.Id, Arg.Any<CancellationToken>())
            .Returns((LostPetEvent?)null);
        _piiScrubber.Scrub(Arg.Any<string?>()).Returns(x => x.ArgAt<string?>(0));
        _uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        _imageProcessor.ResizeAsync(Arg.Any<byte[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(x => x.ArgAt<byte[]>(0));
        _blobService.UploadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(),
            Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("https://blob.core.windows.net/photos/test.jpg");
    }

    private ReportSightingCommandHandler BuildHandler(AnimalPhotoValidationSettings? settings = null) =>
        new(_sightingRepo, _petRepo, _lostPetRepo, _userRepo,
            _userLocationRepo, _notificationRepo,
            _blobService, _imageProcessor, _piiScrubber, _dispatcher,
            _validator,
            Options.Create(new Application.Common.Settings.ResolveCheckSettings()),
            Options.Create(settings ?? new AnimalPhotoValidationSettings()),
            _uow);

    private static ReportSightingCommand CommandWithPhoto() => new(
        Guid.NewGuid(), 9.9281, -84.0907, null,
        new MemoryStream([0xFF, 0xD8, 0xFF, 0xE0]),
        "image/jpeg",
        DateTimeOffset.UtcNow.AddMinutes(-5));

    [Fact]
    public async Task Handle_PhotoWithAnimalDetected_UploadsProceedsNormally()
    {
        _validator.ValidateAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new AnimalPhotoValidationResult(
                IsAnimalDetected: true, Confidence: 0.92f, DetectedTags: ["dog", "animal"], ServiceAvailable: true));

        var cmd = CommandWithPhoto() with { PetId = _pet.Id };
        var result = await BuildHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _blobService.Received(1).UploadAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(),
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PhotoWithNoAnimalDetected_ReturnsFailureAndDoesNotUpload()
    {
        _validator.ValidateAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new AnimalPhotoValidationResult(
                IsAnimalDetected: false, Confidence: 0f, DetectedTags: ["car", "road"], ServiceAvailable: true));

        var cmd = CommandWithPhoto() with { PetId = _pet.Id };
        var result = await BuildHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainMatch("*mascota*");

        // Photo must NOT be uploaded when validation fails
        await _blobService.DidNotReceive().UploadAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(),
            Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _sightingRepo.DidNotReceive().AddAsync(Arg.Any<Domain.Sightings.Sighting>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_VisionServiceUnavailable_FailOpenAndAllowsUpload()
    {
        // When Vision API is down, ServiceAvailable = false and IsAnimalDetected = true (fail-open)
        _validator.ValidateAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AnimalPhotoValidationResult.ServiceUnavailable);

        var cmd = CommandWithPhoto() with { PetId = _pet.Id };
        var result = await BuildHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue("fail-open must never block a legitimate sighting");
        await _blobService.Received(1).UploadAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(),
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EnforcementDisabled_SkipsValidationAndAllowsUpload()
    {
        // EnforceOnSightings = false → validator should NOT be called
        var cmd = CommandWithPhoto() with { PetId = _pet.Id };
        var result = await BuildHandler(new AnimalPhotoValidationSettings { EnforceOnSightings = false })
            .Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _validator.DidNotReceive()
            .ValidateAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoPhoto_ValidatorNotCalled()
    {
        // Validator is only invoked when a photo is present
        var cmdNoPhoto = new ReportSightingCommand(
            _pet.Id, 9.9281, -84.0907, "saw the dog", null, null, DateTimeOffset.UtcNow);

        await BuildHandler().Handle(cmdNoPhoto, CancellationToken.None);

        await _validator.DidNotReceive()
            .ValidateAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
