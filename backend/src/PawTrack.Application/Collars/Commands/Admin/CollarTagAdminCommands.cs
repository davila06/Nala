using MediatR;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Collars;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Collars.Commands.Admin;

// ── Register single serial ───────────────────────────────────────────────────

public sealed record RegisterCollarTagCommand(string Serial, string FirmwareVersion)
    : IRequest<Result<CollarTagDto>>;

public sealed class RegisterCollarTagCommandHandler(
    ICollarTagRepository collarTagRepository,
    ICollarAuditRepository auditRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RegisterCollarTagCommand, Result<CollarTagDto>>
{
    public async Task<Result<CollarTagDto>> Handle(
        RegisterCollarTagCommand request, CancellationToken cancellationToken)
    {
        var existing = await collarTagRepository.GetBySerialAsync(request.Serial.ToUpperInvariant(), cancellationToken);
        if (existing is not null)
            return Result.Failure<CollarTagDto>("El serial ya existe en inventario.");

        CollarTag tag;
        try { tag = CollarTag.CreateFromFactory(request.Serial, request.FirmwareVersion); }
        catch (ArgumentException ex) { return Result.Failure<CollarTagDto>(ex.Message); }

        await collarTagRepository.AddAsync(tag, cancellationToken);
        await auditRepository.AddAsync(
            CollarAuditEntry.Create(
                CollarAuditEvent.SerialRegistered,
                $"Firmware {tag.FirmwareVersion}",
                serial: tag.Serial),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(CollarTagDto.FromDomain(tag));
    }
}

// ── Mark as sold ─────────────────────────────────────────────────────────────

public sealed record MarkCollarTagSoldCommand(string Serial) : IRequest<Result<bool>>;

public sealed class MarkCollarTagSoldCommandHandler(
    ICollarTagRepository collarTagRepository,
    ICollarAuditRepository auditRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<MarkCollarTagSoldCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        MarkCollarTagSoldCommand request, CancellationToken cancellationToken)
    {
        var tag = await collarTagRepository.GetBySerialAsync(request.Serial.ToUpperInvariant(), cancellationToken);
        if (tag is null)
            return Result.Failure<bool>("Serial no encontrado.");

        try { tag.MarkSold(); }
        catch (InvalidOperationException ex) { return Result.Failure<bool>(ex.Message); }

        collarTagRepository.Update(tag);
        await auditRepository.AddAsync(
            CollarAuditEntry.Create(CollarAuditEvent.SerialMarkedSold, "Marcado como vendido", serial: tag.Serial),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(true);
    }
}

// ── Bulk import from CSV ─────────────────────────────────────────────────────

public sealed record BulkImportCollarTagsCommand(IReadOnlyList<(string Serial, string FirmwareVersion)> Items)
    : IRequest<Result<BulkImportResultDto>>;

public sealed record BulkImportResultDto(int Imported, int Skipped, IReadOnlyList<string> Errors);

public sealed class BulkImportCollarTagsCommandHandler(
    ICollarTagRepository collarTagRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<BulkImportCollarTagsCommand, Result<BulkImportResultDto>>
{
    public async Task<Result<BulkImportResultDto>> Handle(
        BulkImportCollarTagsCommand request, CancellationToken cancellationToken)
    {
        int imported = 0, skipped = 0;
        var errors = new List<string>();

        foreach (var (serial, fw) in request.Items)
        {
            var existing = await collarTagRepository.GetBySerialAsync(serial.ToUpperInvariant(), cancellationToken);
            if (existing is not null) { skipped++; continue; }

            try
            {
                var tag = CollarTag.CreateFromFactory(serial, fw);
                await collarTagRepository.AddAsync(tag, cancellationToken);
                imported++;
            }
            catch (ArgumentException ex)
            {
                errors.Add($"{serial}: {ex.Message}");
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(new BulkImportResultDto(imported, skipped, errors));
    }
}

// ── Revoke device credential (stolen device) ─────────────────────────────────

public sealed record RevokeCollarCredentialCommand(string Serial) : IRequest<Result<bool>>;

public sealed class RevokeCollarCredentialCommandHandler(
    ICollarTagRepository collarTagRepository,
    ICollarDeviceCredentialRepository credentialRepository,
    ICollarAuditRepository auditRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RevokeCollarCredentialCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        RevokeCollarCredentialCommand request, CancellationToken cancellationToken)
    {
        var tag = await collarTagRepository.GetBySerialAsync(request.Serial.ToUpperInvariant(), cancellationToken);
        if (tag is null || tag.CollarId is null)
            return Result.Failure<bool>("Serial no encontrado o no está activado.");

        var credentials = await credentialRepository.GetForCollarAsync(tag.CollarId.Value, cancellationToken);
        var active = credentials.Where(c => c.IsUsable).ToList();
        if (active.Count == 0)
            return Result.Failure<bool>("No hay credenciales activas para revocar.");

        foreach (var cred in active)
        {
            cred.Revoke();
            credentialRepository.Update(cred);
        }

        await auditRepository.AddAsync(
            CollarAuditEntry.Create(
                CollarAuditEvent.DeviceKeyRevoked,
                $"{active.Count} credencial(es) revocada(s) por admin",
                collarId: tag.CollarId, serial: tag.Serial),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(true);
    }
}

// ── Bulk mark sold ────────────────────────────────────────────────────────────

public sealed record BulkMarkCollarTagsSoldCommand(IReadOnlyList<string> Serials) : IRequest<Result<BulkActionResultDto>>;

public sealed record BulkActionResultDto(int Succeeded, int Failed, IReadOnlyList<string> Errors);

public sealed class BulkMarkCollarTagsSoldCommandHandler(
    ICollarTagRepository collarTagRepository,
    ICollarAuditRepository auditRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<BulkMarkCollarTagsSoldCommand, Result<BulkActionResultDto>>
{
    public async Task<Result<BulkActionResultDto>> Handle(
        BulkMarkCollarTagsSoldCommand request, CancellationToken cancellationToken)
    {
        int succeeded = 0;
        var errors = new List<string>();

        foreach (var serial in request.Serials)
        {
            var tag = await collarTagRepository.GetBySerialAsync(serial.ToUpperInvariant(), cancellationToken);
            if (tag is null) { errors.Add($"{serial}: no encontrado."); continue; }

            try
            {
                tag.MarkSold();
                collarTagRepository.Update(tag);
                await auditRepository.AddAsync(
                    CollarAuditEntry.Create(CollarAuditEvent.SerialMarkedSold, "Marcado como vendido (bulk)", serial: tag.Serial),
                    cancellationToken);
                succeeded++;
            }
            catch (InvalidOperationException ex)
            {
                errors.Add($"{serial}: {ex.Message}");
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(new BulkActionResultDto(succeeded, errors.Count, errors));
    }
}

// ── Bulk revoke ───────────────────────────────────────────────────────────────

public sealed record BulkRevokeCollarTagsCommand(IReadOnlyList<string> Serials, string? Reason)
    : IRequest<Result<BulkActionResultDto>>;

public sealed class BulkRevokeCollarTagsCommandHandler(
    ICollarTagRepository collarTagRepository,
    ICollarDeviceCredentialRepository credentialRepository,
    ICollarAuditRepository auditRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<BulkRevokeCollarTagsCommand, Result<BulkActionResultDto>>
{
    public async Task<Result<BulkActionResultDto>> Handle(
        BulkRevokeCollarTagsCommand request, CancellationToken cancellationToken)
    {
        int succeeded = 0;
        var errors = new List<string>();
        var detail = string.IsNullOrWhiteSpace(request.Reason) ? "Revocado (bulk)" : request.Reason.Trim();

        foreach (var serial in request.Serials)
        {
            var tag = await collarTagRepository.GetBySerialAsync(serial.ToUpperInvariant(), cancellationToken);
            if (tag is null || tag.CollarId is null)
            {
                errors.Add($"{serial}: no encontrado o no activado.");
                continue;
            }

            var credentials = await credentialRepository.GetForCollarAsync(tag.CollarId.Value, cancellationToken);
            var active = credentials.Where(c => c.IsUsable).ToList();
            if (active.Count == 0)
            {
                errors.Add($"{serial}: sin credenciales activas.");
                continue;
            }

            foreach (var cred in active)
            {
                cred.Revoke();
                credentialRepository.Update(cred);
            }

            await auditRepository.AddAsync(
                CollarAuditEntry.Create(
                    CollarAuditEvent.DeviceKeyRevoked, detail, collarId: tag.CollarId, serial: tag.Serial),
                cancellationToken);
            succeeded++;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(new BulkActionResultDto(succeeded, errors.Count, errors));
    }
}

// ── Shared DTO ───────────────────────────────────────────────────────────────

public sealed record CollarTagDto(
    Guid Id,
    string Serial,
    Guid? CollarId,
    string Status,
    string FirmwareVersion,
    DateTimeOffset ManufacturedAt,
    DateTimeOffset? SoldAt,
    DateTimeOffset? ActivatedAt,
    DateTimeOffset? LastPingAt)
{
    public static CollarTagDto FromDomain(CollarTag t) =>
        new(t.Id, t.Serial, t.CollarId, t.Status.ToString(),
            t.FirmwareVersion, t.ManufacturedAt, t.SoldAt, t.ActivatedAt, t.LastPingAt);
}
