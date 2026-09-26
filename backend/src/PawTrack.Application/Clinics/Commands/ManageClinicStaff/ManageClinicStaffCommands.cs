using MediatR;
using PawTrack.Application.Certificates.Interfaces;
using PawTrack.Application.Clinics.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Clinics;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Clinics.Commands.ManageClinicStaff;

public sealed record GrantClinicStaffMembershipCommand(Guid ClinicId, Guid OwnerUserId, string Email, ClinicStaffRole Role, Guid? VeterinarianId) : IRequest<Result<Guid>>;

public sealed class GrantClinicStaffMembershipCommandHandler(
    IClinicRepository clinics, IUserRepository users, IClinicVeterinarianRepository veterinarians,
    IClinicStaffAccessRepository staff, IAuditLogRepository audit, IUnitOfWork unitOfWork)
    : IRequestHandler<GrantClinicStaffMembershipCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(GrantClinicStaffMembershipCommand request, CancellationToken ct)
    {
        var clinic = await clinics.GetByIdAsync(request.ClinicId, ct);
        var owner = await users.GetByIdAsync(request.OwnerUserId, ct);
        if (clinic is null || clinic.UserId != request.OwnerUserId || owner?.HasMfa != true)
            return Result.Failure<Guid>("Titular con MFA requerido.");
        if (string.IsNullOrWhiteSpace(request.Email) || !Enum.IsDefined(request.Role))
            return Result.Failure<Guid>("Cuenta o rol inválido.");
        var user = await users.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), ct);
        if (user is null || !user.IsEmailVerified || user.Id == owner.Id)
            return Result.Failure<Guid>("Cuenta verificada distinta del titular requerida.");
        if (request.Role == ClinicStaffRole.Veterinarian)
        {
            var veterinarian = request.VeterinarianId.HasValue
                ? await veterinarians.GetByIdAsync(request.VeterinarianId.Value, ct) : null;
            if (veterinarian is null || veterinarian.ClinicId != clinic.Id || !veterinarian.IsActive)
                return Result.Failure<Guid>("Veterinario activo de esta clínica requerido.");
        }
        else if (request.VeterinarianId.HasValue)
            return Result.Failure<Guid>("Sólo el rol veterinario puede vincular un registro profesional.");

        var member = await staff.GetAsync(clinic.Id, user.Id, ct);
        if (member is not null)
        {
            if (member.IsRevoked) return Result.Failure<Guid>("Membresía revocada; requiere revisión manual.");
            member.ChangeRole(request.Role, request.VeterinarianId);
        }
        else
        {
            member = ClinicStaffMembership.Grant(clinic.Id, user.Id, request.Role, owner.Id, request.VeterinarianId);
            await staff.AddAsync(member, ct);
        }
        await audit.AddAsync(AuditLogEntry.Create(owner.Id, AuditAction.ClinicStaffGranted,
            "ClinicStaffMembership", member.Id.ToString(), request.Role.ToString()), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(member.Id);
    }
}

public sealed record RevokeClinicStaffMembershipCommand(Guid ClinicId, Guid OwnerUserId, Guid MemberUserId) : IRequest<Result<bool>>;

public sealed class RevokeClinicStaffMembershipCommandHandler(
    IClinicRepository clinics, IUserRepository users, IClinicStaffAccessRepository staff,
    IAuditLogRepository audit, IUnitOfWork unitOfWork)
    : IRequestHandler<RevokeClinicStaffMembershipCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(RevokeClinicStaffMembershipCommand request, CancellationToken ct)
    {
        var clinic = await clinics.GetByIdAsync(request.ClinicId, ct);
        var owner = await users.GetByIdAsync(request.OwnerUserId, ct);
        if (clinic is null || clinic.UserId != owner?.Id || !owner.HasMfa)
            return Result.Failure<bool>("Titular con MFA requerido.");
        var member = await staff.GetAsync(clinic.Id, request.MemberUserId, ct);
        if (member is null || member.IsRevoked) return Result.Failure<bool>("Membresía no encontrada.");
        member.Revoke(owner.Id);
        await audit.AddAsync(AuditLogEntry.Create(owner.Id, AuditAction.ClinicStaffRevoked,
            "ClinicStaffMembership", member.Id.ToString()), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(true);
    }
}

public sealed record GetClinicStaffMembersQuery(Guid ClinicId, Guid OwnerUserId) : IRequest<Result<IReadOnlyList<ClinicStaffMemberReadModel>>>;
public sealed class GetClinicStaffMembersQueryHandler(IClinicRepository clinics, IClinicStaffAccessRepository staff)
    : IRequestHandler<GetClinicStaffMembersQuery, Result<IReadOnlyList<ClinicStaffMemberReadModel>>>
{
    public async Task<Result<IReadOnlyList<ClinicStaffMemberReadModel>>> Handle(GetClinicStaffMembersQuery request, CancellationToken ct)
    {
        var clinic = await clinics.GetByIdAsync(request.ClinicId, ct);
        return clinic?.UserId == request.OwnerUserId
            ? Result.Success(await staff.ListMembersAsync(clinic.Id, ct))
            : Result.Failure<IReadOnlyList<ClinicStaffMemberReadModel>>("Acceso denegado.");
    }
}

public sealed record GetMyClinicStaffWorkspacesQuery(Guid UserId) : IRequest<IReadOnlyList<ClinicStaffWorkspaceReadModel>>;
public sealed class GetMyClinicStaffWorkspacesQueryHandler(IClinicStaffAccessRepository staff)
    : IRequestHandler<GetMyClinicStaffWorkspacesQuery, IReadOnlyList<ClinicStaffWorkspaceReadModel>>
{
    public Task<IReadOnlyList<ClinicStaffWorkspaceReadModel>> Handle(GetMyClinicStaffWorkspacesQuery request, CancellationToken ct) =>
        staff.ListWorkspacesAsync(request.UserId, ct);
}

public sealed record GetClinicTaskAssigneesQuery(Guid ClinicId, Guid RequestingUserId)
    : IRequest<Result<IReadOnlyList<ClinicTaskAssigneeReadModel>>>;

public sealed class GetClinicTaskAssigneesQueryHandler(
    IClinicRepository clinics,
    IClinicFinanceAccessRepository financeAccess,
    IClinicStaffAccessRepository staff)
    : IRequestHandler<GetClinicTaskAssigneesQuery, Result<IReadOnlyList<ClinicTaskAssigneeReadModel>>>
{
    public async Task<Result<IReadOnlyList<ClinicTaskAssigneeReadModel>>> Handle(
        GetClinicTaskAssigneesQuery request,
        CancellationToken cancellationToken)
    {
        var clinic = await clinics.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null || clinic.Status != ClinicStatus.Active)
            return Result.Failure<IReadOnlyList<ClinicTaskAssigneeReadModel>>("Clínica no disponible.");
        var isOwner = clinic.UserId == request.RequestingUserId;
        var isFinanceManager = await financeAccess.HasPermissionAsync(
            request.ClinicId, request.RequestingUserId, ClinicFinancePermission.ManageStaff, cancellationToken);
        if (!isOwner && !isFinanceManager)
            return Result.Failure<IReadOnlyList<ClinicTaskAssigneeReadModel>>("Acceso denegado.");
        return Result.Success(await staff.ListTaskAssigneesAsync(request.ClinicId, cancellationToken));
    }
}
