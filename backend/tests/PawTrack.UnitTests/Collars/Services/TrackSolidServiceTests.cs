using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PawTrack.Infrastructure.Collars;

namespace PawTrack.UnitTests.Collars.Services;

public sealed class TrackSolidServiceTests
{
    [Fact]
    public async Task GetBatchPositionsAsync_WhenNotConfigured_ReturnsEmptyWithoutCallingHttp()
    {
        var config = new ConfigurationBuilder().Build();
        var httpFactoryMock = Substitute.For<IHttpClientFactory>();
        var sut = new TrackSolidService(httpFactoryMock, config, NullLogger<TrackSolidService>.Instance);

        var result = await sut.GetBatchPositionsAsync(["865123045678901"]);

        result.Should().BeEmpty();
        httpFactoryMock.DidNotReceive().CreateClient(Arg.Any<string>());
    }

    [Fact]
    public async Task GetBatchPositionsAsync_WhenSuccessResponse_ParsesPositionsCorrectly()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TrackSolid:AppKey"] = "test-app-key",
                ["TrackSolid:AppSecret"] = "test-secret-12345",
                ["TrackSolid:ApiUrl"] = "https://mock.tracksolidpro.com/route/rest",
            })
            .Build();

        var jsonResponse = """
        {
            "code": 0,
            "message": "success",
            "result": [
                {
                    "imei": "865123045678901",
                    "lat": 9.93215,
                    "lng": -84.08532,
                    "gpsTime": "2026-09-11 14:30:00",
                    "battery": 82,
                    "accuracy": 8,
                    "online": true
                }
            ]
        }
        """;

        var handler = new RecordingHandler(_ => new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(jsonResponse, System.Text.Encoding.UTF8, "application/json"),
        });

        var httpClient = new HttpClient(handler);
        var httpFactoryMock = Substitute.For<IHttpClientFactory>();
        httpFactoryMock.CreateClient("TrackSolid").Returns(httpClient);

        var sut = new TrackSolidService(httpFactoryMock, config, NullLogger<TrackSolidService>.Instance);

        var result = await sut.GetBatchPositionsAsync(["865123045678901"]);

        result.Should().ContainKey("865123045678901");
        var pos = result["865123045678901"];
        pos.Lat.Should().Be(9.93215);
        pos.Lng.Should().Be(-84.08532);
        pos.BatteryPercent.Should().Be(82);
        pos.AccuracyMeters.Should().Be(8);
        pos.IsOnline.Should().BeTrue();
    }

    [Fact]
    public async Task GetBatchPositionsAsync_WhenApiReturnsError_ReturnsEmptyGracefully()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TrackSolid:AppKey"] = "test-app-key",
                ["TrackSolid:AppSecret"] = "test-secret-12345",
            })
            .Build();

        var jsonResponse = """
        {
            "code": 10001,
            "message": "Sign invalid",
            "result": null
        }
        """;

        var handler = new RecordingHandler(_ => new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(jsonResponse, System.Text.Encoding.UTF8, "application/json"),
        });

        var httpClient = new HttpClient(handler);
        var httpFactoryMock = Substitute.For<IHttpClientFactory>();
        httpFactoryMock.CreateClient("TrackSolid").Returns(httpClient);

        var sut = new TrackSolidService(httpFactoryMock, config, NullLogger<TrackSolidService>.Instance);

        var result = await sut.GetBatchPositionsAsync(["865123045678901"]);

        result.Should().BeEmpty();
    }

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(responder(request));
        }
    }
}
