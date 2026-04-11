using FluentValidation;

namespace PawTrack.Application.Safety.Commands.ReportFraud;

/// <summary>
/// Validates free-text inputs on the fraud report endpoint.
/// Without this validator a single request can send megabytes in <c>Description</c>,
/// causing memory pressure before the handler even reads it.
/// </summary>
public sealed class ReportFraudCommandValidator : AbstractValidator<ReportFraudCommand>
{
    private const int MaxDescriptionLength = 1000;

    public ReportFraudCommandValidator()
    {
        // RelatedEntityId or TargetUserId must be present — otherwise the report
        // cannot be acted upon.
        RuleFor(x => x)
            .Must(x => x.RelatedEntityId.HasValue || x.TargetUserId.HasValue)
            .WithName("ReportTarget")
            .WithMessage("Either a related entity ID or a target user ID must be provided.");

        RuleFor(x => x.Description)
            .MaximumLength(MaxDescriptionLength)
            .When(x => x.Description is not null)
            .WithMessage($"Description must not exceed {MaxDescriptionLength} characters.");
    }
}
