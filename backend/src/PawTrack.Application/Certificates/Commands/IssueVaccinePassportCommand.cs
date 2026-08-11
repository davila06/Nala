using FluentValidation;
using MediatR;
using PawTrack.Application.Certificates.DTOs;
using PawTrack.Application.Certificates.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Certificates;
using PawTrack.Domain.Common;
using PawTrack.Domain.Medical;
using System.Security.Cryptography;

namespace PawTrack.Application.Certificates.Commands;

public sealed record IssueVaccinePassportCommand(
    Guid PetId,
    Guid ClinicId,
    Guid IssuedByUserId,
    string VetName,
    string? VetLicense,
    string? PetColor,
    IReadOnlyList<PassportVaccineEntryInput> Vaccines,
    PassportParasiteEntryInput? ParasiteControl)
    : IRequest<Result<CertificateDto>>;

public sealed record PassportVaccineEntryInput(
    string VaccineName,
    string? Brand,
    string? LotNumber,
    DateOnly ApplicationDate,
    DateOnly? ValidUntil);

public sealed record PassportParasiteEntryInput(
    string ProductName,
    DateOnly ApplicationDate,
    DateOnly? NextDueDate);

public sealed class IssueVaccinePassportCommandValidator : AbstractValidator<IssueVaccinePassportCommand>
{
    public IssueVaccinePassportCommandValidator()
    {
        RuleFor(x => x.PetId).NotEmpty();
        RuleFor(x => x.ClinicId).NotEmpty();
        RuleFor(x => x.VetName).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Vaccines).NotEmpty()
            .WithMessage("Al menos una vacuna es requerida para emitir el pasaporte.");
    }
}

public sealed class IssueVaccinePassportCommandHandler(
    ICertificateRepository certificateRepository,
    ICertificateService certificateService,
    IPetRepository petRepository,
    IClinicRepository clinicRepository,
    IUserRepository userRepository,
    ISubscriptionRepository subscriptionRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<IssueVaccinePassportCommand, Result<CertificateDto>>
{
    public async Task<Result<CertificateDto>> Handle(
        IssueVaccinePassportCommand request, CancellationToken ct)
    {
        var subscription = await subscriptionRepository.GetActiveForClinicAsync(request.ClinicId, ct);
        if (subscription is null || subscription.Tier != Domain.Subscriptions.SubscriptionTier.ClinicPartner)
            return Result.Failure<CertificateDto>("El Pasaporte de Vacunas requiere una suscripción Clínica Partner activa.");

        var pet    = await petRepository.GetByIdAsync(request.PetId, ct);
        if (pet is null) return Result.Failure<CertificateDto>("Mascota no encontrada.");

        var clinic = await clinicRepository.GetByUserIdAsync(request.ClinicId, ct);
        if (clinic is null) return Result.Failure<CertificateDto>("Clínica no encontrada.");

        var owner = await userRepository.GetByIdAsync(pet.OwnerId, ct);

        var code = GenerateCode();
        var cert = VetCertificate.Issue(
            request.PetId, request.ClinicId, request.IssuedByUserId,
            CertificateType.VaccinePassport, code,
            validUntil: DateTimeOffset.UtcNow.AddYears(1));

        await certificateRepository.AddAsync(cert, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var vaccines = request.Vaccines
            .Select(v => new PassportVaccineEntry(v.VaccineName, v.Brand, v.LotNumber, v.ApplicationDate, v.ValidUntil))
            .ToList()
            .AsReadOnly() as IReadOnlyList<PassportVaccineEntry>;

        var parasite = request.ParasiteControl is { } pc
            ? new PassportParasiteEntry(pc.ProductName, pc.ApplicationDate, pc.NextDueDate)
            : null;

        var pdfData = new CertificatePdfData(
            cert.Id.ToString(), code,
            pet.Name, pet.Species.ToString(), pet.Breed,
            clinic.Name, clinic.LicenseNumber,
            request.VetName, CertificateType.VaccinePassport.ToString(),
            null, cert.IssuedAt, cert.ValidUntil,
            OwnerName:       owner?.Name,
            MicrochipId:     pet.MicrochipId,
            PetColor:        request.PetColor,
            Vaccines:        vaccines,
            ParasiteControl: parasite);

        var pdfUrl = await certificateService.GenerateAndStoreAsync(pdfData, ct);
        cert.SetPdfUrl(pdfUrl);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(CertificateDto.FromDomain(cert));
    }

    private static string GenerateCode()
    {
        const string chars = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
        return string.Create(8, chars, (span, c) =>
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[8];
            rng.GetBytes(bytes);
            for (var i = 0; i < 8; i++)
                span[i] = c[bytes[i] % c.Length];
        });
    }
}
