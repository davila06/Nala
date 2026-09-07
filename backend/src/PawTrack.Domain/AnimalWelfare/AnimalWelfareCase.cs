using PawTrack.Domain.Common;

namespace PawTrack.Domain.AnimalWelfare;

public enum WelfareCaseType
{
    Abandonment,
    SuspectedAbuse,
    Neglect,
    InjuredAnimal,
    AnimalAtRisk,
    MunicipalCapture,
    Hoarding,
    IrregularAdoption,
    InstitutionalSupport,
}

public enum WelfareCaseStatus
{
    Received,
    Triage,
    Assigned,
    InProgress,
    Referred,
    Resolved,
    Dismissed,
    ClosedNoAction,
}

public enum WelfareSeverity
{
    Low,
    Medium,
    High,
    Critical,
}

public sealed class AnimalWelfareCase
{
    private AnimalWelfareCase() { } // EF Core

    public Guid Id { get; private set; }
    public string PublicCode { get; private set; } = string.Empty;
    public WelfareCaseType Type { get; private set; }
    public WelfareCaseStatus Status { get; private set; }
    public WelfareSeverity Severity { get; private set; }
    public Guid? PetId { get; private set; }
    public Guid? LostPetEventId { get; private set; }
    public Guid? SightingId { get; private set; }
    public Guid? CapturedAnimalId { get; private set; }
    public Guid? AdoptablePetId { get; private set; }
    public string Canton { get; private set; } = string.Empty;
    public double? ApproxLat { get; private set; }
    public double? ApproxLng { get; private set; }
    public string DescriptionSanitized { get; private set; } = string.Empty;
    public Guid? ReporterUserId { get; private set; }
    public bool ReporterIsAnonymous { get; private set; }
    public Guid? AssignedOrganizationUserId { get; private set; }
    public string? AssignedRole { get; private set; }
    public string? ClosureReason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }

    public bool IsClosed => Status is WelfareCaseStatus.Resolved or WelfareCaseStatus.Dismissed or WelfareCaseStatus.ClosedNoAction;

    public static AnimalWelfareCase Create(
        WelfareCaseType type,
        WelfareSeverity severity,
        string canton,
        string descriptionSanitized,
        Guid? reporterUserId,
        bool reporterIsAnonymous,
        double? approxLat,
        double? approxLng,
        Guid? petId = null,
        Guid? lostPetEventId = null,
        Guid? sightingId = null,
        Guid? capturedAnimalId = null,
        Guid? adoptablePetId = null)
    {
        if (string.IsNullOrWhiteSpace(canton)) throw new ArgumentException("Canton is required.", nameof(canton));
        if (string.IsNullOrWhiteSpace(descriptionSanitized)) throw new ArgumentException("Description is required.", nameof(descriptionSanitized));

        var now = DateTimeOffset.UtcNow;
        return new AnimalWelfareCase
        {
            Id = Guid.CreateVersion7(),
            PublicCode = CreatePublicCode(),
            Type = type,
            Severity = severity,
            Status = WelfareCaseStatus.Received,
            Canton = canton.Trim(),
            DescriptionSanitized = descriptionSanitized.Trim(),
            ReporterUserId = reporterIsAnonymous ? null : reporterUserId,
            ReporterIsAnonymous = reporterIsAnonymous,
            ApproxLat = approxLat,
            ApproxLng = approxLng,
            PetId = petId,
            LostPetEventId = lostPetEventId,
            SightingId = sightingId,
            CapturedAnimalId = capturedAnimalId,
            AdoptablePetId = adoptablePetId,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public Result<bool> StartTriage(Guid actorUserId)
    {
        if (IsClosed) return Result.Failure<bool>("No se puede iniciar triage de un caso cerrado.");
        Status = WelfareCaseStatus.Triage;
        Touch();
        return Result.Success(true);
    }

    public Result<bool> SetSeverity(WelfareSeverity severity, Guid actorUserId)
    {
        if (IsClosed) return Result.Failure<bool>("No se puede cambiar severidad de un caso cerrado.");
        Severity = severity;
        Touch();
        return Result.Success(true);
    }

    public Result<bool> AssignTo(Guid organizationUserId, string role, Guid actorUserId)
    {
        if (IsClosed) return Result.Failure<bool>("No se puede asignar un caso cerrado.");
        if (organizationUserId == Guid.Empty) return Result.Failure<bool>("La organización asignada es requerida.");
        if (string.IsNullOrWhiteSpace(role)) return Result.Failure<bool>("El rol asignado es requerido.");
        AssignedOrganizationUserId = organizationUserId;
        AssignedRole = role.Trim();
        Status = WelfareCaseStatus.Assigned;
        Touch();
        return Result.Success(true);
    }

    public Result<bool> MarkInProgress(Guid actorUserId)
    {
        if (IsClosed) return Result.Failure<bool>("No se puede actualizar un caso cerrado.");
        Status = WelfareCaseStatus.InProgress;
        Touch();
        return Result.Success(true);
    }

    public Result<bool> ReferTo(Guid actorUserId, string reason)
    {
        if (IsClosed) return Result.Failure<bool>("No se puede derivar un caso cerrado.");
        if (string.IsNullOrWhiteSpace(reason)) return Result.Failure<bool>("El motivo de derivación es requerido.");
        Status = WelfareCaseStatus.Referred;
        Touch();
        return Result.Success(true);
    }

    public Result<bool> Resolve(Guid actorUserId, string resolution)
    {
        if (string.IsNullOrWhiteSpace(resolution)) return Result.Failure<bool>("La resolución es requerida.");
        if (IsClosed) return Result.Failure<bool>("El caso ya está cerrado.");
        Status = WelfareCaseStatus.Resolved;
        ClosureReason = resolution.Trim();
        ClosedAt = DateTimeOffset.UtcNow;
        Touch();
        return Result.Success(true);
    }

    public Result<bool> Dismiss(Guid actorUserId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason)) return Result.Failure<bool>("El motivo de descarte es requerido.");
        if (IsClosed) return Result.Failure<bool>("El caso ya está cerrado.");
        Status = WelfareCaseStatus.Dismissed;
        ClosureReason = reason.Trim();
        ClosedAt = DateTimeOffset.UtcNow;
        Touch();
        return Result.Success(true);
    }

    public Result<bool> CloseNoAction(Guid actorUserId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason)) return Result.Failure<bool>("El motivo de cierre es requerido.");
        if (IsClosed) return Result.Failure<bool>("El caso ya está cerrado.");
        Status = WelfareCaseStatus.ClosedNoAction;
        ClosureReason = reason.Trim();
        ClosedAt = DateTimeOffset.UtcNow;
        Touch();
        return Result.Success(true);
    }

    private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;

    private static string CreatePublicCode()
    {
        Span<byte> bytes = stackalloc byte[6];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes);
    }
}
