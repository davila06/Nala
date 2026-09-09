using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Municipalities.Commands.UpdateCaptureStatus;
using PawTrack.Application.Municipalities.DTOs;
using PawTrack.Application.Municipalities.Interfaces;
using PawTrack.Domain.Municipalities;

namespace PawTrack.UnitTests.Municipalities;

public sealed class UpdateCaptureStatusAuthorizationTests
{
    [Fact]
    public async Task Handle_DifferentUser_ReturnsAccessDeniedWithoutMutation()
    {
        var repository = Substitute.For<ICapturedAnimalRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var ownerId = Guid.NewGuid();
        var attackerId = Guid.NewGuid();
        var animal = CapturedAnimal.Record(ownerId, "San Jose", "Dog", "Black");
        repository.GetByIdAsync(animal.Id, Arg.Any<CancellationToken>()).Returns(animal);
        var handler = new UpdateCaptureStatusCommandHandler(repository, unitOfWork);

        var result = await handler.Handle(
            new UpdateCaptureStatusCommand(attackerId, animal.Id, CapturedAnimalStatus.OwnerFound),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Access denied.");
        repository.DidNotReceive().Update(Arg.Any<CapturedAnimal>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
