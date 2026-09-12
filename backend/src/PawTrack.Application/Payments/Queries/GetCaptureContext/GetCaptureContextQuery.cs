using MediatR;
using PawTrack.Application.Payments.DTOs;
using PawTrack.Application.Payments.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Payments.Queries.GetCaptureContext;

public sealed record GetCaptureContextQuery(Guid RequestingUserId)
    : IRequest<Result<CaptureContextDto>>;

public sealed class GetCaptureContextQueryHandler(IPaymentGatewayService paymentGatewayService)
    : IRequestHandler<GetCaptureContextQuery, Result<CaptureContextDto>>
{
    public async Task<Result<CaptureContextDto>> Handle(
        GetCaptureContextQuery request, CancellationToken cancellationToken)
    {
        var context = await paymentGatewayService.GenerateCaptureContextAsync(cancellationToken);
        return Result.Success(new CaptureContextDto(
            context.ClientLibraryUrl,
            context.CaptureContextJwt,
            context.KeyId,
            context.IsConfigured));
    }
}
