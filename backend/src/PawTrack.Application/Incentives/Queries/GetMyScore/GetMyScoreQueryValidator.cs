using FluentValidation;

namespace PawTrack.Application.Incentives.Queries.GetMyScore;

public sealed class GetMyScoreQueryValidator : AbstractValidator<GetMyScoreQuery>
{
    public GetMyScoreQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID must not be empty.");
    }
}
