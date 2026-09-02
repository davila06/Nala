using PawTrack.Domain.Collars;

namespace PawTrack.Application.Common.Interfaces;

public interface ICollarDeviceCredentialRepository
{
    Task<CollarDeviceCredential?> GetActiveByHashAsync(string keyHash, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CollarDeviceCredential>> GetForCollarAsync(Guid collarId, CancellationToken cancellationToken = default);
    Task AddAsync(CollarDeviceCredential credential, CancellationToken cancellationToken = default);
    void Update(CollarDeviceCredential credential);
}
