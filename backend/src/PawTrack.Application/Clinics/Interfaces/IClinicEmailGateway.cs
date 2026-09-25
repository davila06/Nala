using PawTrack.Domain.Common;

namespace PawTrack.Application.Clinics.Interfaces;

public interface IClinicEmailGateway
{
    Task<Result<string>> SendAsync(string recipient, string subject, string text, Guid requestId, CancellationToken cancellationToken = default);
}
