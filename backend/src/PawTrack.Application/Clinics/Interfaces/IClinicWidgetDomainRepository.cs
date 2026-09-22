using PawTrack.Domain.Clinics;

namespace PawTrack.Application.Clinics.Interfaces;

public interface IClinicWidgetDomainRepository
{
    Task<IReadOnlyList<ClinicWidgetDomain>> GetForClinicAsync(Guid clinicId, CancellationToken cancellationToken = default);
    Task<bool> ExistsActiveAsync(Guid clinicId, string domain, CancellationToken cancellationToken = default);
    Task AddAsync(ClinicWidgetDomain domain, CancellationToken cancellationToken = default);
    Task<ClinicWidgetDomain?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    void Update(ClinicWidgetDomain domain);
}
