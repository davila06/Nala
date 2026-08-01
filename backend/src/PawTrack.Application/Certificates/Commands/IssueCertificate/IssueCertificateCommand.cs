using FluentValidation;
using MediatR;
using PawTrack.Application.Certificates.DTOs;
using PawTrack.Application.Certificates.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Certificates;
using PawTrack.Domain.Common;
using System.Security.Cryptography;

namespace PawTrack.Application.Certificates.Commands.IssueCertificate;

public sealed record IssueCertificateCommand(
    Guid PetId,
    Guid ClinicId,
    Guid IssuedByUserId,
    CertificateType Type,
    string? Notes,
    DateTimeOffset? ValidUntil,
    // Denormalized display data for the PDF
    string PetName,
    string PetSpecies,
    string? PetBreed,
    string ClinicName,
    string ClinicLicense,
    string VetName) : IRequest<Result<CertificateDto>>;

public sealed class IssueCertificateCommandValidator : AbstractValidator<IssueCertificateCommand>
{
    public IssueCertificateCommandValidator()
    {
        RuleFor(x => x.PetId).NotEmpty();
        RuleFor(x => x.ClinicId).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.ValidUntil)
            .Must(v => v is null || v > DateTimeOffset.UtcNow)
            .WithMessage("ValidUntil must be in the future.");
    }
}

public sealed class IssueCertificateCommandHandler(
    ICertificateRepository  certificateRepository,
    ICertificateService     certificateService,
    ISubscriptionRepository subscriptionRepository,
    IUnitOfWork             unitOfWork)
    : IRequestHandler<IssueCertificateCommand, Result<CertificateDto>>
{
    public async Task<Result<CertificateDto>> Handle(
        IssueCertificateCommand request,
        CancellationToken cancellationToken)
    {
        // PDF certificates are a ClinicPartner-tier feature
        var subscription = await subscriptionRepository.GetActiveForClinicAsync(request.ClinicId, cancellationToken);
        if (subscription is null || subscription.Tier != Domain.Subscriptions.SubscriptionTier.ClinicPartner)
            return Result.Failure<CertificateDto>("PDF certificate issuance requires an active Clínica Partner subscription.");

        var code = GenerateVerificationCode();
        var certificate = VetCertificate.Issue(
            request.PetId,
            request.ClinicId,
            request.IssuedByUserId,
            request.Type,
            code,
            request.Notes,
            request.ValidUntil);

        await certificateRepository.AddAsync(certificate, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken); // get ID persisted before PDF generation

        var pdfUrl = await certificateService.GenerateAndStoreAsync(
            new CertificatePdfData(
                certificate.Id.ToString(),
                code,
                request.PetName,
                request.PetSpecies,
                request.PetBreed,
                request.ClinicName,
                request.ClinicLicense,
                request.VetName,
                request.Type.ToString(),
                request.Notes,
                certificate.IssuedAt,
                request.ValidUntil),
            cancellationToken);

        certificate.SetPdfUrl(pdfUrl);
        certificateRepository.Update(certificate);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(CertificateDto.FromDomain(certificate));
    }

    private static string GenerateVerificationCode()
    {
        const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        Span<byte> bytes = stackalloc byte[8];
        RandomNumberGenerator.Fill(bytes);
        return string.Create(8, bytes.ToArray(), static (span, b) =>
        {
            for (int i = 0; i < 8; i++)
                span[i] = Alphabet[b[i] % Alphabet.Length];
        });
    }
}
