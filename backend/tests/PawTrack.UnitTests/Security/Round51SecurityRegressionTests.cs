using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using PawTrack.API.Controllers;
using PawTrack.Application.Clinics.Commands.RegisterClinic;
using PawTrack.Application.Clinics.DTOs;
using PawTrack.Domain.Common;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// Round-51 security regression tests.
///
/// Gap: <c>POST /api/clinics/register</c> returns HTTP 422 when the supplied
/// contact email is already registered:
///
///   <code>
///   // RegisterClinicCommandHandler
///   var emailInUse = await userRepository.ExistsByEmailAsync(request.ContactEmail, ...);
///   if (emailInUse)
///       return Result.Failure&lt;ClinicDto&gt;("That email address is already associated with an account.");
///
///   // ClinicsController
///   if (result.IsFailure)
///       return UnprocessableEntity(new ProblemDetails { Detail = string.Join("; ", result.Errors) });
///   </code>
///
/// An attacker can probe <c>POST /api/clinics/register</c> with arbitrary email
/// addresses and, from the distinct 422 / 422-different-message response, determine
/// whether that address has a clinic account.  Unlike the <c>AuthController</c>
/// which always returns 201 on register regardless of email existence, the clinic
/// endpoint leaks account existence through the error response body.
///
/// Fix:
///   When the handler returns the canonical email-conflict error, the controller
///   must return HTTP 201 with a generic confirmation message — identical to the
///   success response — so the caller cannot distinguish a new registration from
///   a duplicate one.  All other failure reasons (duplicate license, validation
///   errors) continue to return 422.
/// </summary>
public sealed class Round51SecurityRegressionTests
{
    private static ClinicsController BuildController(ISender sender)
    {
        var controller = new ClinicsController(sender);
        controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext(),
        };
        return controller;
    }

    private static RegisterClinicRequest ValidRequest() => new(
        "Clínica Las Palmas",
        "SEN-2024-00123",
        "San José, Costa Rica",
        9.9281m, -84.0907m,
        "clinic@example.com",
        "SecurePass1!");

    [Fact]
    public async Task Register_WhenEmailAlreadyInUse_Returns201NotRevealingConflict()
    {
        // Arrange — handler reports email conflict
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<RegisterClinicCommand>(), Arg.Any<CancellationToken>())
              .Returns(Result.Failure<ClinicDto>(
                  RegisterClinicCommand.DuplicateEmailError));

        var controller = BuildController(sender);

        // Act
        var result = await controller.Register(ValidRequest(), CancellationToken.None);

        // Assert — 201, not 422; response body must NOT contain the email-conflict message
        result.Should().BeOfType<CreatedResult>(
            "when the email is already registered the controller must return 201 " +
            "using the same code as a successful registration, preventing email enumeration");

        var created = (CreatedResult)result;
        var body = System.Text.Json.JsonSerializer.Serialize(created.Value);
        body.Should().NotContain("email",
            "the 201 response body must not contain any email-related detail " +
            "that could reveal whether the address was already registered");
    }

    [Fact]
    public async Task Register_WhenDuplicateLicense_Returns422WithDetails()
    {
        // Duplicate license is NOT an email-enumeration risk — no PII disclosed
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<RegisterClinicCommand>(), Arg.Any<CancellationToken>())
              .Returns(Result.Failure<ClinicDto>(
                  "A clinic with that SENASA license number is already registered."));

        var controller = BuildController(sender);

        var result = await controller.Register(ValidRequest(), CancellationToken.None);

        result.Should().BeOfType<UnprocessableEntityObjectResult>(
            "duplicate SENASA license is a business rule violation that must be " +
            "surfaced to the caller — it does not disclose user PII");
    }

    [Fact]
    public async Task Register_WhenSuccessful_Returns201()
    {
        var sender = Substitute.For<ISender>();
        var dto = new ClinicDto(Guid.NewGuid(), "Las Palmas", "SEN-2024-00123",
            "San José", 9.93m, -84.08m, "clinic@example.com",
            "Pending", DateTimeOffset.UtcNow);
        sender.Send(Arg.Any<RegisterClinicCommand>(), Arg.Any<CancellationToken>())
              .Returns(Result.Success(dto));

        var controller = BuildController(sender);

        var result = await controller.Register(ValidRequest(), CancellationToken.None);

        result.Should().BeOfType<CreatedResult>();
    }
}
