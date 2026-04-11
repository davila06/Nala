using FluentValidation;

namespace PawTrack.Application.Notifications.Queries.GetNotificationPreferences;

public sealed class GetNotificationPreferencesQueryValidator : AbstractValidator<GetNotificationPreferencesQuery>
{
    public GetNotificationPreferencesQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID must not be empty.");
    }
}
