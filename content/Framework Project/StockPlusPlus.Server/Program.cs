using BitzArt.Blazor.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Hosting;
using Microsoft.JSInterop;
using ShiftSoftware.ShiftBlazor.Extensions;
using ShiftSoftware.ShiftBlazor.Services;
using ShiftSoftware.ShiftEntity.Core.Extensions;
using ShiftSoftware.ShiftIdentity.Blazor;
using ShiftSoftware.ShiftIdentity.Blazor.Providers;
using ShiftSoftware.ShiftIdentity.Blazor.Services;
using StockPlusPlus.Server;
using StockPlusPlus.Server.Components;
using System.Security.Cryptography;
using Microsoft.Extensions.Azure;
using ShiftSoftware.TypeAuth.Blazor.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

#region Blazor Web Settings

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
builder.Services.AddLocalization();

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
        options.AdditionalAssemblies = new[] { typeof(StockPlusPlus.Client._Imports).Assembly, typeof(ShiftSoftware.ShiftIdentity.Dashboard.Blazor.ShiftIdentityDashboarBlazorMaker).Assembly };
#else
        options.AdditionalAssemblies = new[] { typeof(StockPlusPlus.Client._Imports).Assembly };
#endif
        options.AddLanguage("en-US", "English", false)
               .AddLanguage("ar-IQ", "Arabic", true)
               .AddLanguage("ku-IQ", "Kurdish", true);
    };

});

builder.Services.AddTypeAuth(x =>
    x
    .AddActionTree<ShiftSoftware.ShiftIdentity.Core.ShiftIdentityActions>()
    .AddActionTree<ShiftSoftware.ShiftEntity.Core.AzureStorageActionTree>()
#if (includeSampleApp)
    .AddActionTree<StockPlusPlusActionTree>()
#endif
);



//builder.Services.AddAuthorization();
//builder.Services.AddAuthorizationCore();
//builder.Services.AddCascadingAuthenticationState();
//builder.Services.AddScoped<AuthenticationStateProvider, TestAuthStateProvider>();
//builder.Services.AddScoped<IIdentityStore, TestIdentityStore>();

#endregion

#region Authentication Setup
builder.Services.AddAuthentication("TestScheme")
    .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("TestScheme", options => { });

//var rsa = RSA.Create();
//rsa.ImportRSAPublicKey(Convert.FromBase64String(tokenRSAPublicKeyBase64), out _);

//builder.Services.AddAuthentication(a =>
//{
//    a.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
//    a.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
//}).AddJwtBearer(o =>
//{
//    o.TokenValidationParameters = new TokenValidationParameters
//    {
//        ValidateAudience = false,
//        ValidateIssuer = true,
//        ValidIssuer = tokenIssuer,
//        RequireExpirationTime = true,
//        IssuerSigningKey = new RsaSecurityKey(rsa),
//        ValidateIssuerSigningKey = true,
//        ValidateLifetime = true,
//        ClockSkew = TimeSpan.Zero,
//        LifetimeValidator = (DateTime? notBefore, DateTime? expires, SecurityToken securityToken,
//                             TokenValidationParameters validationParameters) =>
//        {
//            bool result = false;
//            var now = DateTime.UtcNow;

//            if (notBefore != null && now < notBefore)
//                result = false;

//            if (expires != null)
//                result = expires > now;

//            if (!result)
//                throw new SecurityTokenExpiredException("Token expired");

//            return result;
//        }
//    };

//    o.Events = new JwtBearerEvents
//    {
//        OnAuthenticationFailed = context =>
//        {
//            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
//            {
//                context.Response.Headers.Add("IS-TOKEN-EXPIRED", "true");
//            }
//            return Task.CompletedTask;
//        }
//    };
//});
#endregion


builder.Services.AddControllers();
builder.Services.AddAzureClients(clientBuilder =>
{
    clientBuilder.AddBlobServiceClient(builder.Configuration["devstoreaccount1:blobServiceUri"]!).WithName("devstoreaccount1");
    clientBuilder.AddQueueServiceClient(builder.Configuration["devstoreaccount1:queueServiceUri"]!).WithName("devstoreaccount1");
    clientBuilder.AddTableServiceClient(builder.Configuration["devstoreaccount1:tableServiceUri"]!).WithName("devstoreaccount1");
});
builder.Services.AddAzureClients(clientBuilder =>
{
    clientBuilder.AddBlobServiceClient(builder.Configuration["devstoreaccount1:blobServiceUri"]!).WithName("devstoreaccount1");
    clientBuilder.AddQueueServiceClient(builder.Configuration["devstoreaccount1:queueServiceUri"]!).WithName("devstoreaccount1");
    clientBuilder.AddTableServiceClient(builder.Configuration["devstoreaccount1:tableServiceUri"]!).WithName("devstoreaccount1");
});
builder.Services.AddAzureClients(clientBuilder =>
{
    clientBuilder.AddBlobServiceClient(builder.Configuration["devstoreaccount1:blobServiceUri"]!).WithName("devstoreaccount1");
    clientBuilder.AddQueueServiceClient(builder.Configuration["devstoreaccount1:queueServiceUri"]!).WithName("devstoreaccount1");
    clientBuilder.AddTableServiceClient(builder.Configuration["devstoreaccount1:tableServiceUri"]!).WithName("devstoreaccount1");
});
builder.Services.AddAzureClients(clientBuilder =>
{
    clientBuilder.AddBlobServiceClient(builder.Configuration["devstoreaccount1:blobServiceUri"]!).WithName("devstoreaccount1");
    clientBuilder.AddQueueServiceClient(builder.Configuration["devstoreaccount1:queueServiceUri"]!).WithName("devstoreaccount1");
    clientBuilder.AddTableServiceClient(builder.Configuration["devstoreaccount1:tableServiceUri"]!).WithName("devstoreaccount1");
});
builder.Services.AddAzureClients(clientBuilder =>
{
    clientBuilder.AddBlobServiceClient(builder.Configuration["devstoreaccount1:blobServiceUri"]!).WithName("devstoreaccount1");
    clientBuilder.AddQueueServiceClient(builder.Configuration["devstoreaccount1:queueServiceUri"]!).WithName("devstoreaccount1");
    clientBuilder.AddTableServiceClient(builder.Configuration["devstoreaccount1:tableServiceUri"]!).WithName("devstoreaccount1");
});

var app = builder.Build();

#region Setup Settings Manager
using (var scope = app.Services.CreateScope())
{
    var settingManager = scope.ServiceProvider.GetRequiredService<SettingManager>();
    var supportedCultures = SettingManager.Configuration.Languages.Select(x => x.CultureName).ToArray();

    app.UseRequestLocalization(options =>
    {
        var cultures = supportedCultures.Select(x => settingManager.GetCulture(x)).ToList();
        var defaultCulture = cultures.FirstOrDefault() ?? settingManager.GetCulture(DefaultAppSetting.Language.CultureName);
        options.DefaultRequestCulture = new RequestCulture(defaultCulture);
        options.SupportedCultures = cultures; 
        options.SupportedUICultures = cultures;

        // Define custom request culture providers order
        // the order is important, only the first provider
        // that can successfully determine the request culture is used.
        options.RequestCultureProviders = [
                new QueryStringRequestCultureProvider(),
                new CookieRequestCultureProvider(),
                new AcceptLanguageHeaderRequestCultureProvider()
            ];
        options.ApplyCurrentCultureToResponseHeaders = true;

    });

    Console.WriteLine("(in scope)Setting up application settings...");

    //var js = scope.ServiceProvider.GetRequiredService<IJSRuntime>();
    //await js.InvokeVoidAsync("console.log", "hello world");
}

#endregion

Console.WriteLine("Application Starting...");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
//app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(StockPlusPlus.Client._Imports).Assembly);

app.Run();
