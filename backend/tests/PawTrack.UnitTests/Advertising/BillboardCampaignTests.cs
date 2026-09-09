using NSubstitute;
using PawTrack.Domain.Advertising;

namespace PawTrack.UnitTests.Advertising;

public sealed class BillboardCampaignTests
{
    [Fact]
    public void Create_Campaign_PreservesPriority()
    {
        var billboard = Billboard.Create(
            Guid.NewGuid(), "VIP GPS", null, BillboardPlacement.Map,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(7),
            priority: 20);

        Assert.Equal(20, billboard.Priority);
    }

    [Fact]
    public async Task TrackDelivery_RejectsImpressionWhenVisitorReachedDailyFrequencyCap()
    {
        var repository = NSubstitute.Substitute.For<PawTrack.Application.Common.Interfaces.IBillboardRepository>();
        var unitOfWork = NSubstitute.Substitute.For<PawTrack.Application.Common.Interfaces.IUnitOfWork>();
        var billboard = Billboard.Create(Guid.NewGuid(), "GPS", null, BillboardPlacement.Map,
            DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(1),
            advertiserName: "GPS CR", frequencyCapPerDay: 1);
        billboard.SetImageUrl("https://example.cr/ad.jpg");
        billboard.Approve(Guid.NewGuid());
        billboard.Activate();
        repository.GetByIdAsync(billboard.Id, Arg.Any<CancellationToken>()).Returns(billboard);
        repository.CountImpressionsByVisitorTodayAsync(billboard.Id, "visitor-hash", Arg.Any<CancellationToken>()).Returns(1);
        var handler = new PawTrack.Application.Advertising.TrackBillboardDeliveryCommandHandler(repository, unitOfWork);

        var result = await handler.Handle(new PawTrack.Application.Advertising.TrackBillboardDeliveryCommand(
            billboard.Id, "Impression", "event-hash", "visitor-hash", null), CancellationToken.None);

        Assert.True(result.IsFailure);
        await repository.DidNotReceive().AddDeliveryEventAsync(Arg.Any<BillboardDeliveryEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Expire_MarksCampaignAsExpiredForRemovalFromDelivery()
    {
        var billboard = Billboard.Create(
            Guid.NewGuid(), "GPS", null, BillboardPlacement.Map,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(7));

        billboard.Expire();

        Assert.Equal(BillboardStatus.Expired, billboard.Status);
    }

    [Fact]
    public void UpdateCampaign_UpdatesCommercialTermsAndReturnsToDraft()
    {
        var billboard = Billboard.Create(
            Guid.NewGuid(), "GPS", "Original", BillboardPlacement.Map,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(7),
            advertiserName: "GPS CR", category: BillboardCategory.GpsAndIdentification);

        billboard.UpdateCampaign(
            "Nueva oferta", "Nueva descripción", "Ver oferta", "https://example.cr/oferta",
            DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow.AddDays(31), 40,
            "GPS Costa Rica", BillboardCategory.PetInsurance, "Heredia", "CTR-2026-001",
            50_000m, 3, true);

        Assert.Equal("GPS Costa Rica", billboard.AdvertiserName);
        Assert.Equal(BillboardCategory.PetInsurance, billboard.Category);
        Assert.Equal("Heredia", billboard.TargetCanton);
        Assert.Equal("CTR-2026-001", billboard.ContractReference);
        Assert.Equal(50_000m, billboard.BudgetCrc);
        Assert.Equal(3, billboard.FrequencyCapPerDay);
        Assert.True(billboard.IsCategoryExclusive);
        Assert.Equal(BillboardCampaignStatus.Draft, billboard.CampaignStatus);
    }

    [Fact]
    public void Approve_RejectsNonRecoveryCategoryForFeed()
    {
        var billboard = Billboard.Create(
            Guid.NewGuid(), "Alimento", null, BillboardPlacement.Feed,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(7),
            advertiserName: "Marca de alimento", category: BillboardCategory.FoodAndNutrition);

        var accepted = billboard.Approve(Guid.NewGuid());

        Assert.False(accepted);
        Assert.Equal(BillboardCampaignStatus.Rejected, billboard.CampaignStatus);
    }

    [Fact]
    public void Activate_RequiresApprovedCampaignAndCreative()
    {
        var billboard = Billboard.Create(
            Guid.NewGuid(), "GPS", null, BillboardPlacement.Map,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(7),
            advertiserName: "GPS CR", category: BillboardCategory.GpsAndIdentification);

        billboard.Activate();

        Assert.Equal(BillboardStatus.Draft, billboard.Status);
    }
}