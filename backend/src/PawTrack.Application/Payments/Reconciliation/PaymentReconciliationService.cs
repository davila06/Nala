using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Payments.Interfaces;
using PawTrack.Domain.Payments;

namespace PawTrack.Application.Payments.Reconciliation;

public enum PaymentReconciliationIssueCode
{
    AuthorizedWithoutSettlement = 1,
    SettlementWithoutInternalPayment = 2,
    SettlementWithoutFulfillment = 3,
    AmountMismatch = 4,
    CurrencyMismatch = 5,
    LedgerMissing = 6,
}

public sealed record PaymentSettlementSnapshot(
    string MerchantReference,
    string ProviderOperationId,
    decimal AmountCrc,
    string Currency,
    string Status);

public sealed record PaymentReconciliationIssue(
    PaymentReconciliationIssueCode Code,
    Guid? PaymentIntentId,
    string MerchantReference,
    string Detail);

public sealed record PaymentReconciliationResult(
    Guid OperationId,
    IReadOnlyList<PaymentReconciliationIssue> Issues);

public interface IPaymentSettlementReportProvider
{
    Task<IReadOnlyCollection<PaymentSettlementSnapshot>> GetSettlementsAsync(
        DateTimeOffset since,
        CancellationToken cancellationToken = default);
}

public sealed class PaymentReconciliationService(
    IPaymentIntentRepository intentRepository,
    IPaymentOperationRepository operationRepository,
    IPaymentLedgerRepository ledgerRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<PaymentReconciliationResult> ReconcileAsync(
        IReadOnlyCollection<PaymentSettlementSnapshot> providerSettlements,
        DateTimeOffset cutoff,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var issues = new List<PaymentReconciliationIssue>();
        var providerByReference = providerSettlements
            .Where(item => string.Equals(item.Status, "SETTLED", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(item => item.MerchantReference, StringComparer.OrdinalIgnoreCase);

        foreach (var snapshot in providerByReference.Values)
        {
            var intent = await intentRepository.GetByMerchantReferenceAsync(snapshot.MerchantReference, cancellationToken);
            if (intent is null)
            {
                issues.Add(new PaymentReconciliationIssue(
                    PaymentReconciliationIssueCode.SettlementWithoutInternalPayment,
                    null,
                    snapshot.MerchantReference,
                    "El proveedor reportó settlement sin PaymentIntent interno."));
                continue;
            }

            if (intent.AmountCrc != snapshot.AmountCrc)
                issues.Add(new PaymentReconciliationIssue(
                    PaymentReconciliationIssueCode.AmountMismatch,
                    intent.Id,
                    snapshot.MerchantReference,
                    $"Interno={intent.AmountCrc:F2}; proveedor={snapshot.AmountCrc:F2}."));

            if (!string.Equals(intent.Currency, snapshot.Currency, StringComparison.OrdinalIgnoreCase))
                issues.Add(new PaymentReconciliationIssue(
                    PaymentReconciliationIssueCode.CurrencyMismatch,
                    intent.Id,
                    snapshot.MerchantReference,
                    $"Interno={intent.Currency}; proveedor={snapshot.Currency}."));

            if (intent.Status is not (PaymentIntentStatus.Settled or PaymentIntentStatus.PartiallyRefunded or PaymentIntentStatus.Refunded))
                issues.Add(new PaymentReconciliationIssue(
                    PaymentReconciliationIssueCode.SettlementWithoutFulfillment,
                    intent.Id,
                    snapshot.MerchantReference,
                    $"Settlement externo con estado interno {intent.Status}."));

            if (!await ledgerRepository.ExistsByIntentAndEntryTypeAsync(intent.Id, PaymentLedgerEntryType.Debit, cancellationToken))
                issues.Add(new PaymentReconciliationIssue(
                    PaymentReconciliationIssueCode.LedgerMissing,
                    intent.Id,
                    snapshot.MerchantReference,
                    "Settlement interno sin asiento Debit."));
        }

        var authorizedIntents = await intentRepository.GetByStatusSinceAsync(
            PaymentIntentStatus.Authorized,
            cutoff,
            cancellationToken);
        foreach (var intent in authorizedIntents)
        {
            if (!providerByReference.ContainsKey(intent.MerchantReference))
                issues.Add(new PaymentReconciliationIssue(
                    PaymentReconciliationIssueCode.AuthorizedWithoutSettlement,
                    intent.Id,
                    intent.MerchantReference,
                    "PaymentIntent autorizado sin settlement del proveedor."));
        }

        var reconciliationKey = $"reconciliation:{cutoff.UtcDateTime:yyyy-MM-dd}";
        var existing = await operationRepository.GetByIdempotencyKeyAsync(
            PaymentOperationType.Reconciliation,
            reconciliationKey,
            cancellationToken);
        if (existing is not null)
            return new PaymentReconciliationResult(existing.Id, issues);

        var responseJson = JsonSerializer.Serialize(new
        {
            cutoff,
            settlements = providerSettlements.Count,
            issueCount = issues.Count,
            issues,
        });
        var operation = PaymentOperation.Create(
            null,
            PaymentOperationType.Reconciliation,
            reconciliationKey,
            Hash(responseJson),
            correlationId);
        if (issues.Count == 0)
            operation.MarkSucceeded(null, responseJson);
        else
            operation.MarkFailed("La conciliación detectó divergencias.", responseJson);

        await operationRepository.AddAsync(operation, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new PaymentReconciliationResult(operation.Id, issues);
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
