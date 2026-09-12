using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.Commands.ReportPayment;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.UnitTests.Subscriptions.Commands;

public sealed class ReportPaymentCommandHandlerTests
{
    private readonly ISubscriptionRepository _subscriptionRepo = Substitute.For<ISubscriptionRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private ReportPaymentCommandHandler CreateSut() =>
        new(_subscriptionRepo, _unitOfWork);

    [Fact]
    public async Task Handle_WhenSubscriptionNotFound_ReturnsFailure()
    {
        _subscriptionRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Subscription?)null);

        var sut = CreateSut();
        var result = await sut.Handle(new ReportPaymentCommand(Guid.NewGuid(), Guid.NewGuid(), "RCPT-123"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Subscription not found.");
    }

    [Fact]
    public async Task Handle_WhenNotOwner_ReturnsAccessDenied()
    {
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var sub = Subscription.CreateForUser(ownerId, SubscriptionTier.UserPlus, "REF12345", 2990m);

        _subscriptionRepo.GetByIdAsync(sub.Id, Arg.Any<CancellationToken>())
            .Returns(sub);

        var sut = CreateSut();
        var result = await sut.Handle(new ReportPaymentCommand(sub.Id, otherUserId, "RCPT-123"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Access denied.");
    }

    [Fact]
    public async Task Handle_WhenValidOwner_SetsPaymentReportedAndBankReceipt()
    {
        var ownerId = Guid.NewGuid();
        var sub = Subscription.CreateForUser(ownerId, SubscriptionTier.UserPlus, "REF12345", 2990m);

        _subscriptionRepo.GetByIdAsync(sub.Id, Arg.Any<CancellationToken>())
            .Returns(sub);

        var sut = CreateSut();
        var result = await sut.Handle(new ReportPaymentCommand(sub.Id, ownerId, "BNCR-987654"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        sub.PaymentReportedAt.Should().NotBeNull();
        sub.BankReceiptNumber.Should().Be("BNCR-987654");
        result.Value!.BankReceiptNumber.Should().Be("BNCR-987654");
        _subscriptionRepo.Received(1).Update(sub);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
