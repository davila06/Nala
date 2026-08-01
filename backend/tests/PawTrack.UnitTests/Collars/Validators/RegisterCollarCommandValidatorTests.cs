using FluentValidation.TestHelper;
using PawTrack.Application.Collars.Commands.RegisterCollar;
using PawTrack.Domain.Collars;

namespace PawTrack.UnitTests.Collars.Validators;

public sealed class RegisterCollarCommandValidatorTests
{
    private readonly RegisterCollarCommandValidator _sut = new();

    [Fact]
    public void PetId_Empty_ShouldFail()
    {
        var cmd = new RegisterCollarCommand(Guid.Empty, Guid.NewGuid(), CollarProvider.Tractive, null);
        _sut.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.PetId);
    }

    [Fact]
    public void OwnerId_Empty_ShouldFail()
    {
        var cmd = new RegisterCollarCommand(Guid.NewGuid(), Guid.Empty, CollarProvider.Generic, null);
        _sut.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.OwnerId);
    }

    [Fact]
    public void ExternalDeviceId_TooLong_ShouldFail()
    {
        var cmd = new RegisterCollarCommand(Guid.NewGuid(), Guid.NewGuid(), CollarProvider.Generic, new string('x', 101));
        _sut.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.ExternalDeviceId);
    }

    [Fact]
    public void ValidCommand_NullDeviceId_ShouldPass()
    {
        var cmd = new RegisterCollarCommand(Guid.NewGuid(), Guid.NewGuid(), CollarProvider.Tractive, null);
        _sut.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }
}
