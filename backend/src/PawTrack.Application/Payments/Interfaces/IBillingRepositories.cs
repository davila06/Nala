using PawTrack.Domain.Payments;

namespace PawTrack.Application.Payments.Interfaces;

public interface IUserBillingProfileRepository
{
    Task<UserBillingProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(UserBillingProfile profile, CancellationToken cancellationToken = default);
    void Update(UserBillingProfile profile);
}

public interface IElectronicInvoiceRepository
{
    Task<ElectronicInvoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ElectronicInvoice?> GetByClaveAsync(string clave, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ElectronicInvoice>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ElectronicInvoice>> GetByPeriodAsync(DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ElectronicInvoice>> GetPendingHaciendaAsync(CancellationToken cancellationToken = default);
    Task<string> GetNextSequenceNumberAsync(ElectronicInvoiceDocumentType documentType, CancellationToken cancellationToken = default);
    Task AddAsync(ElectronicInvoice invoice, CancellationToken cancellationToken = default);
    void Update(ElectronicInvoice invoice);
}
