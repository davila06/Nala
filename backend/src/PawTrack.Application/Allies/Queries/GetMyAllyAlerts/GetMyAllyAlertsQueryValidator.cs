using FluentValidation;

namespace PawTrack.Application.Allies.Queries.GetMyAllyAlerts;

public sealed class GetMyAllyAlertsQueryValidator : AbstractValidator<GetMyAllyAlertsQuery>
{
    public GetMyAllyAlertsQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID must not be empty.");
    }
}
