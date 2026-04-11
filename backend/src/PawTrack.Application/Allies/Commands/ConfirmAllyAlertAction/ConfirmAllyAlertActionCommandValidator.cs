using FluentValidation;

namespace PawTrack.Application.Allies.Commands.ConfirmAllyAlertAction;

public sealed class ConfirmAllyAlertActionCommandValidator : AbstractValidator<ConfirmAllyAlertActionCommand>
{
    public ConfirmAllyAlertActionCommandValidator()
    {
        RuleFor(x => x.ActionSummary).NotEmpty().MaximumLength(280);
    }
}