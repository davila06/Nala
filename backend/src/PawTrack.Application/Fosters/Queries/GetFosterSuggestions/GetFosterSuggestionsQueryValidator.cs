using FluentValidation;

namespace PawTrack.Application.Fosters.Queries.GetFosterSuggestions;

public sealed class GetFosterSuggestionsQueryValidator : AbstractValidator<GetFosterSuggestionsQuery>
{
    public GetFosterSuggestionsQueryValidator()
    {
        RuleFor(x => x.FoundPetReportId)
            .NotEmpty()
            .WithMessage("Found pet report ID must not be empty.");
    }
}
