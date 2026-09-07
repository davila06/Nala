using FluentAssertions;
using PawTrack.Domain.ServiceProviders;

namespace PawTrack.UnitTests.ServiceProviders;

public sealed class ProviderIncidentDomainTests
{
    [Fact]
    public void Open_CreatesAuditableIncidentInOpenState()
    {
        var incident = ProviderIncident.Open(
            Guid.NewGuid(), Guid.NewGuid(), ProviderIncidentType.Welfare,
            "Mascota lesionada durante el servicio.", "evidence-001");

        incident.Status.Should().Be(ProviderIncidentStatus.Open);
        incident.Description.Should().Be("Mascota lesionada durante el servicio.");
        incident.EvidenceReference.Should().Be("evidence-001");
    }

    [Fact]
    public void Resolve_RequiresDecisionAndMovesIncidentToTerminalState()
    {
        var incident = ProviderIncident.Open(
            Guid.NewGuid(), Guid.NewGuid(), ProviderIncidentType.Policy,
            "Servicio no prestado.", null);

        incident.StartInvestigation(Guid.NewGuid());
        incident.Resolve(Guid.NewGuid(), "Reembolso aprobado y proveedor suspendido.");

        incident.Status.Should().Be(ProviderIncidentStatus.Resolved);
        incident.Resolution.Should().Be("Reembolso aprobado y proveedor suspendido.");
        incident.ResolvedAt.Should().NotBeNull();
    }
}