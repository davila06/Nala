using FluentValidation;

namespace PawTrack.Application.Subscriptions.Commands.ReportPayment;

public sealed class ReportPaymentCommandValidator : AbstractValidator<ReportPaymentCommand>
{
    public ReportPaymentCommandValidator()
    {
        RuleFor(x => x.SubscriptionId).NotEmpty();
        RuleFor(x => x.RequestingUserId).NotEmpty();
        RuleFor(x => x.BankReceiptNumber)
            .MaximumLength(64)
            .WithMessage("El número de comprobante no puede exceder 64 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.BankReceiptNumber));
    }
}
