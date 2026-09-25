namespace PawTrack.Domain.Clinics;

public enum ClinicFiscalSubmissionStatus { PendingProvider, SubmittedToProvider, Failed }

public sealed class ClinicFiscalSubmission
{
    private ClinicFiscalSubmission() { }

    public Guid Id { get; private set; }
    public Guid ClinicId { get; private set; }
    public Guid SaleId { get; private set; }
    public Guid RequestedByUserId { get; private set; }
    public string IssuerTaxId { get; private set; } = string.Empty;
    public ClinicFiscalSubmissionStatus Status { get; private set; }
    public string? ProviderReference { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? SubmittedAt { get; private set; }

    public static ClinicFiscalSubmission Create(Guid clinicId, Guid saleId, string issuerTaxId, Guid actorId)
    {
        if (clinicId == Guid.Empty || saleId == Guid.Empty || actorId == Guid.Empty || string.IsNullOrWhiteSpace(issuerTaxId))
            throw new ArgumentException("Clinic, sale, issuer and actor are required.");
        return new ClinicFiscalSubmission
        {
            Id = Guid.CreateVersion7(),
            ClinicId = clinicId,
            SaleId = saleId,
            RequestedByUserId = actorId,
            IssuerTaxId = issuerTaxId.Trim(),
            Status = ClinicFiscalSubmissionStatus.PendingProvider,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void MarkSubmitted(string providerReference)
    {
        if (Status != ClinicFiscalSubmissionStatus.PendingProvider) throw new InvalidOperationException("Submission already processed.");
        if (string.IsNullOrWhiteSpace(providerReference)) throw new ArgumentException("Provider reference required.", nameof(providerReference));
        ProviderReference = providerReference.Trim();
        Status = ClinicFiscalSubmissionStatus.SubmittedToProvider;
        SubmittedAt = DateTimeOffset.UtcNow;
    }

    public void MarkFailed()
    {
        if (Status != ClinicFiscalSubmissionStatus.PendingProvider) throw new InvalidOperationException("Submission already processed.");
        Status = ClinicFiscalSubmissionStatus.Failed;
    }
}
