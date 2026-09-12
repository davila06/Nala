using MediatR;
using Microsoft.Extensions.Configuration;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Subscriptions.Queries.GetSubscriptionQrCode;

public sealed record GetSubscriptionQrCodeQuery(Guid SubscriptionId, Guid RequestingUserId)
    : IRequest<Result<byte[]>>;

public sealed class GetSubscriptionQrCodeQueryHandler(
    ISubscriptionRepository subscriptionRepository,
    IQrCodeService qrCodeService,
    IConfiguration configuration)
    : IRequestHandler<GetSubscriptionQrCodeQuery, Result<byte[]>>
{
    public async Task<Result<byte[]>> Handle(
        GetSubscriptionQrCodeQuery request, CancellationToken cancellationToken)
    {
        var sub = await subscriptionRepository.GetByIdAsync(request.SubscriptionId, cancellationToken);
        if (sub is null)
            return Result.Failure<byte[]>("Subscription not found.");

        if (sub.UserId != request.RequestingUserId && sub.ClinicOwnerId != request.RequestingUserId)
            return Result.Failure<byte[]>("Access denied.");

        var rawPhone = configuration["App:SinpePhone"] ?? "7000-0000";
        var cleanedPhone = rawPhone.Replace("-", "").Replace(" ", "");

        // Format recognized by mobile QR scanners and SMS templates:
        // PASE <MONTO> <TELEFONO> <REFERENCIA>
        var qrPayload = $"PASE {sub.AmountCrc:0} {cleanedPhone} {sub.PaymentReference}";
        var pngBytes = qrCodeService.GeneratePng(qrPayload);

        return Result.Success(pngBytes);
    }
}
