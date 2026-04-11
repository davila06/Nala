using FluentValidation;

namespace PawTrack.Application.Allies.Queries.GetMyAllyProfile;

public sealed class GetMyAllyProfileQueryValidator : AbstractValidator<GetMyAllyProfileQuery>
{
    public GetMyAllyProfileQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID must not be empty.");
    }
}
