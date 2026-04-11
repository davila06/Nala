using FluentValidation;

namespace PawTrack.Application.Pets.Queries.GetPetScanHistory;

public sealed class GetPetScanHistoryQueryValidator : AbstractValidator<GetPetScanHistoryQuery>
{
    public GetPetScanHistoryQueryValidator()
    {
        RuleFor(x => x.PetId)
            .NotEmpty()
            .WithMessage("Pet ID must not be empty.");

        RuleFor(x => x.RequestingUserId)
            .NotEmpty()
            .WithMessage("Requesting user ID must not be empty.");
    }
}
