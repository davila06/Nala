using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using PawTrack.API.Controllers;
using PawTrack.Application.Notifications;
using PawTrack.Domain.Common;

namespace PawTrack.UnitTests.Notifications;

public sealed class PublicContactControllerTests
{
    [Fact]
    public async Task Accepted_submission_returns_202()
    {
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<SubmitContactMessageCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(true));
        var controller = new PublicContactController(sender);

        var response = await controller.Submit(
            new SubmitContactMessageRequest("Ana", "ana@example.cr", "Consulta general", "Mensaje de prueba suficientemente largo.", ""),
            CancellationToken.None);

        response.Should().BeOfType<AcceptedResult>().Which.StatusCode.Should().Be(202);
    }

    [Fact]
    public async Task Provider_failure_returns_503_without_claiming_acceptance()
    {
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<SubmitContactMessageCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<bool>("provider unavailable"));
        var controller = new PublicContactController(sender);

        var response = await controller.Submit(
            new SubmitContactMessageRequest("Ana", "ana@example.cr", "Consulta general", "Mensaje de prueba suficientemente largo.", ""),
            CancellationToken.None);

        response.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(503);
    }
}
