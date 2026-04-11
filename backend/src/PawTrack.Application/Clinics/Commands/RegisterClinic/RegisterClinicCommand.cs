using MediatR;
using PawTrack.Application.Clinics.DTOs;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Auth;
using PawTrack.Domain.Clinics;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Clinics.Commands.RegisterClinic;

/// <summary>
/// Registers a new clinic and creates the associated user account (Role = Clinic, Status = Pending).
/// Admin approval is required to activate the clinic before it can scan.
/// </summary>
public sealed record RegisterClinicCommand(
    string Name,
    string LicenseNumber,
    string Address,
    decimal Lat,
    decimal Lng,
    string ContactEmail,
    string Password) : IRequest<Result<ClinicDto>>
{
    /// <summary>
    /// Canonical error message for duplicate contact email.
    /// Used by the API layer for anti-enumeration: the controller maps this specific
    /// failure to a stealth 201 response so callers cannot determine whether a given
    /// email address is already registered as a clinic account.
    /// </summary>
    public const string DuplicateEmailError =
        "That email address is already associated with an account.";
}

public sealed class RegisterClinicCommandHandler(
    IClinicRepository clinicRepository,
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RegisterClinicCommand, Result<ClinicDto>>
{
    public async Task<Result<ClinicDto>> Handle(
        RegisterClinicCommand request,
        CancellationToken cancellationToken)
    {
        // Guard: license number uniqueness
        var existing = await clinicRepository.GetByLicenseNumberAsync(
            request.LicenseNumber, cancellationToken);

        if (existing is not null)
            return Result.Failure<ClinicDto>("A clinic with that SENASA license number is already registered.");

        // Guard: email uniqueness
        var emailInUse = await userRepository.ExistsByEmailAsync(request.ContactEmail, cancellationToken);
        if (emailInUse)
            return Result.Failure<ClinicDto>(RegisterClinicCommand.DuplicateEmailError);

        // Create user with Clinic role
        var passwordHash = passwordHasher.Hash(request.Password);
        var (user, _) = User.Create(request.ContactEmail, passwordHash, request.Name); // raw token discarded — clinics bypass email verification
        user.AssignClinicRole();

        // Clinics bypass email verification — they are activiated manually by admin
        // (In a later sprint, send an admin-to-clinic welcome email here.)

        await userRepository.AddAsync(user, cancellationToken);

        // Create clinic entity linked to this user
        var clinic = Clinic.Create(
            user.Id,
            request.Name,
            request.LicenseNumber,
            request.Address,
            request.Lat,
            request.Lng,
            request.ContactEmail);

        await clinicRepository.AddAsync(clinic, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(ClinicDto.FromDomain(clinic));
    }
}
