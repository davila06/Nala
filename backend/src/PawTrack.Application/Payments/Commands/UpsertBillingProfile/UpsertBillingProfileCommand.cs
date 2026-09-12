using FluentValidation;
using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Payments.DTOs;
using PawTrack.Application.Payments.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Payments;

namespace PawTrack.Application.Payments.Commands.UpsertBillingProfile;

public sealed record UpsertBillingProfileCommand(
    Guid UserId,
    TaxIdentificationType IdentificationType,
    string IdentificationNumber,
    string LegalName,
    string BillingEmail,
    bool RequiresInvoice = true,
    string? Province = null,
    string? Canton = null,
    string? District = null,
    string? AddressDetails = null,
    string? PhoneNumber = null)
    : IRequest<Result<UserBillingProfileDto>>;

public sealed class UpsertBillingProfileCommandValidator : AbstractValidator<UpsertBillingProfileCommand>
{
    public UpsertBillingProfileCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.IdentificationNumber)
            .NotEmpty().WithMessage("El número de identificación es obligatorio.")
            .MaximumLength(30);
        RuleFor(x => x.LegalName)
            .NotEmpty().WithMessage("La razón social o nombre legal es obligatorio.")
            .MaximumLength(200);
        RuleFor(x => x.BillingEmail)
            .NotEmpty().WithMessage("El correo electrónico de facturación es obligatorio.")
            .EmailAddress().WithMessage("Formato de correo electrónico inválido.")
            .MaximumLength(200);
    }
}

public sealed class UpsertBillingProfileCommandHandler(
    IUserBillingProfileRepository profileRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpsertBillingProfileCommand, Result<UserBillingProfileDto>>
{
    public async Task<Result<UserBillingProfileDto>> Handle(
        UpsertBillingProfileCommand request, CancellationToken cancellationToken)
    {
        var existing = await profileRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (existing is null)
        {
            var created = UserBillingProfile.Create(
                request.UserId,
                request.IdentificationType,
                request.IdentificationNumber,
                request.LegalName,
                request.BillingEmail,
                request.RequiresInvoice,
                request.Province,
                request.Canton,
                request.District,
                request.AddressDetails,
                request.PhoneNumber);

            await profileRepository.AddAsync(created, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success(UserBillingProfileDto.FromDomain(created));
        }

        existing.Update(
            request.IdentificationType,
            request.IdentificationNumber,
            request.LegalName,
            request.BillingEmail,
            request.RequiresInvoice,
            request.Province,
            request.Canton,
            request.District,
            request.AddressDetails,
            request.PhoneNumber);

        profileRepository.Update(existing);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(UserBillingProfileDto.FromDomain(existing));
    }
}
