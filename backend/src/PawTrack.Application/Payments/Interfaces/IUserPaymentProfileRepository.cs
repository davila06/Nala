using PawTrack.Domain.Payments;

namespace PawTrack.Application.Payments.Interfaces;

public interface IUserPaymentProfileRepository
{
    Task<UserPaymentProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserPaymentProfile>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserPaymentProfile?> GetDefaultByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(UserPaymentProfile profile, CancellationToken cancellationToken = default);
    void Update(UserPaymentProfile profile);
    void Delete(UserPaymentProfile profile);
}
