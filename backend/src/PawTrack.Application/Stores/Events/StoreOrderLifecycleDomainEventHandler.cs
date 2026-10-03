using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Stores;
using PawTrack.Domain.Stores.Events;

namespace PawTrack.Application.Stores.Events;

public sealed class StoreOrderLifecycleDomainEventHandler(
    IStoreRepository storeRepository,
    INotificationDispatcher notificationDispatcher)
    : INotificationHandler<StoreOrderLifecycleDomainEvent>
{
    public async Task Handle(StoreOrderLifecycleDomainEvent notification, CancellationToken cancellationToken)
    {
        var store = await storeRepository.GetByIdAsync(notification.StoreId, cancellationToken);
        if (store is null) return;

        var orderCode = notification.OrderId.ToString("N")[..8].ToUpperInvariant();
        var (recipientUserId, title, body, route) = notification.Status switch
        {
            StoreOrderStatus.AwaitingStoreAcceptance => (
                store.UserId,
                $"Nuevo pedido en {store.Name}",
                $"El pedido #{orderCode} por ₡{notification.TotalCrc:N0} requiere revisar disponibilidad.",
                "/tienda/portal/ordenes"),
            StoreOrderStatus.PaymentReported => (
                store.UserId,
                $"Pago reportado para #{orderCode}",
                "El cliente reportó un pago. Verifica el abono fuera de PawTrack antes de continuar.",
                "/tienda/portal/ordenes"),
            StoreOrderStatus.AwaitingPayment or StoreOrderStatus.PendingPayment => (
                notification.CustomerId,
                "La tienda aceptó tu pedido",
                $"La disponibilidad fue aceptada. Coordina el pago externo del pedido #{orderCode} con la tienda.",
                "/mis-pedidos"),
            StoreOrderStatus.Paid => (
                notification.CustomerId,
                "La tienda registró la verificación del pago",
                $"La tienda indicó que verificó el pago del pedido #{orderCode}. PawTrack no consultó al banco.",
                "/mis-pedidos"),
            StoreOrderStatus.ReadyForPickup => (
                notification.CustomerId,
                "Tu pedido está listo para retirar",
                $"El pedido #{orderCode} está listo para retirar en la tienda.",
                "/mis-pedidos"),
            StoreOrderStatus.OutForDelivery => (
                notification.CustomerId,
                "Tu pedido está en camino",
                $"El pedido #{orderCode} salió para entrega.",
                "/mis-pedidos"),
            StoreOrderStatus.Delivered => (
                notification.CustomerId,
                "Pedido entregado",
                $"El pedido #{orderCode} fue marcado como entregado por la tienda.",
                "/mis-pedidos"),
            StoreOrderStatus.Rejected => (
                notification.CustomerId,
                "La tienda rechazó tu solicitud",
                $"La solicitud de pedido #{orderCode} fue rechazada. Revisa el motivo en tus pedidos.",
                "/mis-pedidos"),
            StoreOrderStatus.Cancelled => (
                notification.CustomerId,
                "Pedido cancelado",
                $"El pedido #{orderCode} fue cancelado. Coordina cualquier pago o devolución directamente con la tienda.",
                "/mis-pedidos"),
            StoreOrderStatus.Expired => (
                notification.CustomerId,
                "Venció la reserva temporal",
                $"La reserva temporal del pedido #{orderCode} venció; verifica la disponibilidad antes de volver a pedir.",
                "/mis-pedidos"),
            StoreOrderStatus.Refunded => (
                notification.CustomerId,
                "La tienda registró una devolución externa",
                $"La tienda registró una referencia de devolución para el pedido #{orderCode}. PawTrack no envió fondos.",
                "/mis-pedidos"),
            _ => (
                notification.CustomerId,
                "Actualización de pedido",
                $"El pedido #{orderCode} cambió a {notification.Status}.",
                "/mis-pedidos"),
        };

        await notificationDispatcher.DispatchStoreOrderLifecycleAsync(
            notification.EventId,
            recipientUserId,
            notification.OrderId,
            title,
            body,
            route,
            cancellationToken);
    }
}
