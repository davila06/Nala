using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using PawTrack.API.Controllers;
using PawTrack.Application.Adoptions;
using PawTrack.Application.Common;
using PawTrack.Domain.Adoptions;
using PawTrack.Domain.Common;
using PawTrack.Domain.Pets;

namespace PawTrack.UnitTests.Adoptions;

public sealed class AdoptionsControllerTests
{
    [Fact]
    public async Task GetAnimalsForMap_ForwardsFiltersAndUsesHardCap()
    {
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<GetAdoptablePetsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new PagedResult<AdoptablePetDto>([], 0, 1, 500)));
        var controller = new AdoptionsController(sender);

        var result = await controller.GetAnimalsForMap(
            PetSpecies.Dog,
            PetSize.Medium,
            AgeCategory.Adult,
            true,
            true,
            true,
            false,
            9.9281,
            -84.0907,
            25,
            CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        await sender.Received(1).Send(
            Arg.Is<GetAdoptablePetsQuery>(query =>
                query.Species == PetSpecies.Dog &&
                query.Size == PetSize.Medium &&
                query.AgeCategory == AgeCategory.Adult &&
                query.IsVaccinated == true &&
                query.IsSterilized == true &&
                query.OkWithKids == true &&
                query.OkWithDogs == false &&
                query.NearLat == 9.9281 &&
                query.NearLng == -84.0907 &&
                query.RadiusKm == 25 &&
                query.Page == 1 &&
                query.PageSize == 500),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAnimalsForMap_WithIncompleteCoordinates_ReturnsBadRequest()
    {
        var sender = Substitute.For<ISender>();
        var controller = new AdoptionsController(sender);

        var result = await controller.GetAnimalsForMap(
            null, null, null, null, null, null, null,
            9.9281, null, 25, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        await sender.DidNotReceive().Send(
            Arg.Any<GetAdoptablePetsQuery>(),
            Arg.Any<CancellationToken>());
    }
}