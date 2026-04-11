using Microsoft.Extensions.Configuration;
using PawTrack.Application.Common.Interfaces;

namespace PawTrack.Infrastructure.Bot;

internal sealed class WhatsAppSettings(IConfiguration configuration) : IWhatsAppSettings
{
    public string? VerifyToken => configuration["WhatsApp:VerifyToken"];
}
