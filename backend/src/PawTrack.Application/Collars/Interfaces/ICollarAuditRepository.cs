using PawTrack.Domain.Collars;

namespace PawTrack.Application.Collars.Interfaces;

public interface ICollarAuditRepository
{
    Task AddAsync(CollarAuditEntry entry, CancellationToken cancellationToken = default);

    /// <summary>Owner-facing audit log for an activated collar, newest first.</summary>
    Task<IReadOnlyList<CollarAuditEntry>> GetByCollarIdAsync(
        Guid collarId, int skip, int take, CancellationToken cancellationToken = default);

    /// <summary>Admin-facing audit log spanning the full serial lifecycle (pre- and post-activation), newest first.</summary>
    Task<IReadOnlyList<CollarAuditEntry>> GetBySerialAsync(
        string serial, int skip, int take, CancellationToken cancellationToken = default);
}
