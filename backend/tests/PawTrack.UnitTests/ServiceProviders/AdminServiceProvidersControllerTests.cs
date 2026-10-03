using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using PawTrack.API.Controllers;
using PawTrack.Application.ServiceProviders;
using PawTrack.Domain.Common;
using System.Security.Claims;

namespace PawTrack.UnitTests.ServiceProviders;

public sealed class AdminServiceProvidersControllerTests
{
    [Fact]
    public async Task RecordExternalRefund_ForwardsAdminAndEvidence()
    {
        var sender = Substitute.For<ISender>();
        RecordManualProviderRefundCommand? capturedCommand = null;
        sender.Send(Arg.Do<RecordManualProviderRefundCommand>(command => capturedCommand = command), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<ProviderPaymentDto>("test")));
        var adminId = Guid.NewGuid();
        var controller = new AdminServiceProvidersController(sender)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, adminId.ToString())], "test")),
                },
            },
        };

        var response = await controller.RecordExternalRefund(
            Guid.NewGuid(), new RecordProviderExternalRefundRequest(
                5000m, "BANK-REFUND-7", "Devolución SINPE ya realizada", "provider-refund-7"),
            CancellationToken.None);

        response.Should().BeOfType<UnprocessableEntityObjectResult>();
        capturedCommand.Should().NotBeNull();
        capturedCommand!.AdminUserId.Should().Be(adminId);
        capturedCommand.AmountCrc.Should().Be(5000m);
        capturedCommand.ExternalReference.Should().Be("BANK-REFUND-7");
        capturedCommand.Reason.Should().Be("Devolución SINPE ya realizada");
        capturedCommand.IdempotencyKey.Should().Be("provider-refund-7");
    }

    [Fact]
    public void ControllerRequiresAdminRole()
    {
        var authorization = typeof(AdminServiceProvidersController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .Single();

        authorization.Roles.Should().Be("Admin");
    }
}