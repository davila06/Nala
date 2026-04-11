namespace PawTrack.Domain.Safety;

public enum FraudContext
{
    /// <summary>Suspicious behaviour observed via the public pet profile (QR scan / link).</summary>
    PublicProfile,

    /// <summary>Suspicious content or threat received through the masked chat.</summary>
    ChatMessage,

    /// <summary>After a phone number or contact detail was shared outside the app.</summary>
    PhoneContact,

    /// <summary>Other / unclassified.</summary>
    Other,
}
