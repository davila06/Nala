using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using PawTrack.Application.Clinics.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Infrastructure.Clinics;

public sealed class ConfiguredClinicFiscalGateway(IHttpClientFactory clients, IConfiguration configuration) : IClinicFiscalGateway
{
    public async Task<Result<string>> SubmitAsync(Guid clinicId, Guid saleId, string issuerTaxId, string receiptNumber,
        decimal totalCrc, CancellationToken cancellationToken = default)
    {
        var url = configuration["ClinicFiscal:ProviderUrl"];
        var token = configuration["ClinicFiscal:ApiToken"];
        if (string.IsNullOrWhiteSpace(token) || !Uri.TryCreate(url, UriKind.Absolute, out var endpoint)
            || endpoint.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(endpoint.UserInfo))
            return Result.Failure<string>("Proveedor fiscal HTTPS no configurado.");
        if (string.IsNullOrWhiteSpace(issuerTaxId)) return Result.Failure<string>("Emisor fiscal requerido.");

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Idempotency-Key", saleId.ToString("N"));
        request.Content = JsonContent.Create(new { clinicId, saleId, issuerTaxId, receiptNumber, totalCrc, currency = "CRC" });
        try
        {
            using var response = await clients.CreateClient("ClinicFiscal").SendAsync(request, cancellationToken);
            if (response.StatusCode != HttpStatusCode.Accepted || !response.Headers.TryGetValues("X-Fiscal-Reference", out var values))
                return Result.Failure<string>("Proveedor fiscal no confirmo recepcion.");
            var reference = values.FirstOrDefault();
            return string.IsNullOrWhiteSpace(reference) ? Result.Failure<string>("Falta referencia fiscal del proveedor.") : Result.Success(reference);
        }
        catch (HttpRequestException)
        {
            return Result.Failure<string>("No se pudo contactar al proveedor fiscal.");
        }
    }
}
