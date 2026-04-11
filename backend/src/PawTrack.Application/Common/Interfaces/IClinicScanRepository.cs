using PawTrack.Domain.Clinics;

namespace PawTrack.Application.Common.Interfaces;

public interface IClinicScanRepository
{
    Task AddAsync(ClinicScan scan, CancellationToken cancellationToken = default);
}
