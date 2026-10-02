namespace PawTrack.Application.Common.Interfaces;

public sealed record ContactEmailMessage(string Name, string ReplyTo, string Topic, string Message);

public interface IContactEmailSender
{
    Task<bool> SendAsync(ContactEmailMessage message, CancellationToken cancellationToken = default);
}
