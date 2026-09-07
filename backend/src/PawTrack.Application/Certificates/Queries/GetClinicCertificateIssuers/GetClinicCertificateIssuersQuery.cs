using MediatR;
using PawTrack.Application.Certificates.Commands.ManageCertificateIssuers;
using PawTrack.Application.Certificates.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Certificates.Queries.GetClinicCertificateIssuers;

public sealed record GetClinicCertificateIssuersQuery(Guid ClinicId, Guid RequestingUserId)
    : IRequest<Result<ClinicCertificateIssuersDto>>;

public sealed record ClinicCertificateIssuersDto(
    ClinicVerificationDto? Verification,
    IReadOnlyList<ClinicVeterinarianDto> Veterinarians);

public sealed class GetClinicCertificateIssuersQueryHandler(
    IClinicRepository clinicRepository,
    IClinicVerificationRepository verificationRepository,
    IClinicVeterinarianRepository veterinarianRepository)
    : IRequestHandler<GetClinicCertificateIssuersQuery, Result<ClinicCertificateIssuersDto>>
{
    public async Task<Result<ClinicCertificateIssuersDto>> Handle(
        GetClinicCertificateIssuersQuery request,
        CancellationToken cancellationToken)
    {
        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null)
            return Result.Failure<ClinicCertificateIssuersDto>("Clínica no encontrada.");

        if (clinic.UserId != request.RequestingUserId)
            return Result.Failure<ClinicCertificateIssuersDto>("Acceso denegado.");

        var verification = await verificationRepository.GetLatestForClinicAsync(request.ClinicId, cancellationToken);
        var veterinarians = await veterinarianRepository.GetActiveForClinicAsync(request.ClinicId, cancellationToken);

        return Result.Success(new ClinicCertificateIssuersDto(
            verification is null ? null : ClinicVerificationDto.FromDomain(verification),
            veterinarians.Select(ClinicVeterinarianDto.FromDomain).ToList()));
    }
}
