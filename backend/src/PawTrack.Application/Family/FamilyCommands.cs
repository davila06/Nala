using MediatR;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.Services;
using PawTrack.Domain.Common;
using PawTrack.Domain.Family;

namespace PawTrack.Application.Family;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public sealed record FamilyMemberDto(Guid UserId, string Name, string Email, string Role, DateTimeOffset JoinedAt);
public sealed record FamilyAccountDto(Guid Id, string Name, IReadOnlyList<FamilyMemberDto> Members);
public sealed record FamilyInvitationDto(Guid Token, string InvitedEmail, DateTimeOffset ExpiresAt);

// ── Create account ────────────────────────────────────────────────────────────

public sealed record CreateFamilyAccountCommand(Guid OwnerId, string Name) : IRequest<Result<FamilyAccountDto>>;

public sealed class CreateFamilyAccountCommandHandler(
    IFamilyRepository familyRepository,
    ISubscriptionService subscriptionService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateFamilyAccountCommand, Result<FamilyAccountDto>>
{
    public async Task<Result<FamilyAccountDto>> Handle(
        CreateFamilyAccountCommand request, CancellationToken ct)
    {
        var isFamilia = await subscriptionService.IsFamiliaAsync(request.OwnerId, ct);
        if (!isFamilia)
            return Result.Failure<FamilyAccountDto>("La cuenta familiar requiere el plan Familia.");

        var existing = await familyRepository.GetByOwnerAsync(request.OwnerId, ct);
        if (existing is not null)
            return Result.Failure<FamilyAccountDto>("Ya tienes una cuenta familiar creada.");

        var account = FamilyAccount.Create(request.OwnerId, request.Name);
        var ownerMembership = FamilyMembership.CreateOwner(account.Id, request.OwnerId);

        await familyRepository.AddAccountAsync(account, ct);
        await familyRepository.AddMembershipAsync(ownerMembership, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(new FamilyAccountDto(account.Id, account.Name, []));
    }
}

// ── Invite member ─────────────────────────────────────────────────────────────

public sealed record InviteFamilyMemberCommand(Guid OwnerId, string InvitedEmail) : IRequest<Result<FamilyInvitationDto>>;

public sealed class InviteFamilyMemberCommandHandler(
    IFamilyRepository familyRepository,
    IEmailSender emailSender,
    IUnitOfWork unitOfWork,
    ILogger<InviteFamilyMemberCommandHandler> logger)
    : IRequestHandler<InviteFamilyMemberCommand, Result<FamilyInvitationDto>>
{
    private const int MaxMembers = 5;

    public async Task<Result<FamilyInvitationDto>> Handle(
        InviteFamilyMemberCommand request, CancellationToken ct)
    {
        var account = await familyRepository.GetByOwnerAsync(request.OwnerId, ct);
        if (account is null)
            return Result.Failure<FamilyInvitationDto>("No tienes una cuenta familiar. Crea una primero.");

        if (account.OwnerId != request.OwnerId)
            return Result.Failure<FamilyInvitationDto>("Solo el dueño puede invitar miembros.");

        var count = await familyRepository.CountActiveMembersAsync(account.Id, ct);
        if (count >= MaxMembers)
            return Result.Failure<FamilyInvitationDto>($"La cuenta familiar ya tiene el máximo de {MaxMembers} miembros.");

        // Limit open invitations to prevent spam
        const int MaxPendingInvitations = 3;
        var pending = await familyRepository.CountPendingInvitationsAsync(account.Id, ct);
        if (pending >= MaxPendingInvitations)
            return Result.Failure<FamilyInvitationDto>($"Ya tienes {MaxPendingInvitations} invitaciones pendientes. Espera a que sean aceptadas o expiren.");

        var invitation = FamilyInvitation.Create(account.Id, request.InvitedEmail);
        await familyRepository.AddInvitationAsync(invitation, ct);
        await unitOfWork.SaveChangesAsync(ct);

        // Fire-and-forget — log failures; don't roll back the persisted invitation
        _ = emailSender.SendFamilyInvitationAsync(
            request.InvitedEmail,
            invitation.Token.ToString(),
            CancellationToken.None)
            .ContinueWith(t => logger.LogWarning(t.Exception,
                "Family invitation email failed for {Email}", request.InvitedEmail),
                CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);

        return Result.Success(new FamilyInvitationDto(
            invitation.Token, invitation.InvitedEmail, invitation.ExpiresAt));
    }
}

// ── Accept invitation ─────────────────────────────────────────────────────────

public sealed record AcceptFamilyInvitationCommand(Guid AcceptingUserId, Guid Token) : IRequest<Result<bool>>;

public sealed class AcceptFamilyInvitationCommandHandler(
    IFamilyRepository familyRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AcceptFamilyInvitationCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        AcceptFamilyInvitationCommand request, CancellationToken ct)
    {
        var invitation = await familyRepository.GetInvitationByTokenAsync(request.Token, ct);
        if (invitation is null || invitation.IsExpired || invitation.IsAccepted)
            return Result.Failure<bool>("La invitación no es válida o ya fue usada.");

        // Verify that the accepting user's email matches the invited email
        var user = await userRepository.GetByIdAsync(request.AcceptingUserId, ct);
        if (user is null)
            return Result.Failure<bool>("Usuario no encontrado.");

        if (!string.Equals(user.Email, invitation.InvitedEmail, StringComparison.OrdinalIgnoreCase))
            return Result.Failure<bool>("Esta invitación fue enviada a otra dirección de correo.");

        invitation.Accept();
        familyRepository.UpdateInvitation(invitation);

        var membership = FamilyMembership.CreateMember(invitation.FamilyAccountId, request.AcceptingUserId);
        await familyRepository.AddMembershipAsync(membership, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(true);
    }
}

// ── Remove member ─────────────────────────────────────────────────────────────

public sealed record RemoveFamilyMemberCommand(Guid OwnerId, Guid MemberUserId) : IRequest<Result<bool>>;

public sealed class RemoveFamilyMemberCommandHandler(
    IFamilyRepository familyRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RemoveFamilyMemberCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        RemoveFamilyMemberCommand request, CancellationToken ct)
    {
        var account = await familyRepository.GetByOwnerAsync(request.OwnerId, ct);
        if (account is null || account.OwnerId != request.OwnerId)
            return Result.Failure<bool>("Acceso denegado.");

        var memberships = await familyRepository.GetActiveMembershipsAsync(account.Id, ct);
        var target = memberships.FirstOrDefault(m => m.UserId == request.MemberUserId);
        if (target is null)
            return Result.Failure<bool>("Miembro no encontrado.");

        if (target.Role == FamilyMemberRole.Owner)
            return Result.Failure<bool>("No puedes eliminar al dueño de la cuenta.");

        target.Deactivate();
        familyRepository.UpdateMembership(target);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(true);
    }
}

// ── Get family members ────────────────────────────────────────────────────────

public sealed record GetFamilyMembersQuery(Guid RequestingUserId) : IRequest<Result<FamilyAccountDto?>>;

public sealed class GetFamilyMembersQueryHandler(
    IFamilyRepository familyRepository,
    IUserRepository userRepository)
    : IRequestHandler<GetFamilyMembersQuery, Result<FamilyAccountDto?>>
{
    public async Task<Result<FamilyAccountDto?>> Handle(
        GetFamilyMembersQuery request, CancellationToken ct)
    {
        var account = await familyRepository.GetByMemberAsync(request.RequestingUserId, ct)
                      ?? await familyRepository.GetByOwnerAsync(request.RequestingUserId, ct);

        if (account is null) return Result.Success<FamilyAccountDto?>(null);

        var memberships = await familyRepository.GetActiveMembershipsAsync(account.Id, ct);
        var userIds = memberships.Select(m => m.UserId).ToList();
        var users = await userRepository.GetByIdsAsync(userIds, ct);
        var userMap = users.ToDictionary(u => u.Id);

        var memberDtos = memberships
            .Where(m => userMap.ContainsKey(m.UserId))
            .Select(m => new FamilyMemberDto(
                m.UserId,
                userMap[m.UserId].Name,
                userMap[m.UserId].Email,
                m.Role.ToString(),
                m.JoinedAt))
            .ToList();

        return Result.Success<FamilyAccountDto?>(new FamilyAccountDto(account.Id, account.Name, memberDtos));
    }
}
