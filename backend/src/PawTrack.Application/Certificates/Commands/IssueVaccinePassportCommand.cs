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
    Guid VeterinarianId,
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
        RuleFor(x => x.VeterinarianId).NotEmpty();
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
    IClinicMedicalAccessGrantRepository grantRepository,
    IClinicVerificationRepository clinicVerificationRepository,
    IClinicVeterinarianRepository veterinarianRepository,
    IVaccinePassportRepository vaccinePassportRepository,
    ICertificateAuditLogRepository auditLogRepository,
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

        var pet = await petRepository.GetByIdAsync(request.PetId, ct);
        if (pet is null) return Result.Failure<CertificateDto>("Mascota no encontrada.");

        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, ct);
        if (clinic is null) return Result.Failure<CertificateDto>("Clínica no encontrada.");
        if (clinic.UserId != request.IssuedByUserId)
            return Result.Failure<CertificateDto>("Acceso denegado.");
        if (clinic.Status != Domain.Clinics.ClinicStatus.Active)
            return Result.Failure<CertificateDto>("La clínica no está activa.");

        var verification = await clinicVerificationRepository.GetActiveForClinicAsync(request.ClinicId, ct);
        if (verification is null || !verification.IsActive)
            return Result.Failure<CertificateDto>("La clínica no está verificada para emitir pasaportes.");

        var veterinarian = await veterinarianRepository.GetByIdAsync(request.VeterinarianId, ct);
        if (veterinarian is null || veterinarian.ClinicId != request.ClinicId || !veterinarian.IsActive)
            return Result.Failure<CertificateDto>("El veterinario no está autorizado para emitir pasaportes.");

        if (!await grantRepository.HasActiveGrantAsync(request.ClinicId, request.PetId, ct))
            return Result.Failure<CertificateDto>("La clínica no tiene acceso activo al expediente de esta mascota.");

        if (pet.Species == Domain.Pets.PetSpecies.Dog && !HasRabiesVaccine(request.Vaccines))
            return Result.Failure<CertificateDto>("La vacuna contra la rabia es requerida para perros.");

        var owner = await userRepository.GetByIdAsync(pet.OwnerId, ct);

        var code = GenerateCode();
        var cert = VetCertificate.Issue(
            request.PetId, request.ClinicId, request.IssuedByUserId,
            CertificateType.VaccinePassport, code,
            validUntil: DateTimeOffset.UtcNow.AddYears(1));

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
            veterinarian.FullName, CertificateType.VaccinePassport.ToString(),
            null, cert.IssuedAt, cert.ValidUntil,
            OwnerName: owner?.Name,
            MicrochipId: pet.MicrochipId,
            PetColor: request.PetColor,
            Vaccines: vaccines,
            ParasiteControl: parasite);

        var passport = VaccinePassport.Issue(
            cert.Id,
            request.PetId,
            request.ClinicId,
            request.VeterinarianId,
            new VaccinePassportPetSnapshot(
                pet.Name,
                pet.Species.ToString(),
                pet.Breed,
                null,
                request.PetColor,
                pet.MicrochipId,
                owner?.Name),
            new VaccinePassportIssuerSnapshot(
                clinic.Name,
                clinic.LicenseNumber,
                veterinarian.FullName,
                veterinarian.LicenseNumber),
            request.Vaccines
                .Select(v => new VaccinePassportVaccine(v.VaccineName, v.Brand, v.LotNumber, v.ApplicationDate, v.ValidUntil))
                .ToList()
                .AsReadOnly(),
            request.ParasiteControl is { } parasiteInput
                ? new VaccinePassportParasiteControl(parasiteInput.ProductName, parasiteInput.ApplicationDate, parasiteInput.NextDueDate)
                : null,
            DateOnly.FromDateTime(cert.ValidUntil!.Value.UtcDateTime),
            code);

        await certificateRepository.AddAsync(cert, ct);
        await vaccinePassportRepository.AddAsync(passport, ct);
        await auditLogRepository.AddAsync(
            CertificateAuditLog.Create(cert.Id, CertificateAuditAction.Issued, request.IssuedByUserId), ct);
        await unitOfWork.SaveChangesAsync(ct);

        var pdfUrl = await certificateService.GenerateAndStoreAsync(pdfData, ct);
        cert.SetPdfUrl(pdfUrl);
        await auditLogRepository.AddAsync(
            CertificateAuditLog.Create(cert.Id, CertificateAuditAction.PdfGenerated, request.IssuedByUserId), ct);
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

    private static bool HasRabiesVaccine(IReadOnlyList<PassportVaccineEntryInput> vaccines) =>
        vaccines.Any(vaccine =>
            vaccine.VaccineName.Contains("rabia", StringComparison.OrdinalIgnoreCase) ||
            vaccine.VaccineName.Contains("rabies", StringComparison.OrdinalIgnoreCase));
}
