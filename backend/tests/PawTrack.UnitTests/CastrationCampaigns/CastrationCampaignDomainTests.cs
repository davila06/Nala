using FluentAssertions;
using PawTrack.Domain.CastrationCampaigns;

namespace PawTrack.UnitTests.CastrationCampaigns;

public sealed class CastrationCampaignDomainTests
{
    private static CastrationCampaign CreateCampaign(int capacity = 2)
    {
        var reservationsOpenAt = DateTimeOffset.UtcNow.AddDays(1);
        return CastrationCampaign.Create(
            organizerUserId: Guid.NewGuid(),
            executingClinicId: Guid.NewGuid(),
            title: "Jornada de esterilizacion Cartago",
            venueLabel: "Centro Civico de Cartago",
            canton: "Cartago",
            latitude: 9.8644,
            longitude: -83.9194,
            startsAt: reservationsOpenAt.AddDays(14),
            endsAt: reservationsOpenAt.AddDays(14).AddHours(8),
            reservationsOpenAt: reservationsOpenAt,
            reservationsCloseAt: reservationsOpenAt.AddDays(12),
            capacity: capacity,
            basePriceCrc: 15_000m,
            consentVersion: "v1");
    }

    [Fact]
    public void Create_WithValidData_CreatesDraftCampaign()
    {
        var campaign = CreateCampaign(capacity: 25);

        campaign.Id.Version.Should().Be(7);
        campaign.Status.Should().Be(CastrationCampaignStatus.Draft);
        campaign.Capacity.Should().Be(25);
        campaign.ReservedCount.Should().Be(0);
        campaign.AvailableCapacity.Should().Be(25);
        campaign.BasePriceCrc.Should().Be(15_000m);
    }

    [Fact]
    public void Publish_FromDraft_ThrowsBecauseApprovalIsRequired()
    {
        var campaign = CreateCampaign();

        var act = campaign.Publish;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Campaign must be approved before publication.");
    }

    [Fact]
    public void Reserve_WhenCampaignIsNotPublished_Throws()
    {
        var campaign = CreateCampaign();

        var act = campaign.ReserveSlot;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Campaign is not accepting reservations.");
    }

    [Fact]
    public void Reserve_WhenCapacityIsExhausted_ThrowsAndDoesNotOverbook()
    {
        var campaign = CreateCampaign(capacity: 1);
        campaign.SubmitForApproval();
        campaign.Approve(Guid.NewGuid());
        campaign.Publish();
        campaign.ReserveSlot();

        var act = campaign.ReserveSlot;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Campaign capacity is exhausted.");
        campaign.ReservedCount.Should().Be(1);
        campaign.AvailableCapacity.Should().Be(0);
    }

    [Fact]
    public void CreateAppointment_CapturesEligibilityAndConsentSnapshots()
    {
        var campaign = CreateCampaign();
        var ownerId = Guid.NewGuid();
        var petId = Guid.NewGuid();

        var appointment = CastrationAppointment.Create(
            campaign.Id,
            petId,
            ownerId,
            scheduledAt: campaign.StartsAt.AddHours(1),
            eligibilitySnapshot: "{\"species\":\"Dog\",\"weightKg\":12.5,\"eligible\":true}",
            consentVersion: campaign.ConsentVersion,
            consentAcceptedAt: DateTimeOffset.UtcNow,
            basePriceCrc: campaign.BasePriceCrc,
            ivaAmountCrc: 1_950m,
            totalAmountCrc: 16_950m);

        appointment.Id.Version.Should().Be(7);
        appointment.Status.Should().Be(CastrationAppointmentStatus.Reserved);
        appointment.CampaignId.Should().Be(campaign.Id);
        appointment.PetId.Should().Be(petId);
        appointment.OwnerUserId.Should().Be(ownerId);
        appointment.EligibilitySnapshot.Should().Contain("\"eligible\":true");
        appointment.ConsentVersion.Should().Be("v1");
        appointment.TotalAmountCrc.Should().Be(16_950m);
    }

    [Fact]
    public void Appointment_ClinicalWorkflow_RequiresOrderedTransitions()
    {
        var campaign = CreateCampaign();
        var appointment = CastrationAppointment.Create(
            campaign.Id, Guid.NewGuid(), Guid.NewGuid(), campaign.StartsAt,
            "{\"eligible\":true}", "v1", DateTimeOffset.UtcNow, 10_000m, 0m, 10_000m);

        appointment.Confirm();
        appointment.CheckIn();
        appointment.Complete(Guid.NewGuid(), "Procedimiento sin complicaciones", "Control en 8 dias");

        appointment.Status.Should().Be(CastrationAppointmentStatus.Completed);
        appointment.ExecutingVeterinarianId.Should().NotBeEmpty();
        appointment.ClinicalOutcome.Should().Be("Procedimiento sin complicaciones");
    }

    [Fact]
    public void Appointment_CheckInBeforeConfirmation_Throws()
    {
        var campaign = CreateCampaign();
        var appointment = CastrationAppointment.Create(
            campaign.Id, Guid.NewGuid(), Guid.NewGuid(), campaign.StartsAt,
            "{\"eligible\":true}", "v1", DateTimeOffset.UtcNow, 0m, 0m, 0m);

        var act = appointment.CheckIn;

        act.Should().Throw<InvalidOperationException>();
    }
}
