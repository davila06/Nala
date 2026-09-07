using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.ServiceProviders;
using PawTrack.Domain.Common;
using PawTrack.Domain.ServiceProviders;

namespace PawTrack.UnitTests.ServiceProviders;

public sealed class DeactivateServiceAvailabilityRuleCommandHandlerTests
{
    [Fact]
    public async Task Handle_RuleOwnedByAnotherProvider_ReturnsFailure()
    {
        var repository = Substitute.For<IServiceProviderRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var provider = ServiceProvider.Create(Guid.NewGuid(), "Grooming", "Cuidado", ServiceProviderCategory.Groomer, "Heredia", 10m, -84m, "owner@example.cr");
        var rule = ServiceAvailabilityRule.Create(Guid.NewGuid(), DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0));
        repository.GetByUserIdAsync(provider.UserId, Arg.Any<CancellationToken>()).Returns(provider);
        repository.GetAvailabilityRuleByIdAsync(rule.Id, Arg.Any<CancellationToken>()).Returns(rule);

        var handler = new DeactivateServiceAvailabilityRuleCommandHandler(repository, unitOfWork);
        var result = await handler.Handle(new DeactivateServiceAvailabilityRuleCommand(provider.UserId, rule.Id), default);

        result.IsFailure.Should().BeTrue();
        rule.IsActive.Should().BeTrue();
    }
}