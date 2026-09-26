using PawTrack.Domain.Clinics;

namespace PawTrack.Application.Common.Interfaces;

public interface IClinicOrganizationRepository
{
    Task AddAsync(ClinicOrganization organization, CancellationToken cancellationToken = default);
}
