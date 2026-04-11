using FluentValidation;

namespace PawTrack.Application.Allies.Commands.SubmitAllyApplication;

public sealed class SubmitAllyApplicationCommandValidator : AbstractValidator<SubmitAllyApplicationCommand>
{
    public SubmitAllyApplicationCommandValidator()
    {
        RuleFor(x => x.OrganizationName).NotEmpty().MaximumLength(120);
        RuleFor(x => x.CoverageLabel).NotEmpty().MaximumLength(120);
        RuleFor(x => x.CoverageLat).InclusiveBetween(-90, 90);
        RuleFor(x => x.CoverageLng).InclusiveBetween(-180, 180);
        RuleFor(x => x.CoverageRadiusMetres).InclusiveBetween(250, 20000);
    }
}