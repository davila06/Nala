using FluentValidation;
using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Medical;

namespace PawTrack.Application.Medical.ClinicAccess;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public sealed record ClinicAccessGrantDto(
    Guid Id,
    Guid PetId,
    Guid ClinicId,
    string ClinicName,
    string InitiatedBy,
    bool IsPending,
    bool IsActive,
    DateTimeOffset? AcceptedAt,
    DateTimeOffset CodeExpiresAt,
    DateTimeOffset CreatedAt);

public sealed record GeneratedAccessCodeDto(
    Guid GrantId,
    string RawCode,
    DateTimeOffset ExpiresAt,
    string InitiatedBy);

// ── Owner generates code → clinic enters it ───────────────────────────────────

public sealed record OwnerGenerateAccessCodeCommand(
    Guid PetId, Guid OwnerId, Guid ClinicId)
    : IRequest<Result<GeneratedAccessCodeDto>>;

public sealed class OwnerGenerateAccessCodeCommandValidator
    : AbstractValidator<OwnerGenerateAccessCodeCommand>
{
    public OwnerGenerateAccessCodeCommandValidator()
    {
        RuleFor(x => x.ClinicId).NotEmpty();
    }
}

public sealed class OwnerGenerateAccessCodeCommandHandler(
    IPetRepository petRepository,
    IClinicRepository clinicRepository,
    IClinicMedicalAccessGrantRepository grantRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<OwnerGenerateAccessCodeCommand, Result<GeneratedAccessCodeDto>>
{
    public async Task<Result<GeneratedAccessCodeDto>> Handle(
        OwnerGenerateAccessCodeCommand request, CancellationToken ct)
    {
        var pet = await petRepository.GetByIdAsync(request.PetId, ct);
        if (pet is null || pet.OwnerId != request.OwnerId)
            return Result.Failure<GeneratedAccessCodeDto>("Mascota no encontrada o acceso denegado.");

        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, ct);
        if (clinic is null) return Result.Failure<GeneratedAccessCodeDto>("Clínica no encontrada.");

        // Idempotency: if active grant already exists, inform caller
        if (await grantRepository.HasActiveGrantAsync(request.ClinicId, request.PetId, ct))
            return Result.Failure<GeneratedAccessCodeDto>("Esta clínica ya tiene acceso activo a la mascota.");

        var (grant, rawCode) = ClinicMedicalAccessGrant.Generate(
            request.PetId, request.ClinicId, request.OwnerId, "Owner");

        await grantRepository.AddAsync(grant, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(new GeneratedAccessCodeDto(
            grant.Id, rawCode, grant.CodeExpiresAt, "Owner"));
    }
}

// ── Clinic accepts owner's code ────────────────────────────────────────────────

public sealed record ClinicAcceptOwnerCodeCommand(
    Guid ClinicId, Guid ClinicUserId, string RawCode)
    : IRequest<Result<ClinicAccessGrantDto>>;

public sealed class ClinicAcceptOwnerCodeCommandHandler(
    IClinicRepository clinicRepository,
    IClinicMedicalAccessGrantRepository grantRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ClinicAcceptOwnerCodeCommand, Result<ClinicAccessGrantDto>>
{
    public async Task<Result<ClinicAccessGrantDto>> Handle(
        ClinicAcceptOwnerCodeCommand request, CancellationToken ct)
    {
        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, ct);
        if (clinic is null) return Result.Failure<ClinicAccessGrantDto>("Clínica no válida.");

        var codeHash = ComputeHash(request.RawCode.Trim().ToUpperInvariant());
        var grant = await grantRepository.FindPendingByCodeHashAsync(codeHash, ct);

        if (grant is null || grant.IsCodeExpired)
            return Result.Failure<ClinicAccessGrantDto>("Código inválido o expirado.");

        if (grant.ClinicId != request.ClinicId)
            return Result.Failure<ClinicAccessGrantDto>("Este código pertenece a otra clínica.");

        if (grant.InitiatedBy != "Owner")
            return Result.Failure<ClinicAccessGrantDto>("Este código fue generado por una clínica. Úselo desde la app del propietario.");

        if (!grant.TryAccept(request.RawCode.Trim().ToUpperInvariant()))
            return Result.Failure<ClinicAccessGrantDto>("El código no es válido.");

        grantRepository.Update(grant);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(new ClinicAccessGrantDto(
            grant.Id, grant.PetId, grant.ClinicId, clinic.Name,
            grant.InitiatedBy, grant.IsPending, grant.IsEffectivelyActive,
            grant.AcceptedAt, grant.CodeExpiresAt, grant.CreatedAt));
    }

    private static string ComputeHash(string raw)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

// ── Clinic generates code → owner enters it ───────────────────────────────────

public sealed record ClinicGenerateAccessCodeCommand(
    Guid ClinicId, Guid ClinicUserId, Guid PetId)
    : IRequest<Result<GeneratedAccessCodeDto>>;

public sealed class ClinicGenerateAccessCodeCommandHandler(
    IClinicRepository clinicRepository,
    IPetRepository petRepository,
    IClinicMedicalAccessGrantRepository grantRepository,
    IClinicScanRepository clinicScanRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ClinicGenerateAccessCodeCommand, Result<GeneratedAccessCodeDto>>
{
    private const int RecentScanWindowDays = 90;

    public async Task<Result<GeneratedAccessCodeDto>> Handle(
        ClinicGenerateAccessCodeCommand request, CancellationToken ct)
    {
        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, ct);
        if (clinic is null) return Result.Failure<GeneratedAccessCodeDto>("Clínica no válida.");

        var pet = await petRepository.GetByIdAsync(request.PetId, ct);
        if (pet is null) return Result.Failure<GeneratedAccessCodeDto>("Mascota no encontrada.");

        // Clinic must have prior legitimate contact with the pet (scan history or existing grant)
        var hasContactHistory =
            await clinicScanRepository.HasRecentScanAsync(request.ClinicId, request.PetId, RecentScanWindowDays, ct)
            || await grantRepository.HasActiveGrantAsync(request.ClinicId, request.PetId, ct);

        if (!hasContactHistory)
            return Result.Failure<GeneratedAccessCodeDto>(
                "La clínica debe haber escaneado la mascota antes de solicitar acceso permanente.");

        if (await grantRepository.HasActiveGrantAsync(request.ClinicId, request.PetId, ct))
            return Result.Failure<GeneratedAccessCodeDto>("Esta clínica ya tiene acceso activo a la mascota.");

        var (grant, rawCode) = ClinicMedicalAccessGrant.Generate(
            request.PetId, request.ClinicId, pet.OwnerId, "Clinic");

        await grantRepository.AddAsync(grant, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(new GeneratedAccessCodeDto(
            grant.Id, rawCode, grant.CodeExpiresAt, "Clinic"));
    }
}

// ── Owner accepts clinic's code ───────────────────────────────────────────────

public sealed record OwnerAcceptClinicCodeCommand(
    Guid PetId, Guid OwnerId, string RawCode)
    : IRequest<Result<ClinicAccessGrantDto>>;

public sealed class OwnerAcceptClinicCodeCommandHandler(
    IPetRepository petRepository,
    IClinicRepository clinicRepository,
    IClinicMedicalAccessGrantRepository grantRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<OwnerAcceptClinicCodeCommand, Result<ClinicAccessGrantDto>>
{
    public async Task<Result<ClinicAccessGrantDto>> Handle(
        OwnerAcceptClinicCodeCommand request, CancellationToken ct)
    {
        var pet = await petRepository.GetByIdAsync(request.PetId, ct);
        if (pet is null || pet.OwnerId != request.OwnerId)
            return Result.Failure<ClinicAccessGrantDto>("Mascota no encontrada o acceso denegado.");

        var codeHash = ComputeHash(request.RawCode.Trim().ToUpperInvariant());
        var grant = await grantRepository.FindPendingByCodeHashAsync(codeHash, ct);

        if (grant is null || grant.IsCodeExpired)
            return Result.Failure<ClinicAccessGrantDto>("Código inválido o expirado.");

        if (grant.PetId != request.PetId)
            return Result.Failure<ClinicAccessGrantDto>("Este código no corresponde a esta mascota.");

        if (grant.InitiatedBy != "Clinic")
            return Result.Failure<ClinicAccessGrantDto>("Este código fue generado por ti. Compártelo con la clínica.");

        if (!grant.TryAccept(request.RawCode.Trim().ToUpperInvariant()))
            return Result.Failure<ClinicAccessGrantDto>("El código no es válido.");

        grantRepository.Update(grant);
        await unitOfWork.SaveChangesAsync(ct);

        var clinic = await clinicRepository.GetByIdAsync(grant.ClinicId, ct);

        return Result.Success(new ClinicAccessGrantDto(
            grant.Id, grant.PetId, grant.ClinicId, clinic?.Name ?? "Clínica",
            grant.InitiatedBy, grant.IsPending, grant.IsEffectivelyActive,
            grant.AcceptedAt, grant.CodeExpiresAt, grant.CreatedAt));
    }

    private static string ComputeHash(string raw)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

// ── Owner revokes grant ────────────────────────────────────────────────────────

public sealed record RevokeClinicAccessGrantCommand(
    Guid PetId, Guid OwnerId, Guid ClinicId)
    : IRequest<Result<bool>>;

public sealed class RevokeClinicAccessGrantCommandHandler(
    IPetRepository petRepository,
    IClinicMedicalAccessGrantRepository grantRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RevokeClinicAccessGrantCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        RevokeClinicAccessGrantCommand request, CancellationToken ct)
    {
        var pet = await petRepository.GetByIdAsync(request.PetId, ct);
        if (pet is null || pet.OwnerId != request.OwnerId)
            return Result.Failure<bool>("Mascota no encontrada o acceso denegado.");

        var grant = await grantRepository.GetActiveGrantAsync(request.ClinicId, request.PetId, ct);
        if (grant is null)
            return Result.Failure<bool>("No existe un acceso activo para esta clínica.");

        grant.Revoke();
        grantRepository.Update(grant);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(true);
    }
}

// ── Owner lists grants for a pet ──────────────────────────────────────────────

public sealed record GetPetClinicGrantsQuery(Guid PetId, Guid OwnerId)
    : IRequest<Result<IReadOnlyList<ClinicAccessGrantDto>>>;

public sealed class GetPetClinicGrantsQueryHandler(
    IPetRepository petRepository,
    IClinicRepository clinicRepository,
    IClinicMedicalAccessGrantRepository grantRepository)
    : IRequestHandler<GetPetClinicGrantsQuery, Result<IReadOnlyList<ClinicAccessGrantDto>>>
{
    public async Task<Result<IReadOnlyList<ClinicAccessGrantDto>>> Handle(
        GetPetClinicGrantsQuery request, CancellationToken ct)
    {
        var pet = await petRepository.GetByIdAsync(request.PetId, ct);
        if (pet is null || pet.OwnerId != request.OwnerId)
            return Result.Failure<IReadOnlyList<ClinicAccessGrantDto>>("Acceso denegado.");

        var grants = await grantRepository.GetByPetIdAsync(request.PetId, ct);

        // batch fetch all clinics in a single query
        var clinicIds = grants.Select(g => g.ClinicId).Distinct();
        var clinics = (await clinicRepository.GetByIdsAsync(clinicIds, ct))
            .ToDictionary(c => c.Id);

        var result = grants.Select(g => new ClinicAccessGrantDto(
            g.Id, g.PetId, g.ClinicId,
            clinics.TryGetValue(g.ClinicId, out var c) ? c.Name : "Clínica desconocida",
            g.InitiatedBy, g.IsPending, g.IsEffectivelyActive,
            g.AcceptedAt, g.CodeExpiresAt, g.CreatedAt)).ToList();

        return Result.Success<IReadOnlyList<ClinicAccessGrantDto>>(result);
    }
}

// ── Clinic lists its authorized pets ─────────────────────────────────────────

public sealed record GetClinicAuthorizedPetsQuery(Guid ClinicId)
    : IRequest<Result<IReadOnlyList<AuthorizedPetDto>>>;

public sealed record AuthorizedPetDto(
    Guid PetId,
    string PetName,
    string Species,
    string? PhotoUrl,
    DateTimeOffset GrantedAt,
    Guid GrantId);

public sealed class GetClinicAuthorizedPetsQueryHandler(
    IPetRepository petRepository,
    IClinicMedicalAccessGrantRepository grantRepository)
    : IRequestHandler<GetClinicAuthorizedPetsQuery, Result<IReadOnlyList<AuthorizedPetDto>>>
{
    public async Task<Result<IReadOnlyList<AuthorizedPetDto>>> Handle(
        GetClinicAuthorizedPetsQuery request, CancellationToken ct)
    {
        var grants = await grantRepository.GetByClinicIdAsync(request.ClinicId, ct);
        var active = grants.Where(g => g.IsEffectivelyActive).ToList();

        // batch fetch all pets in a single query
        var petIds = active.Select(g => g.PetId).Distinct();
        var pets = (await petRepository.GetByIdsAsync(petIds, ct))
            .ToDictionary(p => p.Id);

        var result = active
            .Where(g => pets.ContainsKey(g.PetId))
            .Select(g =>
            {
                var p = pets[g.PetId];
                return new AuthorizedPetDto(
                    p.Id, p.Name, p.Species.ToString(),
                    p.PhotoUrl, g.AcceptedAt!.Value, g.Id);
            })
            .ToList();

        return Result.Success<IReadOnlyList<AuthorizedPetDto>>(result);
    }
}
