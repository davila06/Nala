using FluentAssertions;
using NSubstitute;
using PawTrack.Application.CastrationCampaigns.Commands.ReserveCastrationAppointment;
using PawTrack.Application.CastrationCampaigns.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.CastrationCampaigns;
using PawTrack.Domain.Pets;

namespace PawTrack.UnitTests.CastrationCampaigns;

public sealed class ReserveCastrationAppointmentCommandHandlerTests
{
    private readonly ICastrationCampaignRepository _campaignRepository = Substitute.For<ICastrationCampaignRepository>();
    private readonly ICastrationAppointmentRepository _appointmentRepository = Substitute.For<ICastrationAppointmentRepository>();
    private readonly IPetRepository _petRepository = Substitute.For<IPetRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private ReserveCastrationAppointmentCommandHandler CreateSut() =>
        new(_campaignRepository, _appointmentRepository, _petRepository, _unitOfWork);

    private static CastrationCampaign PublishedCampaign()
    {
        var now = DateTimeOffset.UtcNow;
        var campaign = CastrationCampaign.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Jornada", "Centro Civico", "Cartago",
            9.8644, -83.9194, now.AddDays(10), now.AddDays(10).AddHours(8),
            now.AddHours(-1), now.AddDays(8), 2, 15_000m, "v1");
        campaign.SubmitForApproval();
        campaign.Approve(Guid.NewGuid());
        campaign.Publish();
        return campaign;
    }

    private static ReserveCastrationAppointmentCommand Command(Guid ownerId, Guid petId, Guid campaignId) => new(
        RequestingUserId: ownerId,
        CampaignId: campaignId,
        PetId: petId,
        ScheduledAt: DateTimeOffset.UtcNow.AddDays(10).AddHours(1),
        WeightKg: 12.5m,
        ConfirmsFastingInstructions: true,
        IsPregnant: false,
        IsInHeat: false,
        ConsentAccepted: true,
        ConsentVersion: "v1",
        RequiresInvoice: true);

    [Fact]
    public async Task Handle_WhenPetBelongsToAnotherOwner_ReturnsFailure()
    {
        var ownerId = Guid.NewGuid();
        var pet = Pet.Create(Guid.NewGuid(), "Luna", PetSpecies.Dog, null, null);
        var campaign = PublishedCampaign();
        _campaignRepository.GetByIdAsync(campaign.Id, Arg.Any<CancellationToken>()).Returns(campaign);
        _petRepository.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);

        var result = await CreateSut().Handle(Command(ownerId, pet.Id, campaign.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Pet does not belong to the requesting user.");
    }

    [Fact]
    public async Task Handle_WhenEligible_ReservesCapacityAndPersistsAppointmentWithIva()
    {
        var ownerId = Guid.NewGuid();
        var pet = Pet.Create(ownerId, "Luna", PetSpecies.Dog, null, null);
        var campaign = PublishedCampaign();
        _campaignRepository.GetByIdAsync(campaign.Id, Arg.Any<CancellationToken>()).Returns(campaign);
        _petRepository.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        _appointmentRepository.ExistsForPetAsync(campaign.Id, pet.Id, Arg.Any<CancellationToken>()).Returns(false);

        var result = await CreateSut().Handle(Command(ownerId, pet.Id, campaign.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.BasePriceCrc.Should().Be(15_000m);
        result.Value.IvaAmountCrc.Should().Be(1_950m);
        result.Value.TotalAmountCrc.Should().Be(16_950m);
        campaign.ReservedCount.Should().Be(1);
        await _appointmentRepository.Received(1).AddAsync(
            Arg.Is<CastrationAppointment>(x => x.PetId == pet.Id && x.EligibilitySnapshot.Contains("12.5")),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
