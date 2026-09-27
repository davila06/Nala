using MediatR;
using PawTrack.Application.Common.Behaviors;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Clinics;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Clinics.Commands.ManageClinicSiteAccess;

public static class ClinicSiteAccessErrors
{
    public const string NotOrganizationAdministrator = "Organization owner or administrator access is required.";
    public const string TargetMustBeMember = "The target user must be an active member of this organization.";
    public const string OwnerMustRetainSite = "An organization owner must retain access to at least one site.";
}

[BypassClinicActiveSite]
public sealed record GetClinicSiteAccessQuery(Guid ClinicId, Guid RequestingUserId)
    : IRequest<Result<IReadOnlyList<ClinicSiteAccessMemberReadModel>>>;

public sealed class GetClinicSiteAccessQueryHandler(
    IClinicSiteAccessManagementRepository accessRepository)
    : IRequestHandler<GetClinicSiteAccessQuery, Result<IReadOnlyList<ClinicSiteAccessMemberReadModel>>>
{
    public async Task<Result<IReadOnlyList<ClinicSiteAccessMemberReadModel>>> Handle(
        GetClinicSiteAccessQuery request,
        CancellationToken cancellationToken)
    {
        var organizationId = await accessRepository.GetOrganizationIdForClinicAsync(request.ClinicId, cancellationToken);
        if (!organizationId.HasValue || !await CanManageAsync(organizationId.Value, request.RequestingUserId, cancellationToken))
            return Result.Failure<IReadOnlyList<ClinicSiteAccessMemberReadModel>>(ClinicSiteAccessErrors.NotOrganizationAdministrator);

        return Result.Success(await accessRepository.ListActiveMembersAsync(
            organizationId.Value, request.ClinicId, cancellationToken: cancellationToken));
    }

    private async Task<bool> CanManageAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken)
    {
        var role = await accessRepository.GetActiveRoleAsync(organizationId, userId, cancellationToken);
        return role is ClinicOrganizationRole.Owner or ClinicOrganizationRole.Administrator;
    }
}

[BypassClinicActiveSite]
public sealed record GrantClinicSiteAccessCommand(Guid ClinicId, Guid RequestingUserId, Guid TargetUserId)
    : IRequest<Result<bool>>;

public sealed class GrantClinicSiteAccessCommandHandler(
    IClinicSiteAccessManagementRepository accessRepository,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<GrantClinicSiteAccessCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(GrantClinicSiteAccessCommand request, CancellationToken cancellationToken)
    {
        var organizationId = await accessRepository.GetOrganizationIdForClinicAsync(request.ClinicId, cancellationToken);
        if (!organizationId.HasValue || !await CanManageAsync(organizationId.Value, request.RequestingUserId, cancellationToken))
            return Result.Failure<bool>(ClinicSiteAccessErrors.NotOrganizationAdministrator);

        if (!await accessRepository.IsActiveMemberAsync(organizationId.Value, request.TargetUserId, cancellationToken))
            return Result.Failure<bool>(ClinicSiteAccessErrors.TargetMustBeMember);

        if (await accessRepository.GetActiveAccessAsync(
                organizationId.Value, request.ClinicId, request.TargetUserId, cancellationToken) is not null)
            return Result.Success(false);

        var access = ClinicOrganizationSiteAccess.Grant(
            organizationId.Value, request.ClinicId, request.TargetUserId, request.RequestingUserId);
        await accessRepository.AddAsync(access, cancellationToken);
        await auditLogRepository.AddAsync(AuditLogEntry.Create(
            request.RequestingUserId,
            AuditAction.ClinicSiteAccessGranted,
            "ClinicOrganizationSiteAccess",
            access.Id.ToString(),
            $"OrganizationId={organizationId.Value};ClinicId={request.ClinicId};UserId={request.TargetUserId}"), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(true);
    }

    private async Task<bool> CanManageAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken)
    {
        var role = await accessRepository.GetActiveRoleAsync(organizationId, userId, cancellationToken);
        return role is ClinicOrganizationRole.Owner or ClinicOrganizationRole.Administrator;
    }
}

[BypassClinicActiveSite]
public sealed record RevokeClinicSiteAccessCommand(Guid ClinicId, Guid RequestingUserId, Guid TargetUserId)
    : IRequest<Result<bool>>;

public sealed class RevokeClinicSiteAccessCommandHandler(
    IClinicSiteAccessManagementRepository accessRepository,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RevokeClinicSiteAccessCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(RevokeClinicSiteAccessCommand request, CancellationToken cancellationToken)
    {
        var organizationId = await accessRepository.GetOrganizationIdForClinicAsync(request.ClinicId, cancellationToken);
        if (!organizationId.HasValue || !await CanManageAsync(organizationId.Value, request.RequestingUserId, cancellationToken))
            return Result.Failure<bool>(ClinicSiteAccessErrors.NotOrganizationAdministrator);

        var access = await accessRepository.GetActiveAccessAsync(
            organizationId.Value, request.ClinicId, request.TargetUserId, cancellationToken);
        if (access is null)
            return Result.Success(false);

        var targetRole = await accessRepository.GetActiveRoleAsync(
            organizationId.Value, request.TargetUserId, cancellationToken);
        if (targetRole == ClinicOrganizationRole.Owner
            && !await accessRepository.HasOtherActiveSiteAccessAsync(
                organizationId.Value, request.ClinicId, request.TargetUserId, cancellationToken))
            return Result.Failure<bool>(ClinicSiteAccessErrors.OwnerMustRetainSite);

        access.Revoke();
        await auditLogRepository.AddAsync(AuditLogEntry.Create(
            request.RequestingUserId,
            AuditAction.ClinicSiteAccessRevoked,
            "ClinicOrganizationSiteAccess",
            access.Id.ToString(),
            $"OrganizationId={organizationId.Value};ClinicId={request.ClinicId};UserId={request.TargetUserId}"), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(true);
    }

    private async Task<bool> CanManageAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken)
    {
        var role = await accessRepository.GetActiveRoleAsync(organizationId, userId, cancellationToken);
        return role is ClinicOrganizationRole.Owner or ClinicOrganizationRole.Administrator;
    }
}
