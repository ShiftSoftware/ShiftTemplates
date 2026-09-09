#if IDENTITY_ADMISSION_PREVIEW
using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using OtpNet;
using ShiftIdentity.Tests.Infrastructure;
using ShiftSoftware.ShiftBlazor.Extensions;
using ShiftSoftware.ShiftIdentity.Blazor;
using ShiftSoftware.ShiftIdentity.Blazor.Services;
using ShiftSoftware.ShiftIdentity.Core;
using ShiftSoftware.ShiftIdentity.Core.DTOs;
using ShiftSoftware.ShiftIdentity.Core.Localization;
using ShiftSoftware.ShiftIdentity.Dashboard.Blazor.Extensions;
using ShiftSoftware.ShiftIdentity.Data.Authentication;
using ShiftSoftware.ShiftIdentity.Data.Entities;
using StockPlusPlus.Data.DbContext;

namespace StockPlusPlus.API.Development.IdentityPreview;

internal static class IdentityPreviewHost
{
    public static async Task RunAsync()
    {
        await using var fixture = new SqlIdentityFixture
        {
            ContextFactory = options => new PreviewDb(new DbContextOptions<DB>(options.Extensions.ToDictionary(x => x.GetType())))
        };
        await fixture.InitializeAsync();
        await SeedAsync(fixture);

        // No default configuration providers: never load appsettings, user secrets or application connections.
        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(IdentityPreviewHost).Assembly.GetName().Name,
            EnvironmentName = Environments.Development
        });
        builder.WebHost.UseKestrel(options => options.Listen(IPAddress.Loopback, 0));
        builder.WebHost.UseStaticWebAssets();
        builder.Services.AddLogging(x => x.AddConsole());
        builder.Services.AddRouting();
        builder.Services.AddRazorComponents().AddInteractiveServerComponents();
        builder.Services.AddAntiforgery(options =>
        {
            options.Cookie.Name = "StockPlusPlus.IdentityPreview." + Guid.NewGuid().ToString("N");
            options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
        });
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        builder.Services.Configure<Microsoft.AspNetCore.DataProtection.KeyManagement.KeyManagementOptions>(options =>
            options.XmlRepository = new PreviewKeyRepository());
        IdentityHttpHost.AddAdmissionServices(builder.Services, fixture);
        var preview = new IdentityPreviewState(fixture);
        builder.Services.AddSingleton(preview);
        builder.Services.AddScoped<PreviewIdentityStore>();
        builder.Services.AddScoped<IIdentityStore>(sp => sp.GetRequiredService<PreviewIdentityStore>());
        builder.Services.AddScoped<AuthenticationStateProvider, PreviewAuthState>();
        builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(preview.Origin) });
        builder.Services.AddScoped<AuthenticationFlow>();
        builder.Services.AddShiftBlazor(options => options.ShiftConfiguration = config => config.BaseAddress = "/");
        // Shared dialog/settings services expect synchronous storage. Keep that storage in this
        // server circuit too, so preview settings never use browser localStorage.
        builder.Services.AddScoped<Blazored.LocalStorage.ISyncLocalStorageService, PreviewSettingsStore>();
        builder.Services.AddShiftIdentityDashboardBlazor(options =>
        {
            options.Title = "StockPlusPlus authentication preview";
            options.LogoPath = "/_preview/logo.svg";
        });
        builder.Services.AddTransient(sp => new ShiftIdentityLocalizer(sp, typeof(ShiftSoftwareLocalization.Identity.Resource)));

        await using var app = builder.Build();
        app.Use(async (context, next) =>
        {
            if (context.Connection.RemoteIpAddress is not { } remote || !IPAddress.IsLoopback(remote) ||
                context.Request.Host.Host != "127.0.0.1")
            {
                context.Response.StatusCode = 403;
                return;
            }
            context.Response.Headers.CacheControl = "no-store";
            var path = context.Request.Path;
            if (path != "/" && !path.StartsWithSegments("/_blazor") && !path.StartsWithSegments("/_preview") &&
                !path.StartsWithSegments("/api/identity/v2") && !path.StartsWithSegments("/_content") &&
                !(path.StartsWithSegments("/_framework") && path.Value!.EndsWith(".js", StringComparison.Ordinal)))
            {
                context.Response.StatusCode = 404;
                return;
            }
            await next(context);
        });
        app.UseWhen(context => context.Request.Path.StartsWithSegments("/_content") ||
            (context.Request.Path.StartsWithSegments("/_framework") && context.Request.Path.Value!.EndsWith(".js", StringComparison.Ordinal)),
            assets => assets.UseStaticFiles());
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAntiforgery();
        // .NET 10 serves the Blazor bootstrap through the static-asset endpoint manifest.
        // The preview path allowlist above still excludes appsettings and other application files.
        app.MapStaticAssets();
        IdentityHttpHost.MapAdmissionEndpoints(app);
        app.MapGet("/_preview/logo.svg", () => Results.Text(
            """<svg xmlns="http://www.w3.org/2000/svg" width="360" height="42"><text x="0" y="32" font-size="30" font-family="sans-serif" fill="#24597a">StockPlusPlus · Preview</text></svg>""", "image/svg+xml"));
        app.MapGet("/_preview/info", () => new
        {
            SyntheticOnly = true, Password = fixture.Password,
            Accounts = IdentityPreviewState.Accounts, CurrentMfaCode = preview.CurrentCode,
            SecondsRemaining = preview.SecondsRemaining
        });
        app.MapPost("/_preview/stop", (HttpContext context) =>
        {
            // A same-origin review control, with no configured application or database involved.
            if (context.Request.Headers.Origin != preview.Origin) return Results.StatusCode(403);
            app.Lifetime.StopApplication();
            return Results.Ok();
        });
        app.MapRazorComponents<IdentityPreviewPage>().AddInteractiveServerRenderMode();
        await app.StartAsync();
        preview.Origin = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        Console.WriteLine($"IDENTITY_PREVIEW_URL={preview.Origin}/");
        Console.WriteLine("Synthetic preview only. The owned database is removed on graceful shutdown.");
        await app.WaitForShutdownAsync();
    }

    private static async Task SeedAsync(SqlIdentityFixture fixture)
    {
        await using var db = fixture.CreateContext();
        var template = await db.Users.SingleAsync(x => x.ID == fixture.UserID);
        foreach (var account in IdentityPreviewState.Accounts)
        {
            var hash = HashService.GenerateHash(fixture.Password);
            var user = new User
            {
                Username = account.Username, FullName = account.Label, IsActive = true,
                PasswordHash = hash.PasswordHash, Salt = hash.Salt,
                RequireChangePassword = account.Username is "preview-restricted" or "preview-required-mfa",
                AccessTree = account.Username == "preview-admin"
                    ? "{\"ShiftIdentityActions\":{\"ManageMfaRecovery\":[\"m\"]}}" : null,
                CompanyID = template.CompanyID, CompanyBranchID = template.CompanyBranchID,
                CountryID = template.CountryID, RegionID = template.RegionID
            };
            db.Add(user);
            await db.SaveChangesAsync();
            db.Add(new UserSecurityState
            {
                UserID = user.ID,
                ProtectedTotpSecret = account.Username is "preview-mfa" or "preview-required-mfa" or "preview-recovery" or "preview-admin"
                    ? fixture.Protection.CreateProtector("Identity.Totp.v2").Protect(fixture.FactorSecret) : null
            });
        }
        await db.SaveChangesAsync();
    }

    private sealed class PreviewDb(DbContextOptions<DB> options) : DB(options)
    {
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.ConfigureIdentitySecurity();
        }
    }

    private sealed class PreviewKeyRepository : Microsoft.AspNetCore.DataProtection.Repositories.IXmlRepository
    {
        private readonly List<System.Xml.Linq.XElement> elements = [];
        public IReadOnlyCollection<System.Xml.Linq.XElement> GetAllElements()
        {
            lock (elements) return elements.Select(x => new System.Xml.Linq.XElement(x)).ToArray();
        }
        public void StoreElement(System.Xml.Linq.XElement element, string friendlyName)
        {
            lock (elements) elements.Add(new(element));
        }
    }

}

public sealed class IdentityPreviewState(SqlIdentityFixture fixture)
{
    public string Origin { get; set; } = "";
    public string Password => fixture.Password;
    public string CurrentCode => new Totp(fixture.FactorSecret).ComputeTotp();
    public int SecondsRemaining => 30 - (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() % 30);
    public bool Mandatory { get; private set; }
    public Task<(long UserID, string? Code)> FactorAsync(string username) => fixture.GetSyntheticFactorAsync(username);
    public async Task<AuthenticatorCodeSnapshot> CodeSnapshotAsync(string username, string? newSecret, CancellationToken cancellation)
    {
        var generated = fixture.Clock.GetUtcNow();
        var expires = DateTimeOffset.FromUnixTimeSeconds((generated.ToUnixTimeSeconds() / 30 + 1) * 30);
        var factor = await fixture.GetSyntheticFactorAsync(username, generated, cancellation);
        var codes = new List<AuthenticatorCode> { new(username, factor.Code, factor.UserID) };
        if (newSecret is not null)
            codes.Add(new("New authenticator", new Totp(Base32Encoding.ToBytes(newSecret)).ComputeTotp(generated.UtcDateTime)));
        return new(generated, expires, codes.ToArray());
    }
    public async Task SetMandatoryAsync(bool mandatory)
    {
        await fixture.ChangeMfaPolicyAsync(mandatory);
        Mandatory = mandatory;
    }
    public static PreviewAccount[] Accounts { get; } =
    [
        new("preview-basic", "First signed-in authenticator enrollment"),
        new("preview-mfa", "Replace an existing authenticator"),
        new("preview-mandatory", "Required enrollment when the policy below is enabled"),
        new("preview-restricted", "Password change required, without MFA"),
        new("preview-required-mfa", "Password change required, then existing MFA"),
        new("preview-recovery", "Lost authenticator — ask the synthetic admin for a recovery code"),
        new("preview-admin", "Recovery operator with the dedicated permission")
    ];
}
public sealed record PreviewAccount(string Username, string Label);

public sealed class PreviewIdentityStore : IIdentityStore
{
    private TokenDTO? token;
    public Task<TokenDTO?> GetTokenAsync() => Task.FromResult(token);
    public string? GetToken() => token?.Token;
    public Task StoreTokenAsync(TokenDTO value) { token = value; return Task.CompletedTask; }
    public Task RemoveTokenAsync() { token = null; return Task.CompletedTask; }
}

internal sealed class PreviewAuthState(PreviewIdentityStore store) : AuthenticationStateProvider
{
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await store.GetTokenAsync();
        return new(token is null ? new ClaimsPrincipal(new ClaimsIdentity()) :
            new ClaimsPrincipal(new ClaimsIdentity(new Microsoft.IdentityModel.JsonWebTokens.JsonWebToken(token.Token).Claims, "PreviewSession")));
    }
}

internal sealed class PreviewSettingsStore : Blazored.LocalStorage.ISyncLocalStorageService
{
    private readonly Dictionary<string, string> values = [];
    public event EventHandler<Blazored.LocalStorage.ChangingEventArgs>? Changing;
    public event EventHandler<Blazored.LocalStorage.ChangedEventArgs>? Changed;
    public void Clear() => values.Clear();
    public bool ContainKey(string key) => values.ContainsKey(key);
    public T GetItem<T>(string key) => values.TryGetValue(key, out var value) ? System.Text.Json.JsonSerializer.Deserialize<T>(value)! : default!;
    public string GetItemAsString(string key) => values.GetValueOrDefault(key)!;
    public string Key(int index) => values.Keys.ElementAtOrDefault(index)!;
    public IEnumerable<string> Keys() => values.Keys.ToArray();
    public int Length() => values.Count;
    public void RemoveItem(string key) => values.Remove(key);
    public void RemoveItems(IEnumerable<string> keys) { foreach (var key in keys) RemoveItem(key); }
    public void SetItem<T>(string key, T data) => SetItemAsString(key, System.Text.Json.JsonSerializer.Serialize(data));
    public void SetItemAsString(string key, string data)
    {
        var old = GetItemAsString(key);
        var change = new Blazored.LocalStorage.ChangingEventArgs { Key = key, OldValue = old, NewValue = data };
        Changing?.Invoke(this, change);
        if (change.Cancel) return;
        values[key] = data;
        Changed?.Invoke(this, new() { Key = key, OldValue = old, NewValue = data });
    }
}
#endif
