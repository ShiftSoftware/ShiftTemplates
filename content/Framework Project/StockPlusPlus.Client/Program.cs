using BitzArt.Blazor.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ShiftSoftware.ShiftBlazor.Extensions;
using ShiftSoftware.ShiftEntity.Core.Extensions;
using ShiftSoftware.ShiftIdentity.Dashboard.Blazor.Extensions;
using ShiftSoftware.TypeAuth.Blazor.Extensions;
using StockPlusPlus.Shared.ActionTrees;
using System.Globalization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

var baseUrl = builder.Configuration!.GetValue<string>("BaseURL");
var shiftIdentityApiURL = builder.Configuration.GetValue<string>("ShiftIdentityApi");
shiftIdentityApiURL ??= baseUrl; //Fallback to BaseURL if empty
var shiftIdentityFrontEndURL = builder.Configuration.GetValue<string>("ShiftIdentityFrontEnd");
shiftIdentityFrontEndURL ??= baseUrl; //Fallback to BaseURL if empty

builder.Services.AddScoped(sp =>
{
    return new HttpClient()
    {
        BaseAddress = new Uri(baseUrl!)
    };
});

builder.AddBlazorCookies();

builder.Services.AddShiftBlazor(config =>
{
    config.ShiftConfiguration = options =>
    {
        options.BaseAddress = baseUrl!;
        options.ExternalAddresses = new Dictionary<string, string?>
        {
            ["ShiftIdentityApi"] = shiftIdentityApiURL,
            ["StockPluPlus"] = baseUrl
        };
        options.UserListEndpoint = shiftIdentityApiURL.AddUrlPath("IdentityPublicUser");
#if (internalShiftIdentityHosting)
        options.AdditionalAssemblies = new[] { typeof(ShiftSoftware.ShiftIdentity.Dashboard.Blazor.ShiftIdentityDashboarBlazorMaker).Assembly };
#endif
        options.AddLanguage("en-US", "English", false)
               .AddLanguage("ar-IQ", "Arabic", true)
               .AddLanguage("en-US", "English RTL", true)
               .AddLanguage("ku-IQ", "Kurdish", true);
    };

});

builder.Services.AddScoped<AuthenticationStateProvider,
    ShiftSoftware.ShiftIdentity.Blazor.Providers.PersistentCookieAuthStateProvider>();
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddShiftIdentityDashboardBlazor(x =>
{
    x.Title = "StockPlusPlus";
    x.UseCookieAuth = true;
#if (internalShiftIdentityHosting)
    x.ShiftIdentityHostingType = ShiftSoftware.ShiftIdentity.Core.ShiftIdentityHostingTypes.Internal;
#else
    x.ShiftIdentityHostingType = ShiftSoftware.ShiftIdentity.Core.ShiftIdentityHostingTypes.External;
    x.ExternalIdentityApiUrl = shiftIdentityApiURL;
#endif
});

// ShiftIdentityLocalizer needed by Dashboard.Blazor components
builder.Services.AddTransient(x => new ShiftSoftware.ShiftIdentity.Core.Localization.ShiftIdentityLocalizer(
    x, typeof(ShiftSoftwareLocalization.Identity.Resource)));

// No-op IIdentityStore — cookie auth handles token storage
builder.Services.AddScoped<ShiftSoftware.ShiftIdentity.Blazor.IIdentityStore,
    ShiftSoftware.ShiftIdentity.Blazor.Services.NoOpIdentityStore>();


builder.Services.AddTypeAuth(x =>
    x
    .AddActionTree<ShiftSoftware.ShiftIdentity.Core.ShiftIdentityActions>()
    .AddActionTree<ShiftSoftware.ShiftEntity.Core.AzureStorageActionTree>()
#if (includeSampleApp)
    .AddActionTree<StockPlusPlusActionTree>()
#endif
);

var app = builder.Build();


var settingManager = app.Services.GetRequiredService<ShiftSoftware.ShiftBlazor.Services.SettingManager>();
await settingManager.Setup(true);

await app.RunAsync();
