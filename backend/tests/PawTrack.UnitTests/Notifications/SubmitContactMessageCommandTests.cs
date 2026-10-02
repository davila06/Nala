using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Notifications;

namespace PawTrack.UnitTests.Notifications;

public sealed class SubmitContactMessageCommandTests
{
    [Fact]
    public void Validator_rejects_invalid_email_topic_and_message_length()
    {
        var validator = new SubmitContactMessageCommandValidator();
        var command = new SubmitContactMessageCommand("Ana", "not-an-email", "Unsupported topic", "Short", "");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Select(error => error.PropertyName)
            .Should().Contain(["Email", "Topic", "Message"]);
    }

    [Fact]
    public async Task Valid_command_sends_to_contact_sender()
    {
        var emailSender = Substitute.For<IContactEmailSender>();
        emailSender.SendAsync(Arg.Any<ContactEmailMessage>(), Arg.Any<CancellationToken>()).Returns(true);
        var handler = new SubmitContactMessageCommandHandler(emailSender);
        var command = new SubmitContactMessageCommand(
            "Ana Pérez", "ana@example.cr", "Consulta general", "Necesito ayuda con mi cuenta.", "");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await emailSender.Received(1).SendAsync(
            Arg.Is<ContactEmailMessage>(message =>
                message.ReplyTo == "ana@example.cr" && message.Topic == "Consulta general"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Honeypot_submission_returns_accepted_without_sending()
    {
        var emailSender = Substitute.For<IContactEmailSender>();
        var handler = new SubmitContactMessageCommandHandler(emailSender);
        var command = new SubmitContactMessageCommand(
            "Bot", "bot@example.cr", "Consulta general", "Mensaje de prueba suficientemente largo.", "https://spam.test");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await emailSender.DidNotReceiveWithAnyArgs().SendAsync(default!, default);
    }

    [Fact]
    public async Task Provider_failure_returns_failure_to_api()
    {
        var emailSender = Substitute.For<IContactEmailSender>();
        emailSender.SendAsync(Arg.Any<ContactEmailMessage>(), Arg.Any<CancellationToken>()).Returns(false);
        var handler = new SubmitContactMessageCommandHandler(emailSender);
        var command = new SubmitContactMessageCommand(
            "Ana", "ana@example.cr", "Consulta general", "Necesito ayuda con mi cuenta.", "");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
