using FluentValidation;
using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Payments.DTOs;
using PawTrack.Application.Payments.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Payments;

namespace PawTrack.Application.Payments.Commands.SavePaymentProfile;

public sealed record SavePaymentProfileCommand(
    Guid UserId,
    string TransientToken,
    string? CardholderName = null,
    bool SetAsDefault = true)
    : IRequest<Result<PaymentProfileDto>>;

public sealed class SavePaymentProfileCommandValidator : AbstractValidator<SavePaymentProfileCommand>
{
    public SavePaymentProfileCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.TransientToken).NotEmpty().MaximumLength(5000);
        RuleFor(x => x.CardholderName).MaximumLength(150);
    }
}

public sealed class SavePaymentProfileCommandHandler(
    IUserPaymentProfileRepository profileRepository,
    IPaymentGatewayService paymentGatewayService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SavePaymentProfileCommand, Result<PaymentProfileDto>>
{
    public async Task<Result<PaymentProfileDto>> Handle(
        SavePaymentProfileCommand request, CancellationToken cancellationToken)
    {
        var tokenResult = await paymentGatewayService.TokenizeTransientTokenAsync(
            new TokenizePaymentRequest(request.TransientToken, request.CardholderName),
            cancellationToken);

        if (!tokenResult.Success)
        {
            return Result.Failure<PaymentProfileDto>(
                tokenResult.ErrorMessage ?? "No se pudo tokenizar la tarjeta con la pasarela.");
        }

        var existingProfiles = await profileRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        var shouldBeDefault = request.SetAsDefault || existingProfiles.Count == 0;

        if (shouldBeDefault)
        {
            foreach (var p in existingProfiles.Where(x => x.IsDefault))
            {
                p.SetDefault(false);
                profileRepository.Update(p);
            }
        }

        var newProfile = UserPaymentProfile.CreateCard(
            request.UserId,
            tokenResult.PaymentInstrumentId ?? tokenResult.CustomerProfileId ?? Guid.NewGuid().ToString("N"),
            tokenResult.CardBrand ?? "Card",
            tokenResult.LastFourDigits ?? "0000",
            tokenResult.ExpirationMonth,
            tokenResult.ExpirationYear,
            request.CardholderName,
            shouldBeDefault);

        await profileRepository.AddAsync(newProfile, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(PaymentProfileDto.FromDomain(newProfile));
    }
}
