using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Localization;
using ShiftSoftware.ShiftBlazor.Extensions;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.ShiftIdentity.Blazor.Extensions;
using ShiftSoftware.ShiftIdentity.Blazor.Handlers;
using ShiftSoftware.ShiftIdentity.Blazor.Providers;
using ShiftSoftware.ShiftIdentity.Dashboard.Blazor.Extensions;
using ShiftSoftware.TypeAuth.Blazor.Extensions;
using StockPlusPlus.Shared.ActionTrees;
using StockPlusPlus.Shared.DTOs.ProductBrand;
using StockPlusPlus.Shared.DTOs.ProductCategory;
using StockPlusPlus.Store;
using System.Net.Http.Json;

[assembly: RootNamespace("StockPlusPlus.Store")]
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp =>
{
    var httpClient = new HttpClient(sp.GetRequiredService<TokenMessageHandlerWithAutoRefresh>())
    {
        BaseAddress = new Uri(builder.Configuration!.GetValue<string>("BaseURL")!)
    };

    return httpClient;
});

var baseUrl = builder.Configuration!.GetValue<string>("BaseURL")!;

var shiftIdentityApiURL = builder.Configuration!.GetValue<string>("ShiftIdentityApi")!;
shiftIdentityApiURL = string.IsNullOrWhiteSpace(shiftIdentityApiURL) ? baseUrl : shiftIdentityApiURL; //Fallback to BaseURL if emtpy

var shiftIdentityFrontEndURL = builder.Configuration!.GetValue<string>("ShiftIdentityFrontEnd")!;
shiftIdentityFrontEndURL = string.IsNullOrWhiteSpace(shiftIdentityFrontEndURL) ? baseUrl : shiftIdentityFrontEndURL; //Fallback to BaseURL if emtpy


builder.Services.AddShiftIdentity("StockPlusPlus-Dev", shiftIdentityApiURL, shiftIdentityFrontEndURL);

builder.Services.AddShiftBlazor(config =>
{
    config.ShiftConfiguration = options =>
    {
        options.BaseAddress = baseUrl!;
        options.ExternalAddresses = new Dictionary<string, string?>
        {
            ["ShiftIdentityApi"] = shiftIdentityApiURL
        };
        options.ApiPath = "/api";
        options.ODataPath = "/odata";
        options.UserListEndpoint = shiftIdentityApiURL.AddUrlPath("IdentityPublicUser");
        options.AdditionalAssemblies = new[] { typeof(ShiftSoftware.ShiftIdentity.Dashboard.Blazor.ShiftIdentityDashboarBlazorMaker).Assembly };
        options.AddLanguage("en-US", "English")
               .AddLanguage("ar-IQ", "Arabic", true)
               .AddLanguage("en-US", "English RTL", true)
               .AddLanguage("ku-IQ", "Kurdish", true);
    };
});

builder.Services.AddShiftIdentityDashboardBlazor(x =>
{
    x.ShiftIdentityHostingType = ShiftSoftware.ShiftIdentity.Core.ShiftIdentityHostingTypes.External;
    x.LogoPath = "/img/shift-full.png";
    x.Title = "StockPlusPlus";
    x.DynamicTypeAuthActionExpander = async () =>
    {

    };

});

builder.Services.AddTypeAuth(x =>
    x
    .AddActionTree<ShiftSoftware.ShiftIdentity.Core.ShiftIdentityActions>()
    .AddActionTree<StockPlusPlusActionTree>()
    .AddActionTree<SystemActionTrees>()
);
var host = builder.Build();

await host.RefreshTokenAsync(50);

await host.RunAsync();
