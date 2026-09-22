using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Clinics.Commands.ManageWidgetDomain;
using PawTrack.Application.Clinics.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Clinics;

namespace PawTrack.UnitTests.Clinics;

public sealed class ManageClinicWidgetDomainCommandTests
{
    [Fact]
    public async Task Add_when_five_active_domains_exist_returns_failure()
    {
        var clinicId = Guid.NewGuid();
        var repository = Substitute.For<IClinicWidgetDomainRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        repository.GetForClinicAsync(clinicId, Arg.Any<CancellationToken>()).Returns(
            Enumerable.Range(1, 5)
                .Select(index => ClinicWidgetDomain.Create(clinicId, $"clinic-{index}.example.com"))
                .ToList());

        var result = await new AddClinicWidgetDomainCommandHandler(repository, unitOfWork)
            .Handle(new AddClinicWidgetDomainCommand(clinicId, "new.example.com"), default);

        result.IsFailure.Should().BeTrue();
        await repository.DidNotReceive().AddAsync(Arg.Any<ClinicWidgetDomain>(), Arg.Any<CancellationToken>());
    }
}
