using FluentValidation;

namespace PawTrack.Application.Clinics.Queries.GetMyClinic;

public sealed class GetMyClinicQueryValidator : AbstractValidator<GetMyClinicQuery>
{
    public GetMyClinicQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID must not be empty.");
    }
}
