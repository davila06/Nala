using FluentValidation;
using MediatR;
using PawTrack.Application.Certificates.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Pets;

namespace PawTrack.Application.Pets.SanitaryIdentity;

public sealed record PetSanitaryIdentityDto(
    Guid PetId,
    string Sex,
    string? Color,
    string? DistinctiveMarks,
    string SterilizedStatus,
    DateOnly? SterilizedAt,
    string? ResidenceCanton,
    string? MicrochipId,
    string MicrochipVerificationStatus,
    DateTimeOffset? MicrochipVerifiedAt,
    Guid? MicrochipVerifiedByClinicId,
    string? MicrochipVerificationNotes)
{
    public static PetSanitaryIdentityDto FromDomain(Pet pet) => new(
        pet.Id,
        pet.Sex.ToString(),
        pet.Color,
        pet.DistinctiveMarks,
        pet.SterilizedStatus.ToString(),
        pet.SterilizedAt,
        pet.ResidenceCanton,
        pet.MicrochipId,
        pet.MicrochipVerificationStatus.ToString(),
        pet.MicrochipVerifiedAt,
        pet.MicrochipVerifiedByClinicId,
        pet.MicrochipVerificationNotes);
}

public sealed record PetSanitaryAuditDto(
    Guid Id,
    Guid PetId,
    Guid ActorUserId,
    Guid? ActorClinicId,
    string Action,
    string FieldName,
    string? PreviousValue,
    string? NewValue,
    string? Reason,
    DateTimeOffset CreatedAt)
{
    public static PetSanitaryAuditDto FromDomain(PetSanitaryIdentityAuditLog log) => new(
        log.Id,
        log.PetId,
        log.ActorUserId,
        log.ActorClinicId,
        log.Action.ToString(),
        log.FieldName,
        log.PreviousValue,
        log.NewValue,
        log.Reason,
        log.CreatedAt);
}

public sealed record UpdatePetSanitaryIdentityCommand(
    Guid PetId,
    Guid RequestingUserId,
    PetSex Sex,
    string? Color,
    string? DistinctiveMarks,
    SterilizedStatus SterilizedStatus,
    DateOnly? SterilizedAt,
    string? ResidenceCanton,
    string? MicrochipId) : IRequest<Result<PetSanitaryIdentityDto>>;

public sealed class UpdatePetSanitaryIdentityCommandValidator : AbstractValidator<UpdatePetSanitaryIdentityCommand>
{
    public UpdatePetSanitaryIdentityCommandValidator()
    {
        RuleFor(x => x.PetId).NotEmpty();
        RuleFor(x => x.RequestingUserId).NotEmpty();
        RuleFor(x => x.Color).MaximumLength(80);
        RuleFor(x => x.DistinctiveMarks).MaximumLength(300);
        RuleFor(x => x.ResidenceCanton).MaximumLength(80);
        RuleFor(x => x.MicrochipId).Matches("^[0-9]{1,15}$").When(x => !string.IsNullOrWhiteSpace(x.MicrochipId));
        RuleFor(x => x.SterilizedAt)
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .When(x => x.SterilizedAt.HasValue);
    }
}

public sealed class UpdatePetSanitaryIdentityCommandHandler(
    IPetRepository petRepository,
    IFamilyRepository familyRepository,
    IPetSanitaryIdentityAuditRepository auditRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdatePetSanitaryIdentityCommand, Result<PetSanitaryIdentityDto>>
{
    public async Task<Result<PetSanitaryIdentityDto>> Handle(UpdatePetSanitaryIdentityCommand request, CancellationToken ct)
    {
        var pet = await petRepository.GetByIdAsync(request.PetId, ct);
        if (pet is null) return Result.Failure<PetSanitaryIdentityDto>("Mascota no encontrada.");
        if (!await CanAccessPetAsync(pet.OwnerId, request.RequestingUserId, familyRepository, ct))
            return Result.Failure<PetSanitaryIdentityDto>("Acceso denegado.");

        var beforeMicrochip = pet.MicrochipId;
        var beforeStatus = pet.MicrochipVerificationStatus.ToString();
        var beforeColor = pet.Color;
        var beforeCanton = pet.ResidenceCanton;

        var updateResult = pet.UpdateSanitaryIdentity(
            request.Sex,
            request.Color,
            request.DistinctiveMarks,
            request.SterilizedStatus,
            request.SterilizedAt,
            request.ResidenceCanton);
        if (updateResult.IsFailure) return Result.Failure<PetSanitaryIdentityDto>(updateResult.Errors);

        if (request.MicrochipId is not null)
        {
            var chipResult = pet.SetMicrochipDeclared(request.MicrochipId);
            if (chipResult.IsFailure) return Result.Failure<PetSanitaryIdentityDto>(chipResult.Errors);
        }

        await auditRepository.AddAsync(PetSanitaryIdentityAuditLog.Create(
            pet.Id, request.RequestingUserId, null, PetSanitaryIdentityAuditAction.SanitaryIdentityUpdated,
            "SanitaryIdentity", null, "Updated"), ct);

        if (beforeMicrochip != pet.MicrochipId || beforeStatus != pet.MicrochipVerificationStatus.ToString())
        {
            await auditRepository.AddAsync(PetSanitaryIdentityAuditLog.Create(
                pet.Id, request.RequestingUserId, null, PetSanitaryIdentityAuditAction.MicrochipDeclared,
                "MicrochipId", beforeMicrochip, pet.MicrochipId), ct);
        }

        if (beforeColor != pet.Color)
        {
            await auditRepository.AddAsync(PetSanitaryIdentityAuditLog.Create(
                pet.Id, request.RequestingUserId, null, PetSanitaryIdentityAuditAction.SanitaryIdentityUpdated,
                "Color", beforeColor, pet.Color), ct);
        }

        if (beforeCanton != pet.ResidenceCanton)
        {
            await auditRepository.AddAsync(PetSanitaryIdentityAuditLog.Create(
                pet.Id, request.RequestingUserId, null, PetSanitaryIdentityAuditAction.ResidenceCantonUpdated,
                "ResidenceCanton", beforeCanton, pet.ResidenceCanton), ct);
        }

        petRepository.Update(pet);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(PetSanitaryIdentityDto.FromDomain(pet));
    }

    internal static async Task<bool> CanAccessPetAsync(Guid ownerId, Guid userId, IFamilyRepository familyRepository, CancellationToken ct)
    {
        if (ownerId == userId) return true;
        var members = await familyRepository.GetActiveMemberIdsAsync(ownerId, ct);
        return members.Contains(userId);
    }
}

public sealed record GetPetSanitaryIdentityQuery(Guid PetId, Guid RequestingUserId)
    : IRequest<Result<PetSanitaryIdentityDto>>;

public sealed class GetPetSanitaryIdentityQueryHandler(
    IPetRepository petRepository,
    IFamilyRepository familyRepository)
    : IRequestHandler<GetPetSanitaryIdentityQuery, Result<PetSanitaryIdentityDto>>
{
    public async Task<Result<PetSanitaryIdentityDto>> Handle(GetPetSanitaryIdentityQuery request, CancellationToken ct)
    {
        var pet = await petRepository.GetByIdAsync(request.PetId, ct);
        if (pet is null) return Result.Failure<PetSanitaryIdentityDto>("Mascota no encontrada.");
        if (!await UpdatePetSanitaryIdentityCommandHandler.CanAccessPetAsync(pet.OwnerId, request.RequestingUserId, familyRepository, ct))
            return Result.Failure<PetSanitaryIdentityDto>("Acceso denegado.");
        return Result.Success(PetSanitaryIdentityDto.FromDomain(pet));
    }
}

public sealed record GetPetSanitaryAuditLogQuery(Guid PetId, Guid RequestingUserId, int Take = 100)
    : IRequest<Result<IReadOnlyList<PetSanitaryAuditDto>>>;

public sealed class GetPetSanitaryAuditLogQueryHandler(
    IPetRepository petRepository,
    IFamilyRepository familyRepository,
    IPetSanitaryIdentityAuditRepository auditRepository)
    : IRequestHandler<GetPetSanitaryAuditLogQuery, Result<IReadOnlyList<PetSanitaryAuditDto>>>
{
    public async Task<Result<IReadOnlyList<PetSanitaryAuditDto>>> Handle(GetPetSanitaryAuditLogQuery request, CancellationToken ct)
    {
        var pet = await petRepository.GetByIdAsync(request.PetId, ct);
        if (pet is null) return Result.Failure<IReadOnlyList<PetSanitaryAuditDto>>("Mascota no encontrada.");
        if (!await UpdatePetSanitaryIdentityCommandHandler.CanAccessPetAsync(pet.OwnerId, request.RequestingUserId, familyRepository, ct))
            return Result.Failure<IReadOnlyList<PetSanitaryAuditDto>>("Acceso denegado.");

        var logs = await auditRepository.GetByPetIdAsync(request.PetId, request.Take, ct);
        return Result.Success(logs.Select(PetSanitaryAuditDto.FromDomain).ToList() as IReadOnlyList<PetSanitaryAuditDto>);
    }
}

public sealed record VerifyPetMicrochipCommand(
    Guid PetId,
    Guid ClinicId,
    Guid ClinicUserId,
    string ObservedChipId,
    string? Notes) : IRequest<Result<PetSanitaryIdentityDto>>;

public sealed record GetClinicPetSanitaryIdentityQuery(Guid PetId, Guid ClinicId, Guid ClinicUserId)
    : IRequest<Result<PetSanitaryIdentityDto>>;

public sealed class GetClinicPetSanitaryIdentityQueryHandler(
    IPetRepository petRepository,
    IClinicRepository clinicRepository,
    IClinicMedicalAccessGrantRepository grantRepository,
    IClinicScanRepository clinicScanRepository)
    : IRequestHandler<GetClinicPetSanitaryIdentityQuery, Result<PetSanitaryIdentityDto>>
{
    private const int RecentScanWindowDays = 90;

    public async Task<Result<PetSanitaryIdentityDto>> Handle(GetClinicPetSanitaryIdentityQuery request, CancellationToken ct)
    {
        var pet = await petRepository.GetByIdAsync(request.PetId, ct);
        if (pet is null) return Result.Failure<PetSanitaryIdentityDto>("Mascota no encontrada.");

        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, ct);
        if (clinic is null || clinic.UserId != request.ClinicUserId)
            return Result.Failure<PetSanitaryIdentityDto>("Acceso denegado.");

        var hasPetAccess = await grantRepository.HasActiveGrantAsync(request.ClinicId, request.PetId, ct)
            || await clinicScanRepository.HasRecentScanAsync(request.ClinicId, request.PetId, RecentScanWindowDays, ct);
        if (!hasPetAccess)
            return Result.Failure<PetSanitaryIdentityDto>("La clínica no tiene acceso activo a esta mascota.");

        return Result.Success(PetSanitaryIdentityDto.FromDomain(pet));
    }
}

public sealed class VerifyPetMicrochipCommandHandler(
    IPetRepository petRepository,
    IClinicRepository clinicRepository,
    IClinicMedicalAccessGrantRepository grantRepository,
    IClinicVerificationRepository clinicVerificationRepository,
    IClinicScanRepository clinicScanRepository,
    IPetSanitaryIdentityAuditRepository auditRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<VerifyPetMicrochipCommand, Result<PetSanitaryIdentityDto>>
{
    private const int RecentScanWindowDays = 90;

    public async Task<Result<PetSanitaryIdentityDto>> Handle(VerifyPetMicrochipCommand request, CancellationToken ct)
    {
        var pet = await petRepository.GetByIdAsync(request.PetId, ct);
        if (pet is null) return Result.Failure<PetSanitaryIdentityDto>("Mascota no encontrada.");

        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, ct);
        if (clinic is null || clinic.UserId != request.ClinicUserId)
            return Result.Failure<PetSanitaryIdentityDto>("Acceso denegado.");
        if (clinic.Status != Domain.Clinics.ClinicStatus.Active)
            return Result.Failure<PetSanitaryIdentityDto>("La clínica no está activa.");
        if (!await clinicVerificationRepository.HasActiveVerificationAsync(request.ClinicId, ct))
            return Result.Failure<PetSanitaryIdentityDto>("La clínica no está verificada.");

        var hasPetAccess = await grantRepository.HasActiveGrantAsync(request.ClinicId, request.PetId, ct)
            || await clinicScanRepository.HasRecentScanAsync(request.ClinicId, request.PetId, RecentScanWindowDays, ct);
        if (!hasPetAccess)
            return Result.Failure<PetSanitaryIdentityDto>("La clínica no tiene acceso activo a esta mascota.");

        var previousStatus = pet.MicrochipVerificationStatus.ToString();
        var previousMicrochip = pet.MicrochipId;
        var verifyResult = pet.VerifyMicrochip(request.ClinicId, request.ObservedChipId, request.Notes);
        var action = verifyResult.IsSuccess
            ? PetSanitaryIdentityAuditAction.MicrochipVerified
            : PetSanitaryIdentityAuditAction.MicrochipConflictFlagged;

        await auditRepository.AddAsync(PetSanitaryIdentityAuditLog.Create(
            pet.Id,
            request.ClinicUserId,
            request.ClinicId,
            action,
            "MicrochipId",
            previousMicrochip ?? previousStatus,
            pet.MicrochipId ?? pet.MicrochipVerificationNotes,
            request.Notes), ct);

        petRepository.Update(pet);
        await unitOfWork.SaveChangesAsync(ct);

        return verifyResult.IsSuccess
            ? Result.Success(PetSanitaryIdentityDto.FromDomain(pet))
            : Result.Failure<PetSanitaryIdentityDto>(verifyResult.Errors);
    }
}

public sealed record GetMicrochipConflictsForAdminQuery(int Page = 1, int PageSize = 20)
    : IRequest<Result<IReadOnlyList<PetSanitaryIdentityDto>>>;

public sealed class GetMicrochipConflictsForAdminQueryHandler(IPetRepository petRepository)
    : IRequestHandler<GetMicrochipConflictsForAdminQuery, Result<IReadOnlyList<PetSanitaryIdentityDto>>>
{
    public async Task<Result<IReadOnlyList<PetSanitaryIdentityDto>>> Handle(GetMicrochipConflictsForAdminQuery request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var pets = await petRepository.GetMicrochipConflictsAsync((page - 1) * pageSize, pageSize, ct);
        return Result.Success(pets.Select(PetSanitaryIdentityDto.FromDomain).ToList() as IReadOnlyList<PetSanitaryIdentityDto>);
    }
}

public sealed record RevokeMicrochipVerificationCommand(Guid PetId, Guid AdminUserId, string Reason)
    : IRequest<Result<PetSanitaryIdentityDto>>;

public sealed record ResolveMicrochipConflictCommand(Guid PetId, Guid AdminUserId, string ConfirmedChipId, string Reason)
    : IRequest<Result<PetSanitaryIdentityDto>>;

public sealed class ResolveMicrochipConflictCommandHandler(
    IPetRepository petRepository,
    IPetSanitaryIdentityAuditRepository auditRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ResolveMicrochipConflictCommand, Result<PetSanitaryIdentityDto>>
{
    public async Task<Result<PetSanitaryIdentityDto>> Handle(ResolveMicrochipConflictCommand request, CancellationToken ct)
    {
        var pet = await petRepository.GetByIdAsync(request.PetId, ct);
        if (pet is null) return Result.Failure<PetSanitaryIdentityDto>("Mascota no encontrada.");

        var previous = pet.MicrochipId;
        var result = pet.ResolveMicrochipConflict(request.AdminUserId, request.ConfirmedChipId, request.Reason);
        if (result.IsFailure) return Result.Failure<PetSanitaryIdentityDto>(result.Errors);

        await auditRepository.AddAsync(PetSanitaryIdentityAuditLog.Create(
            pet.Id,
            request.AdminUserId,
            null,
            PetSanitaryIdentityAuditAction.MicrochipVerified,
            "MicrochipId",
            previous,
            pet.MicrochipId,
            request.Reason), ct);

        petRepository.Update(pet);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(PetSanitaryIdentityDto.FromDomain(pet));
    }
}

public sealed class RevokeMicrochipVerificationCommandHandler(
    IPetRepository petRepository,
    IPetSanitaryIdentityAuditRepository auditRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RevokeMicrochipVerificationCommand, Result<PetSanitaryIdentityDto>>
{
    public async Task<Result<PetSanitaryIdentityDto>> Handle(RevokeMicrochipVerificationCommand request, CancellationToken ct)
    {
        var pet = await petRepository.GetByIdAsync(request.PetId, ct);
        if (pet is null) return Result.Failure<PetSanitaryIdentityDto>("Mascota no encontrada.");

        var previousStatus = pet.MicrochipVerificationStatus.ToString();
        var result = pet.ClearMicrochipVerification(request.AdminUserId, request.Reason);
        if (result.IsFailure) return Result.Failure<PetSanitaryIdentityDto>(result.Errors);

        await auditRepository.AddAsync(PetSanitaryIdentityAuditLog.Create(
            pet.Id,
            request.AdminUserId,
            null,
            PetSanitaryIdentityAuditAction.MicrochipVerificationRevoked,
            "MicrochipVerificationStatus",
            previousStatus,
            pet.MicrochipVerificationStatus.ToString(),
            request.Reason), ct);

        petRepository.Update(pet);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(PetSanitaryIdentityDto.FromDomain(pet));
    }
}
