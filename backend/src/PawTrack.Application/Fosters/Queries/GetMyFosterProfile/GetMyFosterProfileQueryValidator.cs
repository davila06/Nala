using FluentValidation;

namespace PawTrack.Application.Fosters.Queries.GetMyFosterProfile;

public sealed class GetMyFosterProfileQueryValidator : AbstractValidator<GetMyFosterProfileQuery>
{
    public GetMyFosterProfileQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID must not be empty.");
    }
}
