using FluentValidation;

namespace PawTrack.Application.LostPets.Queries.GetRecoveryRates;

/// <summary>
/// Validates the filter parameters of <see cref="GetRecoveryRatesQuery"/>.
/// All three parameters are optional, but when supplied they must be bounded
/// strings to prevent telemetry flooding and unbounded memory allocation.
/// </summary>
public sealed class GetRecoveryRatesQueryValidator : AbstractValidator<GetRecoveryRatesQuery>
{
    public GetRecoveryRatesQueryValidator()
    {
        RuleFor(x => x.Species)
            .MaximumLength(50)
            .When(x => x.Species is not null)
            .WithMessage("Species filter must not exceed 50 characters.");

        RuleFor(x => x.Breed)
            .MaximumLength(100)
            .When(x => x.Breed is not null)
            .WithMessage("Breed filter must not exceed 100 characters.");

        RuleFor(x => x.Canton)
            .MaximumLength(100)
            .When(x => x.Canton is not null)
            .WithMessage("Canton filter must not exceed 100 characters.");
    }
}
