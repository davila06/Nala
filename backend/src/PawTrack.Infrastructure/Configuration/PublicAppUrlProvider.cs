using Microsoft.Extensions.Configuration;
using PawTrack.Application.Common.Interfaces;

namespace PawTrack.Infrastructure.Configuration;

public sealed class PublicAppUrlProvider(IConfiguration configuration) : IPublicAppUrlProvider
{
    public string GetBaseUrl()
        => configuration["App:BaseUrl"]?.TrimEnd('/') ?? "https://pawtrack.cr";
}