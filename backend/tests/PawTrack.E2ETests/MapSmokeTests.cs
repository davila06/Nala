using Microsoft.Playwright;

namespace PawTrack.E2ETests;

public sealed class MapSmokeTests : IAsyncLifetime
{
    private IPlaywright playwright = null!;
    private IBrowser browser = null!;
    private IPage page = null!;

    private string FrontendUrl =>
        Environment.GetEnvironmentVariable("PAWTRACK_FRONTEND_URL") ?? "http://localhost:5173";

    public async Task InitializeAsync()
    {
        playwright = await Playwright.CreateAsync();
        browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
        });
        page = await browser.NewPageAsync();
    }

    [Fact]
    public async Task PublicMap_LoadsEventsWithoutVisibleApiError()
    {
        await page.GotoAsync($"{FrontendUrl.TrimEnd('/')}/map", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 30_000,
        });

        await Assertions.Expect(page.GetByText("PawTrack — Mapa en vivo")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("error al cargar")).Not.ToBeVisibleAsync();
        await Assertions.Expect(page.Locator("body")).ToContainTextAsync("eventos");
    }

    public async Task DisposeAsync()
    {
        await browser.DisposeAsync();
        playwright.Dispose();
    }
}
