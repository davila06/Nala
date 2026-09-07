namespace PawTrack.Domain.ServiceProviders;

public sealed class ProviderService
{
    private ProviderService() { }

    public Guid Id { get; private set; }
    public Guid ServiceProviderId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public ServiceModality Modality { get; private set; }
    public int DurationMinutes { get; private set; }
    public decimal PriceCrc { get; private set; }
    public int Capacity { get; private set; }
    public ProviderServiceStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public static ProviderService Create(
        Guid serviceProviderId,
        string name,
        string description,
        ServiceModality modality,
        int durationMinutes,
        decimal priceCrc,
        int capacity) => new()
        {
            Id = Guid.CreateVersion7(),
            ServiceProviderId = serviceProviderId,
            Name = name.Trim(),
            Description = description.Trim(),
            Modality = modality,
            DurationMinutes = durationMinutes,
            PriceCrc = priceCrc,
            Capacity = capacity,
            Status = ProviderServiceStatus.Published,
            CreatedAt = DateTimeOffset.UtcNow,
        };

    public void Pause() => Status = ProviderServiceStatus.Paused;
    public void Publish() => Status = ProviderServiceStatus.Published;
    public void Archive() => Status = ProviderServiceStatus.Archived;

    public void Update(
        string name,
        string description,
        ServiceModality modality,
        int durationMinutes,
        decimal priceCrc,
        int capacity)
    {
        if (Status == ProviderServiceStatus.Archived)
            throw new InvalidOperationException("No se puede editar un servicio archivado.");

        Name = name.Trim();
        Description = description.Trim();
        Modality = modality;
        DurationMinutes = durationMinutes;
        PriceCrc = priceCrc;
        Capacity = capacity;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}