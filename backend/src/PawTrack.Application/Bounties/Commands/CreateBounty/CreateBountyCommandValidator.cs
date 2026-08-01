using FluentValidation;

namespace PawTrack.Application.Bounties.Commands.CreateBounty;

public sealed class CreateBountyCommandValidator : AbstractValidator<CreateBountyCommand>
{
    public CreateBountyCommandValidator()
    {
        RuleFor(x => x.LostPetEventId).NotEmpty();
        RuleFor(x => x.OwnerId).NotEmpty();
        RuleFor(x => x.Amount)
            .GreaterThan(5_000m).WithMessage("El monto mínimo de recompensa es ₡5,000.")
            .LessThanOrEqualTo(5_000_000m).WithMessage("El monto máximo de recompensa es ₡5,000,000.");
        RuleFor(x => x.CurrencyCode)
            .Length(3).WithMessage("CurrencyCode must be a 3-letter ISO 4217 code.");
    }
}
