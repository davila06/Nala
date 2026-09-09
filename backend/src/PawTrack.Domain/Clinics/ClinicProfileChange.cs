using PawTrack.Domain.Common;

namespace PawTrack.Domain.Clinics;

public enum ClinicProfileChangeStatus
{
    Pending,
    Approved,
    Rejected,
}

public sealed class ClinicProfileChange
{
    private ClinicProfileChange() { }

    public Guid Id { get; private set; }
    public Guid ClinicId { get; private set; }
    public Guid RequestedByUserId { get; private set; }
    public string ProposedProfileJson { get; private set; } = string.Empty;
    public ClinicProfileChangeStatus Status { get; private set; }
    public Guid? ReviewedByUserId { get; private set; }
    public DateTimeOffset? ReviewedAt { get; private set; }
    public string? ReviewReason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static ClinicProfileChange Submit(Guid clinicId, Guid requestedByUserId, string proposedProfileJson) => new()
    {
        Id = Guid.CreateVersion7(),
        ClinicId = clinicId,
        RequestedByUserId = requestedByUserId,
        ProposedProfileJson = proposedProfileJson.Trim(),
        Status = ClinicProfileChangeStatus.Pending,
        CreatedAt = DateTimeOffset.UtcNow,
    };

    public Result<bool> Approve(Guid reviewerId, string? reason)
    {
        if (Status != ClinicProfileChangeStatus.Pending)
            return Result.Failure<bool>("El cambio ya fue revisado.");
        Status = ClinicProfileChangeStatus.Approved;
        ReviewedByUserId = reviewerId;
        ReviewedAt = DateTimeOffset.UtcNow;
        ReviewReason = reason?.Trim();
        return Result.Success(true);
    }

    public Result<bool> Reject(Guid reviewerId, string reason)
    {
        if (Status != ClinicProfileChangeStatus.Pending)
            return Result.Failure<bool>("El cambio ya fue revisado.");
        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure<bool>("El motivo de rechazo es requerido.");
        Status = ClinicProfileChangeStatus.Rejected;
        ReviewedByUserId = reviewerId;
        ReviewedAt = DateTimeOffset.UtcNow;
        ReviewReason = reason.Trim();
        return Result.Success(true);
    }
}