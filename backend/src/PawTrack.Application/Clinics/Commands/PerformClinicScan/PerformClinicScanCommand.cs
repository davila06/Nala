using System.Text.RegularExpressions;
using MediatR;
using PawTrack.Application.Clinics.DTOs;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Clinics;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Clinics.Commands.PerformClinicScan;

/// <summary>
/// Scans a QR code URL or RFID chip identifier at a clinic.
/// Looks up the associated pet, notifies the owner, and records the scan audit entry.
/// </summary>
public sealed record PerformClinicScanCommand(
    Guid ClinicId,
    string Input,
    ScanInputType InputType) : IRequest<Result<ClinicScanResultDto>>;

public sealed class PerformClinicScanCommandHandler(
    IClinicRepository clinicRepository,
    IClinicScanRepository clinicScanRepository,
    IPetRepository petRepository,
    IUserRepository userRepository,
    INotificationDispatcher notificationDispatcher,
    IUnitOfWork unitOfWork)
    : IRequestHandler<PerformClinicScanCommand, Result<ClinicScanResultDto>>
{
    // Matches the pet Id embedded in QR profile URLs: /p/{guid}
    private static readonly Regex PetIdPattern =
        new(@"\/p\/([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public async Task<Result<ClinicScanResultDto>> Handle(
        PerformClinicScanCommand request,
        CancellationToken cancellationToken)
    {
        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);

        if (clinic is null)
            return Result.Failure<ClinicScanResultDto>("Clinic not found.");

        if (clinic.Status != ClinicStatus.Active)
            return Result.Failure<ClinicScanResultDto>("Clinic account is not active.");

        // Resolve pet from input
        var pet = request.InputType switch
        {
            ScanInputType.Qr       => await ResolvePetFromQrAsync(request.Input, cancellationToken),
            ScanInputType.RfidChip => await petRepository.GetByMicrochipIdAsync(
                                          request.Input.Trim().ToUpperInvariant(), cancellationToken),
            _                      => null,
        };

        // Record audit scan regardless of match result
        var scan = ClinicScan.Create(clinic.Id, request.Input, request.InputType, pet?.Id);
        await clinicScanRepository.AddAsync(scan, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (pet is null)
        {
            return Result.Success(new ClinicScanResultDto(
                ScanId: scan.Id,
                Matched: false,
                PetName: null,
                PetPhotoUrl: null,
                OwnerName: null,
                OwnerNotified: false,
                PetSpecies: null));
        }

        // Fetch owner info
        var owner = await userRepository.GetByIdAsync(pet.OwnerId, cancellationToken);
        if (owner is null)
            return Result.Failure<ClinicScanResultDto>("Pet owner account not found.");

        // Fire-and-forget notification (best-effort; scan is already persisted)
        _ = notificationDispatcher.DispatchClinicScanDetectedAsync(
            owner.Id,
            owner.Email,
            owner.Name,
            pet.Name,
            clinic.Name,
            clinic.Address,
            cancellationToken);

        return Result.Success(new ClinicScanResultDto(
            ScanId: scan.Id,
            Matched: true,
            PetName: pet.Name,
            PetPhotoUrl: pet.PhotoUrl,
            OwnerName: owner.Name,
            OwnerNotified: true,   // owner was contacted server-side — raw email is never surfaced
            PetSpecies: pet.Species.ToString()));
    }

    private async Task<Domain.Pets.Pet?> ResolvePetFromQrAsync(
        string input, CancellationToken cancellationToken)
    {
        var match = PetIdPattern.Match(input);
        if (!match.Success) return null;

        if (!Guid.TryParse(match.Groups[1].Value, out var petId)) return null;

        return await petRepository.GetByIdAsync(petId, cancellationToken);
    }
}
