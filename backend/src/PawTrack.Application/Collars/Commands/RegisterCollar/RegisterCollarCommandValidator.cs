using FluentValidation;

namespace PawTrack.Application.Collars.Commands.RegisterCollar;

public sealed class RegisterCollarCommandValidator : AbstractValidator<RegisterCollarCommand>
{
    public RegisterCollarCommandValidator()
    {
        RuleFor(x => x.PetId).NotEmpty();
        RuleFor(x => x.OwnerId).NotEmpty();
        RuleFor(x => x.ExternalDeviceId)
            .MaximumLength(100)
            .When(x => x.ExternalDeviceId is not null);
    }
}
