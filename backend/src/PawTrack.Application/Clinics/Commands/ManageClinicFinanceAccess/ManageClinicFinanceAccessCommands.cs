using MediatR;
using PawTrack.Application.Clinics.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Clinics;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Clinics.Commands.ManageClinicFinanceAccess;

public sealed record ClinicFinanceMembershipDto(Guid Id, Guid UserId, string Email, string Role, bool IsRevoked, DateTimeOffset GrantedAt);
public sealed record GetMyClinicFinanceWorkspacesQuery(Guid UserId) : IRequest<IReadOnlyList<ClinicFinanceWorkspaceDto>>;

public sealed class GetMyClinicFinanceWorkspacesQueryHandler(IClinicFinanceAccessRepository access)
    : IRequestHandler<GetMyClinicFinanceWorkspacesQuery, IReadOnlyList<ClinicFinanceWorkspaceDto>>
{
    public Task<IReadOnlyList<ClinicFinanceWorkspaceDto>> Handle(GetMyClinicFinanceWorkspacesQuery request, CancellationToken ct) =>
        access.ListWorkspacesAsync(request.UserId, ct);
}

public sealed record GrantClinicFinanceMembershipCommand(Guid ClinicId, Guid OwnerUserId, string Email, ClinicFinanceRole Role) : IRequest<Result<Guid>>;

public sealed class GrantClinicFinanceMembershipCommandHandler(
    IClinicRepository clinics, IUserRepository users, IClinicFinanceAccessRepository access,
    IAuditLogRepository audit, IUnitOfWork unitOfWork)
    : IRequestHandler<GrantClinicFinanceMembershipCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(GrantClinicFinanceMembershipCommand request, CancellationToken ct)
    {
        var clinic = await clinics.GetByIdAsync(request.ClinicId, ct);
        var owner = await users.GetByIdAsync(request.OwnerUserId, ct);
        if (clinic is null || clinic.UserId != request.OwnerUserId || owner?.HasMfa != true)
            return Result.Failure<Guid>("Se requiere titular de clínica con MFA activo.");
        var user = await users.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), ct);
        if (user is null || !user.IsEmailVerified || user.Id == owner.Id)
            return Result.Failure<Guid>("Cuenta verificada distinta del titular requerida.");
        var membership = await access.GetAsync(request.ClinicId, user.Id, ct);
        if (membership is not null)
        {
            if (membership.IsRevoked) return Result.Failure<Guid>("Membresía revocada; requiere revisión manual.");
            membership.ChangeRole(request.Role);
        }
        else
        {
            membership = ClinicFinanceMembership.Grant(request.ClinicId, user.Id, request.Role, request.OwnerUserId);
            await access.AddAsync(membership, ct);
        }
        await audit.AddAsync(AuditLogEntry.Create(owner.Id, AuditAction.ClinicFinanceMemberGranted, "ClinicFinanceMembership", membership.Id.ToString(), request.Role.ToString()), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(membership.Id);
    }
}

public sealed record RevokeClinicFinanceMembershipCommand(Guid ClinicId, Guid OwnerUserId, Guid MemberUserId) : IRequest<Result<bool>>;

public sealed class RevokeClinicFinanceMembershipCommandHandler(
    IClinicRepository clinics, IUserRepository users, IClinicFinanceAccessRepository access,
    IAuditLogRepository audit, IUnitOfWork unitOfWork)
    : IRequestHandler<RevokeClinicFinanceMembershipCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(RevokeClinicFinanceMembershipCommand request, CancellationToken ct)
    {
        var clinic = await clinics.GetByIdAsync(request.ClinicId, ct);
        var owner = await users.GetByIdAsync(request.OwnerUserId, ct);
        if (clinic is null || clinic.UserId != request.OwnerUserId || owner?.HasMfa != true)
            return Result.Failure<bool>("Se requiere titular de clínica con MFA activo.");
        var membership = await access.GetAsync(request.ClinicId, request.MemberUserId, ct);
        if (membership is null || membership.IsRevoked) return Result.Failure<bool>("Membresía no encontrada.");
        membership.Revoke(owner.Id);
        await audit.AddAsync(AuditLogEntry.Create(owner.Id, AuditAction.ClinicFinanceMemberRevoked, "ClinicFinanceMembership", membership.Id.ToString()), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(true);
    }
}

public sealed record GetClinicFinanceMembershipsQuery(Guid ClinicId, Guid OwnerUserId) : IRequest<Result<IReadOnlyList<ClinicFinanceMembershipDto>>>;

public sealed class GetClinicFinanceMembershipsQueryHandler(IClinicRepository clinics, IClinicFinanceAccessRepository access)
    : IRequestHandler<GetClinicFinanceMembershipsQuery, Result<IReadOnlyList<ClinicFinanceMembershipDto>>>
{
    public async Task<Result<IReadOnlyList<ClinicFinanceMembershipDto>>> Handle(GetClinicFinanceMembershipsQuery request, CancellationToken ct)
    {
        var clinic = await clinics.GetByIdAsync(request.ClinicId, ct);
        if (clinic is null || clinic.UserId != request.OwnerUserId)
            return Result.Failure<IReadOnlyList<ClinicFinanceMembershipDto>>("Acceso denegado.");
        var members = await access.ListMemberDetailsAsync(request.ClinicId, ct);
        return Result.Success<IReadOnlyList<ClinicFinanceMembershipDto>>(members.Select(member =>
            new ClinicFinanceMembershipDto(member.Id, member.UserId, member.Email, member.Role, member.IsRevoked, member.GrantedAt)).ToList());
    }
}
