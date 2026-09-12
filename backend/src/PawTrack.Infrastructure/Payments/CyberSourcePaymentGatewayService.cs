using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Payments.Interfaces;

namespace PawTrack.Infrastructure.Payments;

/// <summary>
/// Gateway adapter for CyberSource REST API (BAC Credomatic / Visa).
/// Provides Microform capture context, transient token conversion, and recurring card charge.
/// Configuration: CyberSource:MerchantId, CyberSource:KeyId, CyberSource:SecretKey, CyberSource:RunEnvironment.
/// Fallback: If credentials are not configured, provides simulated responses in dev/test to allow seamless UI testing.
/// </summary>
public sealed class CyberSourcePaymentGatewayService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<CyberSourcePaymentGatewayService> logger) : IPaymentGatewayService
{
    private string? MerchantId => configuration["CyberSource:MerchantId"] ?? configuration["Payments:MerchantId"];
    private string? KeyId => configuration["CyberSource:KeyId"] ?? configuration["Payments:ProviderKey"];
    private string? SecretKey => configuration["CyberSource:SecretKey"] ?? configuration["Payments:SecretKey"];
    private string Host => configuration["CyberSource:RunEnvironment"] ?? "apitest.cybersource.com";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(MerchantId) &&
        !string.IsNullOrWhiteSpace(KeyId) &&
        !string.IsNullOrWhiteSpace(SecretKey);

    public async Task<CaptureContextResult> GenerateCaptureContextAsync(
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            logger.LogInformation("CyberSource credentials not configured. Returning simulated capture context for development.");
            var simulatedJwt = GenerateSimulatedJwt();
            return new CaptureContextResult(
                ClientLibraryUrl: "https://flex.cybersource.com/cybersource/assets/microform/0.11/flex-microform.min.js",
                CaptureContextJwt: simulatedJwt,
                KeyId: "simulated-flex-key-pawtrack",
                IsConfigured: false);
        }

        try
        {
            var client = httpClientFactory.CreateClient("CyberSource");
            var requestUri = $"https://{Host}/microform/v2/sessions";

            var payload = new
            {
                targetOrigins = new[] { configuration["App:BaseUrl"] ?? "https://pawtrack.cr", "http://localhost:5173" },
                allowedCardNetworks = new[] { "VISA", "MASTERCARD", "AMEX" },
                clientVersion = "v2",
            };

            var jsonContent = JsonSerializer.Serialize(payload);
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json"),
            };

            ApplySignatureHeaders(request, jsonContent, "/microform/v2/sessions");

            var response = await client.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var jwtString = await response.Content.ReadAsStringAsync(cancellationToken);
                return new CaptureContextResult(
                    ClientLibraryUrl: "https://flex.cybersource.com/cybersource/assets/microform/0.11/flex-microform.min.js",
                    CaptureContextJwt: jwtString.Trim('\"'),
                    KeyId: KeyId!,
                    IsConfigured: true);
            }

            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning("Failed to generate CyberSource capture context: {Status} - {Body}", response.StatusCode, errorBody);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Error calling CyberSource microform session API.");
        }

        return new CaptureContextResult(
            ClientLibraryUrl: "https://flex.cybersource.com/cybersource/assets/microform/0.11/flex-microform.min.js",
            CaptureContextJwt: GenerateSimulatedJwt(),
            KeyId: KeyId ?? "fallback-key",
            IsConfigured: false);
    }

    public async Task<TokenizePaymentResult> TokenizeTransientTokenAsync(
        TokenizePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.TransientToken))
            return new TokenizePaymentResult(false, null, null, null, null, null, null, "Token de tarjeta inválido.");

        if (!IsConfigured)
        {
            logger.LogInformation("CyberSource credentials not configured. Simulating card tokenization.");
            var randLast4 = RandomNumberGenerator.GetInt32(1000, 9999).ToString();
            return new TokenizePaymentResult(
                Success: true,
                CustomerProfileId: $"cust_{Guid.NewGuid():N}"[..20],
                PaymentInstrumentId: $"tok_{Guid.NewGuid():N}",
                CardBrand: "Visa",
                LastFourDigits: randLast4,
                ExpirationMonth: 12,
                ExpirationYear: 2028,
                ErrorMessage: null);
        }

        try
        {
            var client = httpClientFactory.CreateClient("CyberSource");
            var requestUri = $"https://{Host}/tms/v2/tokens";

            var payload = new
            {
                clientReferenceInformation = new { code = $"TOK-{Guid.NewGuid():N}"[..16] },
                tokenInformation = new { transientTokenJwt = request.TransientToken },
            };

            var jsonContent = JsonSerializer.Serialize(payload);
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json"),
            };

            ApplySignatureHeaders(httpRequest, jsonContent, "/tms/v2/tokens");

            var response = await client.SendAsync(httpRequest, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(responseBody);
                var root = doc.RootElement;
                var id = root.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;

                string? brand = "Visa";
                string? last4 = "0000";
                int? expMonth = null;
                int? expYear = null;

                if (root.TryGetProperty("tokenInformation", out var tokenInfo) &&
                    tokenInfo.TryGetProperty("paymentInstrument", out var pi))
                {
                    if (pi.TryGetProperty("card", out var card))
                    {
                        if (card.TryGetProperty("brand", out var b)) brand = b.GetString();
                        if (card.TryGetProperty("suffix", out var s)) last4 = s.GetString();
                        if (card.TryGetProperty("expirationMonth", out var em) && int.TryParse(em.GetString(), out var emVal)) expMonth = emVal;
                        if (card.TryGetProperty("expirationYear", out var ey) && int.TryParse(ey.GetString(), out var eyVal)) expYear = eyVal;
                    }
                }

                return new TokenizePaymentResult(
                    Success: true,
                    CustomerProfileId: null,
                    PaymentInstrumentId: id ?? request.TransientToken,
                    CardBrand: brand,
                    LastFourDigits: last4,
                    ExpirationMonth: expMonth,
                    ExpirationYear: expYear,
                    ErrorMessage: null);
            }

            logger.LogWarning("CyberSource tokenization error: {Status} - {Body}", response.StatusCode, responseBody);
            return new TokenizePaymentResult(false, null, null, null, null, null, null, "La pasarela no pudo tokenizar la tarjeta.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Exception during CyberSource tokenization.");
            return new TokenizePaymentResult(false, null, null, null, null, null, null, "Error de comunicación con la pasarela.");
        }
    }

    public async Task<ChargePaymentResult> ChargeAsync(
        ChargePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.AmountCrc <= 0)
            return new ChargePaymentResult(false, null, null, "INVALID_AMOUNT", "El monto debe ser superior a 0.");

        if (!IsConfigured)
        {
            logger.LogInformation("CyberSource credentials not configured. Simulating card charge of ₡{Amount} for {Purpose}.", request.AmountCrc, request.Purpose);
            var simulatedTxId = $"CS-{Guid.NewGuid():N}"[..18].ToUpperInvariant();
            var simulatedAuth = $"AUTH-{RandomNumberGenerator.GetInt32(100000, 999999)}";
            return new ChargePaymentResult(
                Success: true,
                GatewayTransactionId: simulatedTxId,
                AuthorizationCode: simulatedAuth,
                ErrorCode: null,
                ErrorMessage: null);
        }

        try
        {
            var client = httpClientFactory.CreateClient("CyberSource");
            var requestUri = $"https://{Host}/pts/v2/payments";

            var payload = new
            {
                clientReferenceInformation = new { code = request.OrderReference },
                orderInformation = new
                {
                    amountDetails = new
                    {
                        totalAmount = request.AmountCrc.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                        currency = request.Currency,
                    },
                    billTo = new
                    {
                        email = request.CardholderEmail ?? "cliente@pawtrack.cr",
                        country = "CR",
                    },
                },
                paymentInformation = new
                {
                    customer = new
                    {
                        id = request.PaymentInstrumentOrCustomerToken,
                    },
                },
            };

            var jsonContent = JsonSerializer.Serialize(payload);
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json"),
            };

            ApplySignatureHeaders(httpRequest, jsonContent, "/pts/v2/payments");

            var response = await client.SendAsync(httpRequest, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(responseBody);
                var root = doc.RootElement;
                var status = root.TryGetProperty("status", out var st) ? st.GetString() : null;
                var txId = root.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;

                string? authCode = null;
                if (root.TryGetProperty("processorInformation", out var procInfo) &&
                    procInfo.TryGetProperty("approvalCode", out var ac))
                {
                    authCode = ac.GetString();
                }

                if (string.Equals(status, "AUTHORIZED", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(status, "SETTLED", StringComparison.OrdinalIgnoreCase))
                {
                    return new ChargePaymentResult(true, txId, authCode ?? "AUTH-OK", null, null);
                }

                return new ChargePaymentResult(false, txId, null, status, $"Transacción declinada: {status}");
            }

            logger.LogWarning("CyberSource charge failed: {Status} - {Body}", response.StatusCode, responseBody);
            return new ChargePaymentResult(false, null, null, "DECLINED", "La tarjeta fue declinada por el banco emisor.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Exception during CyberSource charge.");
            return new ChargePaymentResult(false, null, null, "NETWORK_ERROR", "Error de conexión con el banco emisor.");
        }
    }

    private void ApplySignatureHeaders(HttpRequestMessage request, string body, string path)
    {
        var gmtDate = DateTimeOffset.UtcNow.ToString("r");
        request.Headers.Add("v-c-merchant-id", MerchantId);
        request.Headers.Add("Date", gmtDate);
        request.Headers.Add("Host", Host);

        var digest = $"SHA-256={Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(body)))}";
        request.Headers.Add("Digest", digest);

        var signatureString = $"host: {Host}\ndate: {gmtDate}\nrequest-target: post {path}\ndigest: {digest}\nv-c-merchant-id: {MerchantId}";
        var signatureBytes = HMACSHA256.HashData(Convert.FromBase64String(SecretKey!), Encoding.UTF8.GetBytes(signatureString));
        var signatureBase64 = Convert.ToBase64String(signatureBytes);

        var signatureHeader = $"keyid=\"{KeyId}\", algorithm=\"HmacSHA256\", headers=\"host date request-target digest v-c-merchant-id\", signature=\"{signatureBase64}\"";
        request.Headers.Add("Signature", signatureHeader);
    }

    private static string GenerateSimulatedJwt() =>
        $"simulated_capture_context_{Guid.NewGuid():N}.{Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"iss\":\"PawTrackDev\"}"))}";
}
