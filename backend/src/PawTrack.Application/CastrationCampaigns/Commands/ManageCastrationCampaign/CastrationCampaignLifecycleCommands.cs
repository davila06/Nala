using MediatR;
using PawTrack.Application.CastrationCampaigns.DTOs;
using PawTrack.Application.CastrationCampaigns.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Auth;
using PawTrack.Domain.Common;

namespace PawTrack.Application.CastrationCampaigns.Commands.ManageCastrationCampaign;

public sealed record SubmitCastrationCampaignCommand(Guid CampaignId, Guid RequestingUserId)
    : IRequest<Result<CastrationCampaignDto>>;

public sealed record ApproveCastrationCampaignCommand(Guid CampaignId, Guid RequestingUserId)
    : IRequest<Result<CastrationCampaignDto>>;

public sealed record PublishCastrationCampaignCommand(Guid CampaignId, Guid RequestingUserId)
    : IRequest<Result<CastrationCampaignDto>>;

public sealed class SubmitCastrationCampaignCommandHandler(
    ICastrationCampaignRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SubmitCastrationCampaignCommand, Result<CastrationCampaignDto>>
{
    public async Task<Result<CastrationCampaignDto>> Handle(
        SubmitCastrationCampaignCommand request,
        CancellationToken cancellationToken)
    {
        var campaign = await repository.GetByIdAsync(request.CampaignId, cancellationToken);
        if (campaign is null)
            return Result.Failure<CastrationCampaignDto>("Campaign was not found.");
        if (campaign.OrganizerUserId != request.RequestingUserId)
            return Result.Failure<CastrationCampaignDto>("Only the campaign organizer can submit it.");

        try
        {
            campaign.SubmitForApproval();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<CastrationCampaignDto>(ex.Message);
        }

        repository.Update(campaign);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(CastrationCampaignDto.FromDomain(campaign));
    }
}

public sealed class ApproveCastrationCampaignCommandHandler(
    ICastrationCampaignRepository repository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ApproveCastrationCampaignCommand, Result<CastrationCampaignDto>>
{
    public async Task<Result<CastrationCampaignDto>> Handle(
        ApproveCastrationCampaignCommand request,
        CancellationToken cancellationToken)
    {
        var requester = await userRepository.GetByIdAsync(request.RequestingUserId, cancellationToken);
        if (requester is null || !requester.Role.IsAdminOrSuperAdmin())
            return Result.Failure<CastrationCampaignDto>("Only administrators can approve campaigns.");

        var campaign = await repository.GetByIdAsync(request.CampaignId, cancellationToken);
        if (campaign is null)
            return Result.Failure<CastrationCampaignDto>("Campaign was not found.");

        try
        {
            campaign.Approve(request.RequestingUserId);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<CastrationCampaignDto>(ex.Message);
        }

        repository.Update(campaign);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(CastrationCampaignDto.FromDomain(campaign));
    }
}

public sealed class PublishCastrationCampaignCommandHandler(
    ICastrationCampaignRepository repository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<PublishCastrationCampaignCommand, Result<CastrationCampaignDto>>
{
    public async Task<Result<CastrationCampaignDto>> Handle(
        PublishCastrationCampaignCommand request,
        CancellationToken cancellationToken)
    {
        var campaign = await repository.GetByIdAsync(request.CampaignId, cancellationToken);
        if (campaign is null)
            return Result.Failure<CastrationCampaignDto>("Campaign was not found.");

        if (campaign.OrganizerUserId != request.RequestingUserId)
        {
            var requester = await userRepository.GetByIdAsync(request.RequestingUserId, cancellationToken);
            if (requester is null || !requester.Role.IsAdminOrSuperAdmin())
                return Result.Failure<CastrationCampaignDto>("Only the organizer or an administrator can publish campaigns.");
        }

        try
        {
            campaign.Publish();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<CastrationCampaignDto>(ex.Message);
        }

        repository.Update(campaign);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(CastrationCampaignDto.FromDomain(campaign));
    }
}
