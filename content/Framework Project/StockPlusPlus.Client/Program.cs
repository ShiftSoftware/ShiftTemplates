using BitzArt.Blazor.Cookies;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ShiftSoftware.ShiftBlazor.Extensions;
using ShiftSoftware.ShiftEntity.Core.Extensions;
using ShiftSoftware.ShiftIdentity.Blazor.Extensions;
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

builder.Services.AddShiftIdentityBlazorClient(
    appId: "StockPlusPlus-Dev",
    baseUrl: shiftIdentityApiURL!,
    frontEndBaseUrl: shiftIdentityFrontEndURL!,
#if (internalShiftIdentityHosting)
    hostingType: ShiftSoftware.ShiftIdentity.Core.ShiftIdentityHostingTypes.Internal);
#else
    hostingType: ShiftSoftware.ShiftIdentity.Core.ShiftIdentityHostingTypes.External,
    externalIdentityApiUrl: shiftIdentityApiURL);
#endif

builder.Services.AddShiftIdentityDashboardBlazor(x =>
{
    x.Title = "StockPlusPlus";
});


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
