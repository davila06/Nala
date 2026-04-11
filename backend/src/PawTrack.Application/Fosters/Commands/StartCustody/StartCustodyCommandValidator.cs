using FluentValidation;

namespace PawTrack.Application.Fosters.Commands.StartCustody;

/// <summary>
/// Validates the start-custody request.
/// The <c>Note</c> field is a free-text column — without an upper bound a single
/// request can insert thousands of characters into the database.
/// <c>ExpectedDays</c> must be sane to prevent date arithmetic overflow and UI confusion.
/// </summary>
public sealed class StartCustodyCommandValidator : AbstractValidator<StartCustodyCommand>
{
    private const int MaxNoteLength = 500;
    private const int MaxExpectedDays = 365;

    public StartCustodyCommandValidator()
    {
        RuleFor(x => x.ExpectedDays)
            .InclusiveBetween(1, MaxExpectedDays)
            .WithMessage($"Expected days must be between 1 and {MaxExpectedDays}.");

        RuleFor(x => x.Note)
            .MaximumLength(MaxNoteLength)
            .When(x => x.Note is not null)
            .WithMessage($"Note must not exceed {MaxNoteLength} characters.");
    }
}
