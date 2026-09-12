using System.Security.Claims;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using PawTrack.API.Controllers;
using PawTrack.Application.Clinics.Commands.PerformClinicScan;
using PawTrack.Application.Clinics.DTOs;
using PawTrack.Application.Clinics.Queries.GetMyClinic;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Clinics;
using PawTrack.Domain.Common;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.UnitTests.Clinics.Controllers;

public sealed class PetLookupControllerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly IClinicRepository _clinicRepo = Substitute.For<IClinicRepository>();
    private readonly ISubscriptionRepository _subRepo = Substitute.For<ISubscriptionRepository>();

    private PetLookupController CreateSut()
    {
        var controller = new PetLookupController(_sender, _clinicRepo, _subRepo);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext(),
        };
        return controller;
    }

    [Fact]
    public async Task Lookup_WhenNoChipAndNoQr_ReturnsBadRequest()
    {
        var sut = CreateSut();
        var result = await sut.Lookup(null, null, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Lookup_WhenNoAuthAndNoWidgetHeader_ReturnsUnauthorized()
    {
        var sut = CreateSut();
        var result = await sut.Lookup("985141000123456", null, CancellationToken.None);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Lookup_WhenValidWidgetHeaderAndClinicPartner_PerformsScanSuccessfully()
    {
        var clinicId = Guid.NewGuid();
        var clinic = Clinic.Create(Guid.NewGuid(), "Clinica Veterinaria Moderna", "SENASA-999", "San Jose", 9.93m, -84.08m, "info@moderna.cr");
        clinic.Activate();

        _clinicRepo.GetByIdAsync(clinicId, Arg.Any<CancellationToken>()).Returns(clinic);

        var sub = Subscription.CreateForClinic(clinicId, Guid.NewGuid(), SubscriptionTier.ClinicPartner, "REF-12345", 35000m);
        sub.Activate(1);
        _subRepo.GetActiveForClinicAsync(clinicId, Arg.Any<CancellationToken>()).Returns(sub);

        var scanResult = new ClinicScanResultDto(
            ScanId: Guid.NewGuid(),
            Matched: true,
            PetId: Guid.NewGuid(),
            PetName: "Luna",
            PetPhotoUrl: "https://storage.blob.core.windows.net/pets/luna.jpg",
            OwnerName: "Maria R.",
            OwnerNotified: true,
            PetSpecies: "Dog");

        _sender.Send(Arg.Any<PerformClinicScanCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(scanResult));

        var sut = CreateSut();
        sut.HttpContext.Request.Headers["X-Widget-Clinic"] = clinicId.ToString();

        var result = await sut.Lookup("985141000123456", null, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(scanResult);
    }
}
