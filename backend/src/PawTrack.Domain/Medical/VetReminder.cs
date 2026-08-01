namespace PawTrack.Domain.Medical;

public sealed class VetReminder
{
    private VetReminder() { }

    public Guid Id { get; private set; }
    public Guid PetId { get; private set; }
    public Guid OwnerId { get; private set; }
    public MedicalRecordType Type { get; private set; }
    public DateOnly DueDate { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Notes { get; private set; }
    public bool IsCompleted { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset? ReminderSentAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static VetReminder Create(
        Guid petId,
        Guid ownerId,
        MedicalRecordType type,
        DateOnly dueDate,
        string title,
        string? notes = null) => new()
        {
            Id = Guid.CreateVersion7(),
            PetId = petId,
            OwnerId = ownerId,
            Type = type,
            DueDate = dueDate,
            Title = title.Trim(),
            Notes = notes?.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
        };

    public void MarkCompleted()
    {
        IsCompleted = true;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void MarkReminderSent() => ReminderSentAt = DateTimeOffset.UtcNow;
}
