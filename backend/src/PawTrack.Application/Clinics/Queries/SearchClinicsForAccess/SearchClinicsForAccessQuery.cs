using FluentValidation;
using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Clinics.Queries.SearchClinicsForAccess;

public sealed record ClinicAccessSearchResultDto(Guid Id, string Name, string LicenseNumber);

public sealed record SearchClinicsForAccessQuery(string Search)
    : IRequest<Result<IReadOnlyList<ClinicAccessSearchResultDto>>>;

public sealed class SearchClinicsForAccessQueryValidator : AbstractValidator<SearchClinicsForAccessQuery>
{
    public SearchClinicsForAccessQueryValidator()
    {
        RuleFor(x => x.Search).NotEmpty().MinimumLength(2).MaximumLength(100);
    }
}

public sealed class SearchClinicsForAccessQueryHandler(IClinicRepository clinicRepository)
    : IRequestHandler<SearchClinicsForAccessQuery, Result<IReadOnlyList<ClinicAccessSearchResultDto>>>
{
    private const int MaxResults = 10;

    public async Task<Result<IReadOnlyList<ClinicAccessSearchResultDto>>> Handle(
        SearchClinicsForAccessQuery request, CancellationToken cancellationToken)
    {
        var clinics = await clinicRepository.SearchActiveByNameOrLicenseAsync(request.Search, MaxResults, cancellationToken);
        IReadOnlyList<ClinicAccessSearchResultDto> result = clinics
            .Select(c => new ClinicAccessSearchResultDto(c.Id, c.Name, c.LicenseNumber))
            .ToList()
            .AsReadOnly();
        return Result.Success(result);
    }
}
