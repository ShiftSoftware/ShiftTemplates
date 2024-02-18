using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Localization;
using ShiftSoftware.ShiftBlazor.Extensions;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftIdentity.Blazor.Extensions;
using ShiftSoftware.ShiftIdentity.Blazor.Handlers;
using ShiftSoftware.ShiftIdentity.Dashboard.Blazor.Extensions;
using ShiftSoftware.TypeAuth.Blazor.Extensions;
using StockPlusPlus.Web;
using System.Globalization;
#if (includeSampleApp)
using StockPlusPlus.Shared.ActionTrees;
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

builder.Services.AddShiftBlazor(config =>
{
    config.ShiftConfiguration = options =>
    {
        options.BaseAddress = baseUrl!;
        options.ApiPath = "/api";
        options.ODataPath = "/odata";
        options.UserListEndpoint = baseUrl.AddUrlPath("odata/IdentityPublicUser");
        options.AdditionalAssemblies = new[] { typeof(ShiftSoftware.ShiftIdentity.Dashboard.Blazor.ShiftIdentityDashboarBlazorMaker).Assembly };
        options.AddLanguage("en-US", "English")
               .AddLanguage("ar-IQ", "Arabic", true)
               .AddLanguage("en-US", "English RTL", true)
               .AddLanguage("ku-IQ", "Kurdish", true);
    };
});

builder.Services.AddShiftIdentity("StockPlusPlus-Dev", baseUrl, baseUrl);

builder.Services.AddShiftIdentityDashboardBlazor(x =>
{
    x.ShiftIdentityHostingType = ShiftSoftware.ShiftIdentity.Core.ShiftIdentityHostingTypes.Internal;
    x.LogoPath = "/img/shift-full.png";
    x.Title = "StockPlusPlus";
    x.DynamicTypeAuthActionExpander = async () =>
    {
        //var httpService = builder.Services.BuildServiceProvider().GetRequiredService<HttpService>();

        //var projects = await httpService.GetAsync<ODataDTO<ProjectListDTO>>("/odata/project");

        //ToDoActions.DataLevelAccess.Projects.Expand(projects.Data!.Value.Select(x => new KeyValuePair<string, string>(x.ID!, x.Name!)).ToList());

        //ToDoActions.DataLevelAccess.Statuses.Expand(Enum.GetValues<ToDo.Shared.Enums.ToDoStatus>().Select(x => new KeyValuePair<string, string>(((int)x).ToString(), x.Describe())).ToList());
    };


    x.AddCompanyCustomField("SomeExternalLink", "Some External Link")
    .AddCompanyBranchCustomField("Username", "User Name")
    .AddCompanyBranchCustomField("Password", true);
});

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