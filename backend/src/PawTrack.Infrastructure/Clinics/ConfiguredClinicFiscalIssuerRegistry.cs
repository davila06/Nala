using Microsoft.Extensions.Configuration;
using PawTrack.Application.Clinics.Interfaces;

namespace PawTrack.Infrastructure.Clinics;

public sealed class ConfiguredClinicFiscalIssuerRegistry(IConfiguration configuration) : IClinicFiscalIssuerRegistry
{
    public string? GetVerifiedIssuerTaxId(Guid clinicId)
    {
        var section = configuration.GetSection($"ClinicFiscal:Issuers:{clinicId:N}");
        var taxId = section["TaxId"];
        return section.GetValue<bool>("Verified") && taxId is { Length: >= 9 and <= 12 } && taxId.All(char.IsAsciiDigit)
            ? taxId : null;
    }
}
