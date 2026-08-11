namespace PawTrack.Domain.Medical;

/// <summary>
/// A standard preventive-care protocol for a given species and record type.
/// Rows are seeded at migration time; no user-created protocols in MVP.
/// </summary>
public sealed class HealthProtocol
{
    private HealthProtocol() { }

    public Guid Id { get; private set; }
    /// <summary>"Dog" | "Cat" | "Rabbit" | "Bird" — matches Pet.Species string values.</summary>
    public string Species { get; private set; } = string.Empty;
    public MedicalRecordType RecordType { get; private set; }
    /// <summary>Human-readable name shown in the health alert banner.</summary>
    public string ProtocolName { get; private set; } = string.Empty;
    /// <summary>Expected interval in days between the last record and the next due date.</summary>
    public int IntervalDays { get; private set; }

    // ── Seed factory (no public constructor to preserve encapsulation) ────────

    internal static HealthProtocol Seed(
        Guid id, string species, MedicalRecordType recordType,
        string protocolName, int intervalDays) => new()
        {
            Id = id,
            Species = species,
            RecordType = recordType,
            ProtocolName = protocolName,
            IntervalDays = intervalDays,
        };

    // ── Domain logic ──────────────────────────────────────────────────────────

    public DateOnly DueDate(DateOnly lastDate) => lastDate.AddDays(IntervalDays);

    public bool IsOverdue(DateOnly lastDate) =>
        DueDate(lastDate) < DateOnly.FromDateTime(DateTime.UtcNow);

    public int DaysUntilDue(DateOnly lastDate)
    {
        var due = DueDate(lastDate).ToDateTime(TimeOnly.MinValue);
        return (int)(due - DateTime.UtcNow.Date).TotalDays;
    }

    public string Severity(DateOnly lastDate)
    {
        var days = DaysUntilDue(lastDate);
        if (days < 0) return "critical";
        if (days <= 14) return "warning";
        return "info";
    }

    // ── Static seed catalogue ─────────────────────────────────────────────────

    private static readonly Guid[] _seedIds = [
        new("10000000-0000-0000-0000-000000000001"),
        new("10000000-0000-0000-0000-000000000002"),
        new("10000000-0000-0000-0000-000000000003"),
        new("10000000-0000-0000-0000-000000000004"),
        new("10000000-0000-0000-0000-000000000005"),
        new("10000000-0000-0000-0000-000000000006"),
        new("10000000-0000-0000-0000-000000000007"),
        new("10000000-0000-0000-0000-000000000008"),
        new("10000000-0000-0000-0000-000000000009"),
        new("10000000-0000-0000-0000-00000000000a"),
        new("10000000-0000-0000-0000-00000000000b"),
        new("10000000-0000-0000-0000-00000000000c"),
    ];

    public static IReadOnlyList<HealthProtocol> SeedData() =>
    [
        // ── Dogs ─────────────────────────────────────────────────────────────
        Seed(_seedIds[0],  "Dog",    MedicalRecordType.Vaccine,    "Vacunación anual",              365),
        Seed(_seedIds[1],  "Dog",    MedicalRecordType.Deworming,  "Desparasitación semestral",      180),
        Seed(_seedIds[2],  "Dog",    MedicalRecordType.Checkup,    "Revisión veterinaria anual",     365),
        // ── Cats ─────────────────────────────────────────────────────────────
        Seed(_seedIds[3],  "Cat",    MedicalRecordType.Vaccine,    "Vacunación anual",              365),
        Seed(_seedIds[4],  "Cat",    MedicalRecordType.Deworming,  "Desparasitación semestral",      180),
        Seed(_seedIds[5],  "Cat",    MedicalRecordType.Checkup,    "Revisión veterinaria anual",     365),
        // ── Rabbits ───────────────────────────────────────────────────────────
        Seed(_seedIds[6],  "Rabbit", MedicalRecordType.Deworming,  "Desparasitación trimestral",     90),
        Seed(_seedIds[7],  "Rabbit", MedicalRecordType.Checkup,    "Revisión veterinaria semestral", 180),
        // ── Birds ─────────────────────────────────────────────────────────────
        Seed(_seedIds[8],  "Bird",   MedicalRecordType.Checkup,    "Revisión veterinaria anual",     365),
        // ── Other ─────────────────────────────────────────────────────────────
        Seed(_seedIds[9],  "Other",  MedicalRecordType.Vaccine,    "Vacunación anual",              365),
        Seed(_seedIds[10], "Other",  MedicalRecordType.Deworming,  "Desparasitación semestral",      180),
        Seed(_seedIds[11], "Other",  MedicalRecordType.Checkup,    "Revisión veterinaria anual",     365),
    ];
}
