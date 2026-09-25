namespace PawTrack.Application.Clinics.Interfaces;

public interface IClinicFiscalIssuerRegistry
{
    string? GetVerifiedIssuerTaxId(Guid clinicId);
}
