using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using System.Security.Claims;
using PawTrack.API.Controllers;
using PawTrack.Application.Stores;
using PawTrack.Domain.Common;
using PawTrack.Domain.Stores;

namespace PawTrack.UnitTests.Stores;

public sealed class StoreOrdersControllerTests
{
    [Fact]
    public async Task PlaceOrder_RequiresIdempotencyHeader()
    {
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<PlaceStoreOrderCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<StoreOrderDto>("test")));
        var controller = new StoreOrdersController(sender)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
        controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())], "test"));

        var result = await controller.PlaceOrder(
            new PlaceOrderRequest(Guid.NewGuid(), "Pickup", null, null, [new OrderLineRequest(Guid.NewGuid(), 1)]),
            CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        await sender.DidNotReceive().Send(Arg.Any<PlaceStoreOrderCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PlaceOrder_ForwardsIdempotencyHeaderToCommand()
    {
        var sender = Substitute.For<ISender>();
        var controller = new StoreOrdersController(sender)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
        controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())], "test"));
        controller.HttpContext.Request.Headers["Idempotency-Key"] = "order-attempt-1";
        PlaceStoreOrderCommand? capturedCommand = null;
        sender.Send(Arg.Do<PlaceStoreOrderCommand>(command => capturedCommand = command), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<StoreOrderDto>("test")));

        await controller.PlaceOrder(
            new PlaceOrderRequest(Guid.NewGuid(), "Pickup", null, null, [new OrderLineRequest(Guid.NewGuid(), 1)]),
            CancellationToken.None);

        capturedCommand.Should().NotBeNull();
        capturedCommand!.IdempotencyKey.Should().Be("order-attempt-1");
    }

    [Fact]
    public async Task PlaceOrder_MapsIdempotencyPayloadConflictToConflictResponse()
    {
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<PlaceStoreOrderCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<StoreOrderDto>("IDEMPOTENCY_KEY_CONFLICT")));
        var controller = new StoreOrdersController(sender)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
        controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())], "test"));
        controller.HttpContext.Request.Headers["Idempotency-Key"] = "order-attempt-1";

        var response = await controller.PlaceOrder(
            new PlaceOrderRequest(Guid.NewGuid(), "Pickup", null, null, [new OrderLineRequest(Guid.NewGuid(), 1)]),
            CancellationToken.None);

        response.Should().BeOfType<ConflictObjectResult>();
        ((ProblemDetails)((ConflictObjectResult)response).Value!)
            .Extensions["code"].Should().Be("IDEMPOTENCY_KEY_CONFLICT");
    }
}