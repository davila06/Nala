using MediatR;
using PawTrack.Application.Payments.DTOs;
using PawTrack.Application.Payments.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Payments.Queries.GetUserPaymentProfiles;

public sealed record GetUserPaymentProfilesQuery(Guid UserId)
    : IRequest<Result<IReadOnlyList<PaymentProfileDto>>>;

public sealed class GetUserPaymentProfilesQueryHandler(IUserPaymentProfileRepository profileRepository)
    : IRequestHandler<GetUserPaymentProfilesQuery, Result<IReadOnlyList<PaymentProfileDto>>>
{
    public async Task<Result<IReadOnlyList<PaymentProfileDto>>> Handle(
        GetUserPaymentProfilesQuery request, CancellationToken cancellationToken)
    {
        var profiles = await profileRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        var dtos = profiles.Select(PaymentProfileDto.FromDomain).ToList();
        return Result.Success<IReadOnlyList<PaymentProfileDto>>(dtos);
    }
}
