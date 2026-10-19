using System.Text.Json;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using RichardSzalay.MockHttp;
using ShiftSoftware.ShiftBlazor.Banners;
using ShiftSoftware.TypeAuth.Core;
using StockPlusPlus.Shared.ActionTrees;
using StockPlusPlus.Web.Pages.Banners;
using Xunit;

namespace StockPlusPlus.Web.Tests;

/// <summary>
/// The sample's BannerLayout with the real wwwroot/banners.json: which banner shows where, and that the condition
/// names it uses are all ones BannerLayout knows.
/// </summary>
public class BannerLayoutTests : ShiftBlazorTestContext
{
    private const string RulesUrl = "http://localhost/banners.json";

    private readonly MockHttpMessageHandler api;
    private readonly WarningLogger logger = new();

    public BannerLayoutTests()
    {
        api = Services.AddMockHttpClient();
        Services.AddShiftBlazorBanners();
        Services.AddSingleton<ILogger<BannerLayout>>(logger);
        Services.AddSingleton<IWebAssemblyHostEnvironment>(new HostEnvironment("Development"));

        // Can write invoices; can only read products.
        Services.RemoveAll<ITypeAuthService>();
        var grants = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            [nameof(StockPlusPlusActionTree)] = new Dictionary<string, object>
            {
                [nameof(StockPlusPlusActionTree.Invoice)] = new[] { Access.Read, Access.Write },
                [nameof(StockPlusPlusActionTree.Product)] = new[] { Access.Read },
            }
        });
        Services.AddSingleton<ITypeAuthService>(new TypeAuthContextBuilder().AddAccessTree(grants)
            .AddActionTree<StockPlusPlusActionTree>().Build());
    }

    [Fact]
    public void TheSampleRulesShowOnTheirPages_AndUseOnlyKnownConditions()
    {
        api.When(HttpMethod.Get, RulesUrl).Respond("application/json", File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "banners.json")));
        var navigation = Services.GetRequiredService<NavigationManager>();

        navigation.NavigateTo("/InvoiceList");
        var cut = RenderLayout();
        cut.WaitForAssertion(() => Assert.Equal(["invoice-editors", "development-build"], BannerIds(cut)));
        Assert.Contains("page body", cut.Markup);

        navigation.NavigateTo("/ProductList");
        cut.WaitForAssertion(() => Assert.Equal(["product-viewers", "development-build"], BannerIds(cut)));

        Assert.Empty(logger.Warnings);
    }

    [Fact]
    public void AnUnknownCondition_IsNotMet_AndIsLoggedOnce()
    {
        api.When(HttpMethod.Get, RulesUrl).Respond("application/json", """
            { "rules": [ { "id": "typo", "paths": [ "/**" ], "include": [ "can-write:Invoce" ], "message": { "en": "Typo." } } ] }
            """);
        var navigation = Services.GetRequiredService<NavigationManager>();

        navigation.NavigateTo("/InvoiceList");
        var cut = RenderLayout();
        cut.WaitForAssertion(() => Assert.Single(logger.Warnings));

        navigation.NavigateTo("/ProductList");
        cut.WaitForAssertion(() => Assert.Contains("page body", cut.Markup));

        Assert.Empty(BannerIds(cut));
        var warning = Assert.Single(logger.Warnings);
        Assert.Contains("'can-write:Invoce'", warning);
        Assert.Contains("can-write:Invoice", warning);
    }

    private IRenderedComponent<BannerLayout> RenderLayout()
        => Render<BannerLayout>(p => p.Add(x => x.Body, (RenderFragment)(b => b.AddContent(0, "page body"))));

    private static List<string?> BannerIds(IRenderedComponent<BannerLayout> cut)
        => cut.FindAll("[data-banner-id]").Select(x => x.GetAttribute("data-banner-id")).ToList();

    private sealed class HostEnvironment(string environment) : IWebAssemblyHostEnvironment
    {
        public string Environment => environment;
        public string BaseAddress => "http://localhost/";
    }

    private sealed class WarningLogger : ILogger<BannerLayout>
    {
        public List<string> Warnings { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
                Warnings.Add(formatter(state, exception));
        }
    }
}
