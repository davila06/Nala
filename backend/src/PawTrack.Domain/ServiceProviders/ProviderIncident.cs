namespace PawTrack.Domain.ServiceProviders;

public sealed class ProviderIncident
{
    private ProviderIncident() { }

    public Guid Id { get; private set; }
    public Guid ServiceProviderId { get; private set; }
    public Guid ReportedByUserId { get; private set; }
    public ProviderIncidentType Type { get; private set; }
    public ProviderIncidentStatus Status { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string? EvidenceReference { get; private set; }
    public string? Resolution { get; private set; }
    public string? AppealReason { get; private set; }
    public Guid? AssignedToUserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? InvestigatingAt { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }

    public static ProviderIncident Open(
        Guid serviceProviderId,
        Guid reportedByUserId,
        ProviderIncidentType type,
        string description,
        string? evidenceReference)
    {
        if (serviceProviderId == Guid.Empty || reportedByUserId == Guid.Empty)
            throw new ArgumentException("Los identificadores del incidente son requeridos.");
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La descripción del incidente es requerida.", nameof(description));

        return new ProviderIncident
        {
            Id = Guid.CreateVersion7(),
            ServiceProviderId = serviceProviderId,
            ReportedByUserId = reportedByUserId,
            Type = type,
            Status = ProviderIncidentStatus.Open,
            Description = description.Trim(),
            EvidenceReference = evidenceReference?.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void StartInvestigation(Guid assignedToUserId)
    {
        if (Status is not (ProviderIncidentStatus.Open or ProviderIncidentStatus.Appealed))
            throw new InvalidOperationException("El incidente no está disponible para investigación.");
        if (assignedToUserId == Guid.Empty)
            throw new ArgumentException("El responsable es requerido.", nameof(assignedToUserId));

        Status = ProviderIncidentStatus.Investigating;
        AssignedToUserId = assignedToUserId;
        InvestigatingAt = DateTimeOffset.UtcNow;
    }

    public void Resolve(Guid resolvedByUserId, string resolution)
    {
        if (Status != ProviderIncidentStatus.Investigating)
            throw new InvalidOperationException("Solo un incidente en investigación puede resolverse.");
        if (resolvedByUserId == Guid.Empty || string.IsNullOrWhiteSpace(resolution))
            throw new ArgumentException("La resolución y el responsable son requeridos.");

        Status = ProviderIncidentStatus.Resolved;
        Resolution = resolution.Trim();
        ResolvedAt = DateTimeOffset.UtcNow;
    }

    public void Appeal(string reason)
    {
        if (Status != ProviderIncidentStatus.Resolved)
            throw new InvalidOperationException("Solo un incidente resuelto puede apelarse.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("El motivo de apelación es requerido.", nameof(reason));

        Status = ProviderIncidentStatus.Appealed;
        AppealReason = reason.Trim();
    }

    public void Close(Guid closedByUserId)
    {
        if (Status != ProviderIncidentStatus.Resolved)
            throw new InvalidOperationException("Solo un incidente resuelto puede cerrarse.");
        if (closedByUserId == Guid.Empty)
            throw new ArgumentException("El responsable es requerido.", nameof(closedByUserId));

        Status = ProviderIncidentStatus.Closed;
        ClosedAt = DateTimeOffset.UtcNow;
    }
}