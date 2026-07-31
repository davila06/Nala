namespace PawTrack.Domain.Bounties;

public enum BountyStatus
{
    /// <summary>Owner declared the bounty; awaiting SINPE deposit confirmation.</summary>
    PendingDeposit = 0,
    /// <summary>Deposit confirmed. Bounty is live on the public map.</summary>
    Active = 1,
    /// <summary>Rescuer triggered claim after HandoverCode verification; awaiting release.</summary>
    Claimed = 2,
    /// <summary>Owner confirmed delivery; funds released to rescuer.</summary>
    Released = 3,
    /// <summary>Owner refunded (pet returned directly, no rescuer involved).</summary>
    Refunded = 4,
    /// <summary>Lost event was closed without a claim; bounty expired.</summary>
    Expired = 5,
}
