using MediatR;

namespace PawTrack.Domain.Payments.Events;

public sealed record PaymentIntentSettledDomainEvent(
    Guid PaymentIntentId,
    Guid UserId,
    string Purpose,
    Guid? TargetEntityId,
    decimal AmountCrc) : INotification;
