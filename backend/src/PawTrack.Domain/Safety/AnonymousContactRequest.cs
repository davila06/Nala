namespace PawTrack.Domain.Safety;

public sealed class AnonymousContactRequest
{
    private AnonymousContactRequest() { }

    public Guid Id { get; private set; }
    public Guid LostPetEventId { get; private set; }
    public Guid OwnerId { get; private set; }
    public string? FinderName { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    public static AnonymousContactRequest Create(
        Guid lostPetEventId,
        Guid ownerId,
        string? finderName,
        string message) => new()
        {
            Id = Guid.CreateVersion7(),
            LostPetEventId = lostPetEventId,
            OwnerId = ownerId,
            FinderName = string.IsNullOrWhiteSpace(finderName) ? null : finderName.Trim(),
            Message = message.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
        };
}