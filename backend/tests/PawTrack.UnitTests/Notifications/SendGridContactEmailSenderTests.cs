using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Infrastructure.Notifications;

namespace PawTrack.UnitTests.Notifications;

public sealed class SendGridContactEmailSenderTests
{
    [Fact]
    public async Task Sends_to_configured_inbox_with_visitor_as_reply_to()
    {
        var handler = new RecordingHandler(HttpStatusCode.Accepted);
        var sender = CreateSender(handler);

        var accepted = await sender.SendAsync(
            new ContactEmailMessage("Ana Pérez", "ana@example.cr", "Consulta general", "Necesito ayuda con mi cuenta."),
            CancellationToken.None);

        accepted.Should().BeTrue();
        handler.Request.Should().NotBeNull();
        handler.Request!.RequestUri!.AbsoluteUri.Should().Be("https://api.sendgrid.com/v3/mail/send");
        handler.Request.Headers.Authorization.Should().Be(new AuthenticationHeaderValue("Bearer", "test-api-key"));
        using var payload = JsonDocument.Parse(handler.Body!);
        var root = payload.RootElement;
        root.GetProperty("personalizations")[0].GetProperty("to")[0].GetProperty("email").GetString()
            .Should().Be("support@pawtrack.test");
        root.GetProperty("reply_to").GetProperty("email").GetString().Should().Be("ana@example.cr");
        root.GetProperty("from").GetProperty("email").GetString().Should().Be("noreply@pawtrack.test");
        root.GetProperty("subject").GetString().Should().Be("Contacto PawTrack CR · Consulta general");
        root.GetProperty("content")[0].GetProperty("type").GetString().Should().Be("text/plain");
        root.GetProperty("content")[0].GetProperty("value").GetString().Should().Contain("Necesito ayuda con mi cuenta.");
        root.GetProperty("tracking_settings").GetProperty("click_tracking").GetProperty("enable").GetBoolean()
            .Should().BeFalse();
        root.GetProperty("tracking_settings").GetProperty("open_tracking").GetProperty("enable").GetBoolean()
            .Should().BeFalse();
    }

    [Fact]
    public async Task Missing_credentials_fail_closed_without_requesting_provider()
    {
        var handler = new RecordingHandler(HttpStatusCode.Accepted);
        var sender = new SendGridContactEmailSender(new FixedClientFactory(handler),
            new ConfigurationBuilder().Build(), NullLogger<SendGridContactEmailSender>.Instance);

        var accepted = await sender.SendAsync(
            new ContactEmailMessage("Ana", "ana@example.cr", "Consulta general", "Mensaje de prueba suficientemente largo."),
            CancellationToken.None);

        accepted.Should().BeFalse();
        handler.Calls.Should().Be(0);
    }

    [Fact]
    public async Task Provider_failure_is_reported_as_not_accepted()
    {
        var handler = new RecordingHandler(HttpStatusCode.ServiceUnavailable);
        var sender = CreateSender(handler);

        var accepted = await sender.SendAsync(
            new ContactEmailMessage("Ana", "ana@example.cr", "Consulta general", "Mensaje de prueba suficientemente largo."),
            CancellationToken.None);

        accepted.Should().BeFalse();
    }

    private static SendGridContactEmailSender CreateSender(RecordingHandler handler)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["SendGrid:ApiKey"] = "test-api-key",
            ["SendGrid:FromEmail"] = "noreply@pawtrack.test",
            ["SendGrid:FromName"] = "PawTrack Test",
            ["Contact:ToEmail"] = "support@pawtrack.test",
        }).Build();

        return new SendGridContactEmailSender(new FixedClientFactory(handler), configuration,
            NullLogger<SendGridContactEmailSender>.Instance);
    }

    private sealed class FixedClientFactory(RecordingHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class RecordingHandler(HttpStatusCode status) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public HttpRequestMessage? Request { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Calls++;
            Request = request;
            Body = await request.Content!.ReadAsStringAsync(ct);
            return new HttpResponseMessage(status);
        }
    }
}
