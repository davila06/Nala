using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Common.Settings;
using PawTrack.Application.Sightings.Commands.ReportFoundPet;
using PawTrack.Application.Sightings.Commands.ReportSighting;
using PawTrack.Application.Sightings.DTOs;
using PawTrack.Domain.Pets;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// Round-43 security regression tests.
///
/// Gap: Pet photo uploads (<c>CreatePetCommandHandler</c> / <c>UpdatePetCommandHandler</c>)
/// pass through <c>IImageProcessor.ResizeAsync()</c> before upload.  ImageSharp will
/// throw <c>UnknownImageFormatException</c> if the bytes are not a valid image,
/// which means the handler naturally rejects non-image uploads.
///
/// <c>ReportSightingCommandHandler</c> and <c>ReportFoundPetCommandHandler</c> do
/// NOT call <c>IImageProcessor</c> — they upload the raw client stream directly to
/// Azure Blob Storage:
///
///   <code>
///   // ReportSightingCommandHandler — uploads raw stream
///   var photoUrl = await blobStorageService.UploadAsync(
///       SightingPhotosContainer, blobName,
///       request.PhotoStream,        // ← raw bytes from client
///       request.PhotoContentType,   // ← only client-supplied Content-Type header validated
///       cancellationToken);
///   </code>
///
/// Both endpoints are <c>[AllowAnonymous]</c> and the target containers
/// (<c>sighting-photos</c>, <c>found-pet-photos</c>) have <c>PublicAccessType.Blob</c>.
/// An attacker can upload HTML / SVG / executables with <c>Content-Type: image/jpeg</c>
/// and the file will be stored in a publicly-accessible blob URL.
///
/// Fix:
///   Inject <c>IImageProcessor</c> into both handlers and run the photo bytes through
///   <c>ResizeAsync()</c> before calling <c>blobStorageService.UploadAsync()</c>.
///   Always upload with <c>"image/jpeg"</c> (the output format of ImageSharp's ResizeAsync).
/// </summary>
public sealed class Round43SecurityRegressionTests
{
    // ── Helpers — ReportSightingCommandHandler ────────────────────────────────

    private static ReportSightingCommandHandler BuildSightingHandler(
        ISightingRepository? sightingRepo = null,
        IPetRepository? petRepo = null,
        ILostPetRepository? lostPetRepo = null,
        IUserRepository? userRepo = null,
        IUserLocationRepository? locationRepo = null,
        INotificationRepository? notifRepo = null,
        IBlobStorageService? blob = null,
        IPiiScrubber? pii = null,
        INotificationDispatcher? dispatcher = null,
        IImageProcessor? imageProcessor = null,
        IUnitOfWork? uow = null)
    {
        sightingRepo ??= Substitute.For<ISightingRepository>();
        petRepo      ??= Substitute.For<IPetRepository>();
        lostPetRepo  ??= Substitute.For<ILostPetRepository>();
        userRepo     ??= Substitute.For<IUserRepository>();
        locationRepo ??= Substitute.For<IUserLocationRepository>();
        notifRepo    ??= Substitute.For<INotificationRepository>();
        blob         ??= Substitute.For<IBlobStorageService>();
        pii          ??= Substitute.For<IPiiScrubber>();
        dispatcher   ??= Substitute.For<INotificationDispatcher>();
        imageProcessor ??= Substitute.For<IImageProcessor>();
        uow          ??= Substitute.For<IUnitOfWork>();

        var settings = Options.Create(new ResolveCheckSettings());

        return new ReportSightingCommandHandler(
            sightingRepo, petRepo, lostPetRepo, userRepo, locationRepo,
            notifRepo, blob, imageProcessor, pii, dispatcher, settings, uow);
    }

    // ── Helpers — ReportFoundPetCommandHandler ────────────────────────────────

    private static ReportFoundPetCommandHandler BuildFoundPetHandler(
        IFoundPetRepository? foundPetRepo = null,
        ILostPetRepository? lostPetRepo = null,
        IUserRepository? userRepo = null,
        IBlobStorageService? blob = null,
        INotificationDispatcher? dispatcher = null,
        IImageProcessor? imageProcessor = null,
        IUnitOfWork? uow = null)
    {
        foundPetRepo ??= Substitute.For<IFoundPetRepository>();
        lostPetRepo  ??= Substitute.For<ILostPetRepository>();
        userRepo     ??= Substitute.For<IUserRepository>();
        blob         ??= Substitute.For<IBlobStorageService>();
        dispatcher   ??= Substitute.For<INotificationDispatcher>();
        imageProcessor ??= Substitute.For<IImageProcessor>();
        uow          ??= Substitute.For<IUnitOfWork>();

        return new ReportFoundPetCommandHandler(
            foundPetRepo, lostPetRepo, userRepo, blob, imageProcessor, dispatcher, uow);
    }

    // ── Tests — ReportSightingCommandHandler ──────────────────────────────────

    [Fact]
    public async Task ReportSightingHandler_WhenPhotoProvided_CallsImageProcessor()
    {
        // Arrange
        var imageProcessor = Substitute.For<IImageProcessor>();
        var blob = Substitute.For<IBlobStorageService>();
        var petRepo = Substitute.For<IPetRepository>();
        var pii = Substitute.For<IPiiScrubber>();
        var uow = Substitute.For<IUnitOfWork>();

        var petId = Guid.NewGuid();
        var fakePet = Domain.Pets.Pet.Create(Guid.NewGuid(), "Luna", PetSpecies.Cat, null, null);
        petRepo.GetByIdAsync(petId, Arg.Any<CancellationToken>()).Returns(fakePet);
        pii.Scrub(Arg.Any<string?>()).Returns(string.Empty);

        var fakeResized = new byte[] { 0xFF, 0xD8, 0xFF }; // minimal JPEG
        imageProcessor.ResizeAsync(Arg.Any<byte[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(fakeResized);

        blob.UploadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(),
                         Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("https://blob/sighting-photos/img.jpg");

        var photoBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // fake PNG header
        using var photoStream = new MemoryStream(photoBytes);

        var handler = BuildSightingHandler(
            petRepo: petRepo,
            blob: blob,
            pii: pii,
            imageProcessor: imageProcessor,
            uow: uow);

        var command = new ReportSightingCommand(
            petId, 9.93, -84.08, null,
            photoStream, "image/jpeg",
            DateTimeOffset.UtcNow);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert — ImageProcessor must have been called before blob upload
        await imageProcessor.Received(1)
            .ResizeAsync(Arg.Any<byte[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>());

        await blob.Received(1).UploadAsync(
            "sighting-photos",
            Arg.Any<string>(),
            Arg.Any<Stream>(),
            "image/jpeg",       // always jpeg after processing
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReportSightingHandler_WhenNoPhoto_DoesNotCallImageProcessor()
    {
        // Arrange
        var imageProcessor = Substitute.For<IImageProcessor>();
        var blob = Substitute.For<IBlobStorageService>();
        var petRepo = Substitute.For<IPetRepository>();
        var pii = Substitute.For<IPiiScrubber>();
        var uow = Substitute.For<IUnitOfWork>();

        var petId = Guid.NewGuid();
        var fakePet = Domain.Pets.Pet.Create(Guid.NewGuid(), "Max", PetSpecies.Dog, null, null);
        petRepo.GetByIdAsync(petId, Arg.Any<CancellationToken>()).Returns(fakePet);
        pii.Scrub(Arg.Any<string?>()).Returns(string.Empty);

        var handler = BuildSightingHandler(
            petRepo: petRepo, blob: blob, pii: pii, imageProcessor: imageProcessor, uow: uow);

        var command = new ReportSightingCommand(
            petId, 9.93, -84.08, null,
            null, null, // no photo
            DateTimeOffset.UtcNow);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        await imageProcessor.DidNotReceive().ResizeAsync(
            Arg.Any<byte[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        await blob.DidNotReceive().UploadAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(),
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ── Tests — ReportFoundPetCommandHandler ─────────────────────────────────

    [Fact]
    public async Task ReportFoundPetHandler_WhenPhotoProvided_CallsImageProcessor()
    {
        // Arrange
        var imageProcessor = Substitute.For<IImageProcessor>();
        var blob = Substitute.For<IBlobStorageService>();
        var foundPetRepo = Substitute.For<IFoundPetRepository>();
        var lostPetRepo = Substitute.For<ILostPetRepository>();
        var uow = Substitute.For<IUnitOfWork>();

        var fakeResized = new byte[] { 0xFF, 0xD8, 0xFF };
        imageProcessor.ResizeAsync(Arg.Any<byte[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(fakeResized);

        blob.UploadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(),
                         Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("https://blob/found-pet-photos/img.jpg");

        lostPetRepo.GetActiveLostPetsForMatchAsync(
            Arg.Any<double>(), Arg.Any<double>(), Arg.Any<double>(), Arg.Any<double>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ActiveLostPetForMatchDto>>(Array.Empty<ActiveLostPetForMatchDto>()));

        var photoBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        using var photoStream = new MemoryStream(photoBytes);

        var handler = BuildFoundPetHandler(
            foundPetRepo: foundPetRepo,
            lostPetRepo: lostPetRepo,
            blob: blob,
            imageProcessor: imageProcessor,
            uow: uow);

        var command = new ReportFoundPetCommand(
            PetSpecies.Dog, null, "marrón", "Mediano",
            9.93, -84.08,
            "Juan", "+50688881234", null,
            photoStream, "image/jpeg");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        await imageProcessor.Received(1)
            .ResizeAsync(Arg.Any<byte[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>());

        await blob.Received(1).UploadAsync(
            "found-pet-photos",
            Arg.Any<string>(),
            Arg.Any<Stream>(),
            "image/jpeg",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReportFoundPetHandler_WhenNoPhoto_DoesNotCallImageProcessor()
    {
        // Arrange
        var imageProcessor = Substitute.For<IImageProcessor>();
        var blob = Substitute.For<IBlobStorageService>();
        var foundPetRepo = Substitute.For<IFoundPetRepository>();
        var lostPetRepo = Substitute.For<ILostPetRepository>();
        var uow = Substitute.For<IUnitOfWork>();

        lostPetRepo.GetActiveLostPetsForMatchAsync(
            Arg.Any<double>(), Arg.Any<double>(), Arg.Any<double>(), Arg.Any<double>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ActiveLostPetForMatchDto>>(Array.Empty<ActiveLostPetForMatchDto>()));

        var handler = BuildFoundPetHandler(
            foundPetRepo: foundPetRepo, lostPetRepo: lostPetRepo,
            blob: blob, imageProcessor: imageProcessor, uow: uow);

        var command = new ReportFoundPetCommand(
            PetSpecies.Cat, null, "gris", "Pequeño",
            9.93, -84.08,
            "Maria", "+50688885678", null,
            null, null); // no photo

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        await imageProcessor.DidNotReceive().ResizeAsync(
            Arg.Any<byte[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        await blob.DidNotReceive().UploadAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(),
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
