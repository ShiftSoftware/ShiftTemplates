using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Localization;
using ShiftSoftware.ShiftBlazor.Extensions;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftIdentity.Blazor.Extensions;
using ShiftSoftware.ShiftIdentity.Blazor.Handlers;
using ShiftSoftware.TypeAuth.Blazor.Extensions;
using StockPlusPlus.Web;
using System.Globalization;
#if (internalShiftIdentityHosting)
using ShiftSoftware.ShiftIdentity.Dashboard.Blazor.Extensions;
#endif
#if (includeSampleApp)
using StockPlusPlus.Shared.ActionTrees;
#endif
#if (includeSampleApp && internalShiftIdentityHosting)
using System.Net.Http.Json;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using StockPlusPlus.Shared.DTOs.Product.ProductBrand;
using StockPlusPlus.Shared.DTOs.Product.ProductCategory;
#endif

[assembly: RootNamespace("StockPlusPlus.Web")]
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
#if (internalShiftIdentityHosting)
        options.AdditionalAssemblies = new[] { typeof(ShiftSoftware.ShiftIdentity.Dashboard.Blazor.ShiftIdentityDashboarBlazorMaker).Assembly };
#endif
        options.AddLanguage("en-US", "English")
               .AddLanguage("ar-IQ", "Arabic", true)
               .AddLanguage("en-US", "English RTL", true)
               .AddLanguage("ku-IQ", "Kurdish", true);
    };
});

builder.Services.AddShiftIdentity("StockPlusPlus-Dev", shiftIdentityApiURL, shiftIdentityFrontEndURL);

#if (internalShiftIdentityHosting)
builder.Services.AddShiftIdentityDashboardBlazor(x =>
{
    x.ShiftIdentityHostingType = ShiftSoftware.ShiftIdentity.Core.ShiftIdentityHostingTypes.External;
    x.LogoPath = "/img/shift-full.png";
    x.Title = "StockPlusPlus";
    x.DynamicTypeAuthActionExpander = async () =>
    {
#if (includeSampleApp)
        var httpService = builder.Services.BuildServiceProvider().GetRequiredService<HttpClient>();

        ODataDTO<ProductBrandListDTO>? brands = null!;
        ODataDTO<ProductCategoryListDTO>? categories = null!;

        await Task.WhenAll(new List<Task>
        {
            Task.Run(async () => { brands = await httpService.GetFromJsonAsync<ODataDTO<ProductBrandListDTO>>("/odata/Brand"); }),
            Task.Run(async () => { categories = await httpService.GetFromJsonAsync<ODataDTO<ProductCategoryListDTO>>("/odata/ProductCategory"); })
        });

        StockActionTrees.DataLevelAccess.ProductBrand.Expand(brands!.Value.Select(x => new KeyValuePair<string, string>(x.ID!, x.Name!)).ToList());

        StockActionTrees.DataLevelAccess.ProductCategory.Expand(categories!.Value.Select(x => new KeyValuePair<string, string>(x.ID!, x.Name!)).ToList());
#endif
    };

#if (includeSampleApp)
    x.AddCompanyCustomField("SomeExternalLink", "Some External Link")
    .AddCompanyBranchCustomField("Username", "User Name")
    .AddCompanyBranchCustomField("Password", true);
#endif
});
#endif

builder.Services.AddTypeAuth(x =>
    x
    .AddActionTree<ShiftSoftware.ShiftIdentity.Core.ShiftIdentityActions>()
#if (includeSampleApp)
    .AddActionTree<StockActionTrees>()
    .AddActionTree<SystemActionTrees>()
#endif
);

var host = builder.Build();

var setMan = host.Services.GetRequiredService<SettingManager>();

var culture = setMan.GetCulture();

CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

await host.RefreshTokenAsync(50);

await host.RunAsync();