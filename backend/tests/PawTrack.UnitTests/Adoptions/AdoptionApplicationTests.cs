using FluentAssertions;
using PawTrack.Domain.Adoptions;

namespace PawTrack.UnitTests.Adoptions;

public sealed class AdoptionApplicationTests
{
    private static AdoptionApplication Make() =>
        AdoptionApplication.Create(Guid.NewGuid(), Guid.NewGuid(), "Tengo patio y experiencia con perros");

    [Fact]
    public void NewApplication_HasPendingStatus() =>
        Make().Status.Should().Be(ApplicationStatus.Pending);

    [Fact]
    public void Approve_SetsApprovedAndTimestamp()
    {
        var app = Make();
        app.Approve("Perfil excelente");
        app.Status.Should().Be(ApplicationStatus.Approved);
        app.ReviewNote.Should().Be("Perfil excelente");
        app.ReviewedAt.Should().NotBeNull();
    }

    [Fact]
    public void Reject_WithNote_SetsNote()
    {
        var app = Make();
        app.Reject("No cumple los requisitos de espacio");
        app.Status.Should().Be(ApplicationStatus.Rejected);
        app.ReviewNote.Should().Be("No cumple los requisitos de espacio");
    }

    [Fact]
    public void Withdraw_FromPending_Succeeds()
    {
        var app = Make();
        app.Withdraw();
        app.Status.Should().Be(ApplicationStatus.Withdrawn);
    }

    [Fact]
    public void Withdraw_FromUnderReview_Succeeds()
    {
        var app = Make();
        app.StartReview();
        app.Withdraw();
        app.Status.Should().Be(ApplicationStatus.Withdrawn);
    }

    [Fact]
    public void Withdraw_FromApproved_Throws()
    {
        var app = Make();
        app.Approve();
        var act = () => app.Withdraw();
        act.Should().Throw<InvalidOperationException>();
    }
}
