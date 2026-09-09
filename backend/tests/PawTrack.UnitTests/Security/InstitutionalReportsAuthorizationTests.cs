using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using PawTrack.API.Controllers;
using PawTrack.Application.Regulatory.Dtos;
using PawTrack.Application.Regulatory.Queries;
using PawTrack.Domain.Common;
using PawTrack.Domain.Regulatory;
using System.Security.Claims;

namespace PawTrack.UnitTests.Security;

public sealed class InstitutionalReportsAuthorizationTests
{
    [Fact]
    public async Task Catalog_AdminScope_IsForbiddenForMunicipalityRole()
    {
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<GetReportCatalogQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<ReportDefinitionDto>>([]));
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Features:RegulatoryReportsEnabled"] = "true" })
            .Build();
        var controller = new InstitutionalReportsController(sender, configuration)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.Role, "Municipality")], "test")),
                },
            },
        };

        var result = await controller.GetCatalog(ExportScope.Admin, CancellationToken.None);

        result.Should().BeOfType<ForbidResult>();
        await sender.DidNotReceive().Send(Arg.Any<GetReportCatalogQuery>(), Arg.Any<CancellationToken>());
    }
}