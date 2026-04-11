namespace PawTrack.Domain.Fosters;

public sealed class CustodyRecord
{
    private CustodyRecord() { } // EF Core

    public Guid Id { get; private set; }
    public Guid FosterUserId { get; private set; }
    public Guid FoundPetReportId { get; private set; }
    public int ExpectedDays { get; private set; }
    public string? Note { get; private set; }
    public CustodyStatus Status { get; private set; }
    public string? Outcome { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }

    public static CustodyRecord Start(
        Guid fosterUserId,
        Guid foundPetReportId,
        int expectedDays,
        string? note)
    {
        return new CustodyRecord
        {
            Id = Guid.CreateVersion7(),
            FosterUserId = fosterUserId,
            FoundPetReportId = foundPetReportId,
            ExpectedDays = expectedDays,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            Status = CustodyStatus.Active,
            StartedAt = DateTimeOffset.UtcNow,
        };
    }

    public void Close(string outcome)
    {
        Outcome = outcome.Trim();
        Status = CustodyStatus.Closed;
        ClosedAt = DateTimeOffset.UtcNow;
    }
}
