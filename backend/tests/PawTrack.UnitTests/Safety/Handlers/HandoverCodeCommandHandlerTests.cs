using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Safety.Commands.GenerateHandoverCode;
using PawTrack.Application.Safety.Commands.VerifyHandoverCode;
using PawTrack.Domain.LostPets;
using PawTrack.Domain.Safety;

namespace PawTrack.UnitTests.Safety.Handlers;

public sealed class HandoverCodeCommandHandlerTests
{
    private readonly IHandoverCodeRepository _handoverCodeRepository = Substitute.For<IHandoverCodeRepository>();
    private readonly ILostPetRepository _lostPetRepository = Substitute.For<ILostPetRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GenerateHandoverCodeCommandHandler _generateHandler;
    private readonly VerifyHandoverCodeCommandHandler _verifyHandler;

    public HandoverCodeCommandHandlerTests()
    {
        _generateHandler = new GenerateHandoverCodeCommandHandler(
            _handoverCodeRepository,
            _lostPetRepository,
            _unitOfWork);

        _verifyHandler = new VerifyHandoverCodeCommandHandler(
            _handoverCodeRepository,
            _lostPetRepository,
            _unitOfWork);
    }

    [Fact]
    public async Task GenerateCode_WhenRequesterIsOwner_ReturnsFresh4DigitCode()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var lostEvent = CreateLostPetEvent(ownerId);

        _lostPetRepository.GetByIdAsync(lostEvent.Id, Arg.Any<CancellationToken>())
            .Returns(lostEvent);

        _handoverCodeRepository.GetActiveByLostPetEventIdAsync(lostEvent.Id, Arg.Any<CancellationToken>())
            .Returns((HandoverCode?)null);

        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(1);

        var command = new GenerateHandoverCodeCommand(lostEvent.Id, ownerId);

        // Act
        var result = await _generateHandler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrWhiteSpace();
        result.Value.Should().MatchRegex("^\\d{4}$");

        await _handoverCodeRepository.Received(1)
            .AddAsync(Arg.Any<HandoverCode>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateCode_WhenPreviousCodeExists_SupersedesOldCode()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var lostEvent = CreateLostPetEvent(ownerId);
        var previousCode = HandoverCode.Generate(lostEvent.Id);

        _lostPetRepository.GetByIdAsync(lostEvent.Id, Arg.Any<CancellationToken>())
            .Returns(lostEvent);

        _handoverCodeRepository.GetActiveByLostPetEventIdAsync(lostEvent.Id, Arg.Any<CancellationToken>())
            .Returns(previousCode);

        var command = new GenerateHandoverCodeCommand(lostEvent.Id, ownerId);

        // Act
        var result = await _generateHandler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        previousCode.IsUsed.Should().BeTrue();
        previousCode.VerifiedByUserId.Should().Be(ownerId);

        _handoverCodeRepository.Received(1).Update(previousCode);
        await _handoverCodeRepository.Received(1)
            .AddAsync(Arg.Any<HandoverCode>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task VerifyCode_WhenCodeMatchesAndNotExpired_ReturnsTrueAndConsumesCode()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var rescuerId = Guid.NewGuid();
        var lostEvent = CreateLostPetEvent(ownerId);
        var activeCode = HandoverCode.Generate(lostEvent.Id);

        _lostPetRepository.GetByIdAsync(lostEvent.Id, Arg.Any<CancellationToken>())
            .Returns(lostEvent);

        _handoverCodeRepository.GetActiveByLostPetEventIdAsync(lostEvent.Id, Arg.Any<CancellationToken>())
            .Returns(activeCode);

        var command = new VerifyHandoverCodeCommand(lostEvent.Id, rescuerId, activeCode.Code);

        // Act
        var result = await _verifyHandler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        activeCode.IsUsed.Should().BeTrue();
        activeCode.VerifiedByUserId.Should().Be(rescuerId);

        _handoverCodeRepository.Received(1).Update(activeCode);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task VerifyCode_WhenVerifierIsOwner_ReturnsFailure()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var lostEvent = CreateLostPetEvent(ownerId);

        _lostPetRepository.GetByIdAsync(lostEvent.Id, Arg.Any<CancellationToken>())
            .Returns(lostEvent);

        var command = new VerifyHandoverCodeCommand(lostEvent.Id, ownerId, "1234");

        // Act
        var result = await _verifyHandler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("El dueño no puede verificar su propio código.");
    }

    [Fact]
    public async Task VerifyCode_WhenCodeIsInvalid_ReturnsFalse()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var rescuerId = Guid.NewGuid();
        var lostEvent = CreateLostPetEvent(ownerId);
        var activeCode = HandoverCode.Generate(lostEvent.Id);

        _lostPetRepository.GetByIdAsync(lostEvent.Id, Arg.Any<CancellationToken>())
            .Returns(lostEvent);

        _handoverCodeRepository.GetActiveByLostPetEventIdAsync(lostEvent.Id, Arg.Any<CancellationToken>())
            .Returns(activeCode);

        var command = new VerifyHandoverCodeCommand(lostEvent.Id, rescuerId, "9999");

        // Act
        var result = await _verifyHandler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
        activeCode.IsUsed.Should().BeFalse();

        _handoverCodeRepository.DidNotReceive().Update(Arg.Any<HandoverCode>());
    }

    private static LostPetEvent CreateLostPetEvent(Guid ownerId)
    {
        return LostPetEvent.Create(
            petId: Guid.NewGuid(),
            ownerId: ownerId,
            description: "Lost near park",
            lastSeenLat: 9.93,
            lastSeenLng: -84.08,
            lastSeenAt: DateTimeOffset.UtcNow.AddHours(-2));
    }
}
