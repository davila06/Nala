using FluentValidation;

namespace PawTrack.Application.Incentives.Queries.GetLeaderboard;

public sealed class GetLeaderboardQueryValidator : AbstractValidator<GetLeaderboardQuery>
{
    public GetLeaderboardQueryValidator()
    {
        RuleFor(x => x.Take)
            .InclusiveBetween(1, 50)
            .WithMessage("Take must be between 1 and 50.");
    }
}
