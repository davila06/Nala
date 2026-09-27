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
    Guid VeterinarianId,
    Guid IssuedByUserId,
    CertificateType Type,
    string? Notes,
    DateTimeOffset? ValidUntil) : IRequest<Result<CertificateDto>>;

public sealed class IssueCertificateCommandValidator : AbstractValidator<IssueCertificateCommand>
{
    public IssueCertificateCommandValidator()
    {
        RuleFor(x => x.PetId).NotEmpty();
        RuleFor(x => x.ClinicId).NotEmpty();
        RuleFor(x => x.VeterinarianId).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.ValidUntil)
            .Must(v => v is null || v > DateTimeOffset.UtcNow)
            .WithMessage("ValidUntil must be in the future.");
    }
}

public sealed class IssueCertificateCommandHandler(
    ICertificateRepository certificateRepository,
    ICertificateService certificateService,
    ISubscriptionRepository subscriptionRepository,
    IClinicRepository clinicRepository,
    IPetRepository petRepository,
    IClinicMedicalAccessGrantRepository grantRepository,
    IClinicVeterinarianRepository veterinarianRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<IssueCertificateCommand, Result<CertificateDto>>
{
    public async Task<Result<CertificateDto>> Handle(
        IssueCertificateCommand request,
        CancellationToken cancellationToken)
    {
        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null || clinic.UserId != request.IssuedByUserId
            || clinic.Status != Domain.Clinics.ClinicStatus.Active)
            return Result.Failure<CertificateDto>("Acceso denegado.");

        var pet = await petRepository.GetByIdAsync(request.PetId, cancellationToken);
        if (pet is null || !await grantRepository.HasActiveGrantAsync(request.ClinicId, request.PetId, cancellationToken))
            return Result.Failure<CertificateDto>("La clínica no tiene acceso activo a esta mascota.");

        var veterinarian = await veterinarianRepository.GetByIdAsync(request.VeterinarianId, cancellationToken);
        if (veterinarian is null || veterinarian.ClinicId != request.ClinicId
            || !veterinarian.HasPermission(ClinicVeterinarianPermission.IssueCertificates))
            return Result.Failure<CertificateDto>("Debe seleccionarse un veterinario autorizado de esta clínica.");

        // PDF certificates are a ClinicPartner-tier feature
        var subscription = await subscriptionRepository.GetActiveForClinicAsync(request.ClinicId, cancellationToken);
        if (subscription is null || subscription.Tier != Domain.Subscriptions.SubscriptionTier.ClinicPartner)
            return Result.Failure<CertificateDto>("PDF certificate issuance requires an active Clínica Partner subscription.");

        var cycleStart = new DateTimeOffset(DateTimeOffset.UtcNow.Year, DateTimeOffset.UtcNow.Month, 1, 0, 0, 0, TimeSpan.Zero);
        if (await certificateRepository.CountForClinicSinceAsync(request.ClinicId, cycleStart, null, cancellationToken) >= 500)
            return Result.Failure<CertificateDto>("La clínica alcanzó la cuota mensual de certificados.");

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

        var artifact = await certificateService.GenerateAndStoreAsync(
            new CertificatePdfData(
                certificate.Id.ToString(),
                code,
                pet.Name,
                pet.Species.ToString(),
                pet.Breed,
                clinic.Name,
                clinic.LicenseNumber,
                veterinarian.FullName,
                request.Type.ToString(),
                request.Notes,
                certificate.IssuedAt,
                request.ValidUntil,
                VeterinarianLicense: veterinarian.LicenseNumber),
            cancellationToken);

        certificate.SetPdfUrl(artifact.PdfUrl);
        if (artifact.SignatureUrl is not null)
            certificate.SetSignature(artifact.SignatureUrl, artifact.SignatureAlgorithm!);
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
