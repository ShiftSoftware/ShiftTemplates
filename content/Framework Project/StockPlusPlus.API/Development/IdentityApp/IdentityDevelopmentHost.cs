#if IDENTITY_DEVELOPMENT_APP
using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using ShiftIdentity.Tests.Infrastructure;
using ShiftSoftware.ShiftIdentity.AspNetCore.Authentication;
using ShiftSoftware.ShiftIdentity.Blazor.Services;
using ShiftSoftware.ShiftIdentity.Core;
using ShiftSoftware.ShiftIdentity.Data.Authentication;
using ShiftSoftware.TypeAuth.Core;
using StockPlusPlus.Data.DbContext;

namespace StockPlusPlus.API.Development.IdentityApp;

// Built only on explicit development opt-in. No configured application controllers or remote services are registered.
public static class IdentityDevelopmentHost
{
    public static readonly string[] Accounts = ["dev-basic", "dev-mfa", "dev-mandatory", "dev-restricted", "dev-required-mfa", "dev-recovery", "dev-admin", "dev-legacy", "dev-no-email"];
    public static async Task RunAsync()
    {
        await using var fixture = new SqlIdentityFixture
        {
            ContextFactory = options => new DevelopmentDb(new DbContextOptions<DB>(options.Extensions.ToDictionary(x => x.GetType())))
        };
        await fixture.InitializeAsync();
        await SeedAsync(fixture);
        fixture.UseRuntimeDeliveryLimits();
        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(IdentityDevelopmentHost).Assembly.GetName().Name,
            EnvironmentName = Environments.Development
        });
        builder.WebHost.UseKestrel(o => o.Listen(IPAddress.Loopback, 0));
        builder.WebHost.UseStaticWebAssets();
        builder.Services.AddLogging(o => o.AddConsole().AddFilter("Microsoft.AspNetCore", LogLevel.Warning));
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        builder.Services.Configure<Microsoft.AspNetCore.DataProtection.KeyManagement.KeyManagementOptions>(o => o.XmlRepository = new MemoryKeys());
        IdentityHttpHost.AddAdmissionServices(builder.Services, fixture);
        IdentityHttpHost.AddResourceAuthentication(builder.Services);
        builder.Services.AddSingleton(new LocalSecurityInbox(fixture.Clock));
        builder.Services.AddSingleton<ISecurityEmailSink>(services => services.GetRequiredService<LocalSecurityInbox>());
        await using var app = builder.Build();
        var runID = Guid.NewGuid().ToString("N");
        var origin = "";
        app.Use(async (context, next) =>
        {
            if (context.Connection.RemoteIpAddress is not { } remote || !IPAddress.IsLoopback(remote) || context.Request.Host.Host != "127.0.0.1")
            { context.Response.StatusCode = 403; return; }
            context.Response.Headers.CacheControl = "no-store";
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["Referrer-Policy"] = "no-referrer";
            context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self' 'wasm-unsafe-eval' 'unsafe-eval'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self' data:; connect-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";
            var path = context.Request.Path.Value!;
            if (path.StartsWith("/appsettings", StringComparison.OrdinalIgnoreCase))
            {
                if (path is "/appsettings.json" or "/appsettings.Development.json")
                    await context.Response.WriteAsJsonAsync(new { IdentityDevelopment = true, RunID = runID });
                else context.Response.StatusCode = 404;
                return;
            }
            if (context.Request.Method != "GET" && context.Request.Method != "HEAD" &&
                context.Request.Headers.Origin != origin)
            { context.Response.StatusCode = 403; return; }
            await next(context);
        });
        app.UseBlazorFrameworkFiles();
        app.UseStaticFiles();
        app.UseAuthentication();
        app.UseAuthorization();
        MapEndpoints(app, fixture);
        MapDevelopmentEndpoints(app, fixture);
        app.MapPost("/development/stop", () => { app.Lifetime.StopApplication(); return Results.Ok(); });
        // Unknown API routes must never fall through to an application controller or an HTML success response.
        app.Map("/api/{**path}", () => Results.NotFound());
        app.MapFallbackToFile("index.html");
        await app.StartAsync();
        origin = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        Console.WriteLine($"IDENTITY_DEVELOPMENT_URL={origin}/");
        Console.WriteLine("Normal StockPlusPlus WebAssembly shell; owned synthetic SQL only. Graceful shutdown removes the database.");
        await app.WaitForShutdownAsync();
    }

    public static void MapDevelopmentEndpoints(IEndpointRouteBuilder endpoints, SqlIdentityFixture fixture)
    {
        endpoints.MapGet("/development/info", async (CancellationToken cancellation) =>
            Results.Ok(await CodeSnapshotAsync(fixture, cancellation)));
        endpoints.MapPost("/development/mandatory/{required:bool}", async (bool required) => { await fixture.ChangeMfaPolicyAsync(required); return Results.Ok(); });
        endpoints.MapPost("/development/verified-email/{required:bool}", async (bool required) => { await fixture.ChangeVerifiedEmailPolicyAsync(required); return Results.Ok(); });
        endpoints.MapGet("/development/inbox", (LocalSecurityInbox inbox) =>
            Results.Ok(new { messages = inbox.Messages, failDeliveries = inbox.FailDeliveries }));
        endpoints.MapPost("/development/inbox/failure/{enabled:bool}", (bool enabled, LocalSecurityInbox inbox) =>
        { inbox.FailDeliveries = enabled; return Results.Ok(); });
    }

    public static async Task<AuthenticatorCodeSnapshot> CodeSnapshotAsync(SqlIdentityFixture fixture, CancellationToken cancellation = default)
    {
        var generated = fixture.Clock.GetUtcNow();
        var expires = DateTimeOffset.FromUnixTimeSeconds((generated.ToUnixTimeSeconds() / 30 + 1) * 30);
        var codes = new List<AuthenticatorCode>();
        foreach (var name in Accounts)
        {
            var factor = await fixture.GetSyntheticFactorAsync(name, generated, cancellation);
            codes.Add(new(name, factor.Code, factor.UserID));
        }
        return new(generated, expires, codes.ToArray(), fixture.Password);
    }

    public static async Task SeedAsync(SqlIdentityFixture fixture)
    {
        foreach (var name in Accounts)
        {
            var id = await fixture.CreateSyntheticUserAsync(name, name == "dev-admin"
                ? "{\"ShiftIdentityActions\":{\"ManageMfaRecovery\":[\"m\"],\"Users\":[\"r\",\"w\"]}}" : null,
                name is "dev-mfa" or "dev-required-mfa" or "dev-recovery" or "dev-admin",
                email: name == "dev-no-email" ? null : name + "@example.invalid");
            await using var db = fixture.CreateContext();
            var user = await db.Users.SingleAsync(u => u.ID == id);
            var security = await db.Set<UserSecurityState>().SingleAsync(s => s.UserID == id);
            user.RequireChangePassword = name is "dev-restricted" or "dev-required-mfa";
            if (name != "dev-no-email")
            {
                user.EmailVerified = name == "dev-admin";
                if (name != "dev-legacy")
                    RecoveryContact.RecordOwnership(user, security, RecoveryEmailProvenance.TrustedAdminAssignment);
            }
            await db.SaveChangesAsync();
        }
    }

    public static void MapEndpoints(IEndpointRouteBuilder endpoints, SqlIdentityFixture fixture)
    {
        IdentityHttpHost.MapAdmissionEndpoints(endpoints);
        var group = endpoints.MapGroup("/api/identity/v2").RequireAuthorization("AdmissionResource");
        group.MapGet("/account", (HttpContext c, CancellationToken _) => Account(c, null));
        group.MapGet("/account/{userID:long}", (HttpContext c, long userID) => Account(c, userID));
        group.MapGet("/users", async (HttpContext c, CancellationToken _) =>
        {
            var actor = await Current(c);
            if (actor is null) return Results.Unauthorized();
            if (!CanManageUsers(actor)) return Results.StatusCode(403);
            await using var db = fixture.CreateContext();
            var ids = await db.Users.Where(u => Accounts.Contains(u.Username) && !u.IsDeleted).OrderBy(u => u.Username)
                .Select(u => u.ID).ToArrayAsync();
            var accounts = new List<AdmissionAccount>();
            foreach (var id in ids)
                if (await Read(id) is { } account) accounts.Add(account);
            return Results.Ok(accounts);
        });
        async Task<IResult> Account(HttpContext c, long? target)
        {
            var actor = await Current(c);
            if (actor is null) return Results.Unauthorized();
            if (target is null || target == actor.UserID) return Results.Ok(actor);
            if (!CanManageUsers(actor)) return Results.StatusCode(403);
            var found = await Read(target.Value);
            return found is null ? Results.NotFound() : Results.Ok(found);
        }
        async Task<AdmissionAccount?> Current(HttpContext c)
        {
            if (!long.TryParse(c.User.FindFirstValue("shift_uid"), out var id)) return null;
            await using var db = fixture.CreateContext();
            var state = await db.Set<UserSecurityState>().AsNoTracking().SingleOrDefaultAsync(s => s.UserID == id);
            var policy = await db.Set<AuthenticationPolicyState>().AsNoTracking().SingleAsync();
            if (state is null || state.SecurityVersion.ToString() != c.User.FindFirstValue("shift_sv") ||
                policy.Revision.ToString() != c.User.FindFirstValue("shift_policy")) return null;
            return await Read(id, requireActive: true);
        }
        async Task<AdmissionAccount?> Read(long id, bool requireActive = false)
        {
            await using var db = fixture.CreateContext();
            var user = await db.Users.AsNoTracking().Include(u => u.AccessTrees).ThenInclude(t => t.AccessTree)
                .SingleOrDefaultAsync(u => u.ID == id && !u.IsDeleted && (!requireActive || u.IsActive));
            var state = await db.Set<UserSecurityState>().AsNoTracking().SingleOrDefaultAsync(s => s.UserID == id);
            if (user is null || state is null) return null;
            var trees = user.AccessTrees.Select(t => t.AccessTree.Tree).ToList();
            if (!string.IsNullOrWhiteSpace(user.AccessTree)) trees.Add(user.AccessTree);
            var permissions = new TypeAuthContext(trees, typeof(ShiftIdentityActions));
            var canWrite = permissions.CanWrite(ShiftIdentityActions.Users);
            return new(user.ID, user.Username, user.FullName, state.ProtectedTotpSecret is not null,
                state.LocalMfaRecoveryRequired, permissions.CanAccess(ShiftIdentityActions.ManageMfaRecovery),
                user.Email, user.EmailVerified, RecoveryContact.IsEligible(user, state), canWrite, canWrite,
                IsActive: user.IsActive, CanManageAccount: canWrite);
        }
        static bool CanManageUsers(AdmissionAccount actor) => actor.CanManageRecovery || actor.CanManagePasswordReset || actor.CanManageEmailVerification;
    }

    private sealed class DevelopmentDb(DbContextOptions<DB> options) : DB(options)
    {
        protected override void OnModelCreating(ModelBuilder builder) { base.OnModelCreating(builder); builder.ConfigureIdentitySecurity(); }
    }
    private sealed class MemoryKeys : Microsoft.AspNetCore.DataProtection.Repositories.IXmlRepository
    {
        private readonly List<System.Xml.Linq.XElement> values = [];
        public IReadOnlyCollection<System.Xml.Linq.XElement> GetAllElements() { lock (values) return values.Select(x => new System.Xml.Linq.XElement(x)).ToArray(); }
        public void StoreElement(System.Xml.Linq.XElement element, string friendlyName) { lock (values) values.Add(new(element)); }
    }
}
#endif
