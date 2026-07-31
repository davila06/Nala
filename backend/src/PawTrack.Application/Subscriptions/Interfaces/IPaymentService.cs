namespace PawTrack.Application.Subscriptions.Interfaces;

/// <summary>Generates a payment reference for SINPE Móvil and verifies payment confirmation.</summary>
public interface IPaymentService
{
    /// <summary>Returns an 8-character uppercase alphanumeric reference unique to this payment.</summary>
    string GenerateReference();
}
