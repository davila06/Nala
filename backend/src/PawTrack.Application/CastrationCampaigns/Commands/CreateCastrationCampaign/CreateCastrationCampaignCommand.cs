using FluentValidation;
using MediatR;
using PawTrack.Application.CastrationCampaigns.DTOs;
using PawTrack.Application.CastrationCampaigns.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Auth;
using PawTrack.Domain.CastrationCampaigns;
using PawTrack.Domain.Common;

namespace PawTrack.Application.CastrationCampaigns.Commands.CreateCastrationCampaign;

public sealed record CreateCastrationCampaignCommand(
    Guid RequestingUserId,
    Guid ExecutingClinicId,
    string Title,
    string VenueLabel,
    string Canton,
    double Latitude,
    double Longitude,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    DateTimeOffset ReservationsOpenAt,
    DateTimeOffset ReservationsCloseAt,
    int Capacity,
    decimal BasePriceCrc,
    string ConsentVersion) : IRequest<Result<CastrationCampaignDto>>;

public sealed class CreateCastrationCampaignCommandValidator : AbstractValidator<CreateCastrationCampaignCommand>
{
    public CreateCastrationCampaignCommandValidator()
    {
        RuleFor(x => x.RequestingUserId).NotEmpty();
        RuleFor(x => x.ExecutingClinicId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(160);
        RuleFor(x => x.VenueLabel).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Canton).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.EndsAt).GreaterThan(x => x.StartsAt);
        RuleFor(x => x.ReservationsCloseAt).GreaterThan(x => x.ReservationsOpenAt);
        RuleFor(x => x.ReservationsCloseAt).LessThanOrEqualTo(x => x.StartsAt);
        RuleFor(x => x.Capacity).InclusiveBetween(1, 10_000);
        RuleFor(x => x.BasePriceCrc).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ConsentVersion).NotEmpty().MaximumLength(40);
    }
}

public sealed class CreateCastrationCampaignCommandHandler(
    ICastrationCampaignRepository repository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateCastrationCampaignCommand, Result<CastrationCampaignDto>>
{
    public async Task<Result<CastrationCampaignDto>> Handle(
        CreateCastrationCampaignCommand request,
        CancellationToken cancellationToken)
    {
        var requester = await userRepository.GetByIdAsync(request.RequestingUserId, cancellationToken);
        if (requester is null || requester.Role is not (
            UserRole.Clinic or UserRole.Municipality or UserRole.Ally or UserRole.Admin or UserRole.SuperAdmin))
        {
            return Result.Failure<CastrationCampaignDto>(
                "Only clinics, municipalities, allies, or administrators can create campaigns.");
        }

        var campaign = CastrationCampaign.Create(
            request.RequestingUserId,
            request.ExecutingClinicId,
            request.Title,
            request.VenueLabel,
            request.Canton,
            request.Latitude,
            request.Longitude,
            request.StartsAt,
            request.EndsAt,
            request.ReservationsOpenAt,
            request.ReservationsCloseAt,
            request.Capacity,
            request.BasePriceCrc,
            request.ConsentVersion);

        await repository.AddAsync(campaign, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(CastrationCampaignDto.FromDomain(campaign));
    }
}
