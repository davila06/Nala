using MediatR;
using PawTrack.Application.Payments.DTOs;
using PawTrack.Application.Payments.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Payments.Queries.GetInvoices;

public sealed record GetUserInvoicesQuery(Guid UserId)
    : IRequest<Result<IReadOnlyList<ElectronicInvoiceDto>>>;

public sealed class GetUserInvoicesQueryHandler(IElectronicInvoiceRepository invoiceRepository)
    : IRequestHandler<GetUserInvoicesQuery, Result<IReadOnlyList<ElectronicInvoiceDto>>>
{
    public async Task<Result<IReadOnlyList<ElectronicInvoiceDto>>> Handle(
        GetUserInvoicesQuery request, CancellationToken cancellationToken)
    {
        var invoices = await invoiceRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        var dtos = invoices.Select(ElectronicInvoiceDto.FromDomain).ToList();
        return Result.Success<IReadOnlyList<ElectronicInvoiceDto>>(dtos);
    }
}
