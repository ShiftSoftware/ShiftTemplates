using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ShiftEntity.Model.Enums;
using ShiftSoftware.ShiftIdentity.Data;
using StockPlusPlus.Shared.ActionTrees;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace StockPlusPlus.Test.Tests;

// SAFETY NET for the Phase-5 API AuthController → minimal-API conversion. These exercise the real login/token
// pipeline end-to-end (over HTTP, through AuthService) against the seeded SuperUser/OneTwo admin. They pass against
// the CURRENT controller and MUST still pass after the conversion — that's the whole point of writing them first.
[Collection("API Collection")]
public class AuthEndpointTests
{
    private readonly CustomWebApplicationFactory factory;

    public AuthEndpointTests(CustomWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    // Case-insensitive property lookup so the assertions don't depend on PascalCase vs camelCase.
    private static JsonElement Prop(JsonElement e, string name)
        => e.EnumerateObject().First(p => string.Equals(p.Name, name, System.StringComparison.OrdinalIgnoreCase)).Value;

    // The host skips DB seeding under the Test environment, so seed a login-able SuperUser ourselves (idempotent —
    // DBSeed checks existence). Mirrors app.SeedDBAsync("SuperUser","OneTwo",…) from the host's Program.cs.
    private static readonly SemaphoreSlim _seedLock = new(1, 1);
    private static bool _seeded;
    private async Task EnsureSeededAsync()
    {
        if (_seeded) return;
        await _seedLock.WaitAsync();
        try
        {
            if (_seeded) return;
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ShiftIdentityDbContext>();
            var actionTrees = new List<Type>
            {
                typeof(ShiftSoftware.ShiftEntity.Core.GeneralActionTree),
                typeof(StockPlusPlusActionTree),
                typeof(ShiftSoftware.ShiftIdentity.Core.ShiftIdentityActions),
            };
            await new DBSeed(db, actionTrees, "SuperUser", "OneTwo", new DBSeedOptions
            {
                CountryExternalId = "1", CountryShortCode = "IQ", CountryCallingCode = "+964",
                RegionExternalId = "1", RegionShortCode = "KRG",
                CompanyShortCode = "SFT", CompanyExternalId = "-1", CompanyAlternativeExternalId = "shift-software",
                CompanyType = CompanyTypes.NotSpecified,
                CompanyBranchExternalId = "-11", CompanyBranchShortCode = "SFT-EBL",
            }).SeedAsync();
            _seeded = true;
        }
        finally { _seedLock.Release(); }
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsAccessAndRefreshTokens()
    {
        var client = factory.CreateClient();
        await EnsureSeededAsync();

        var resp = await client.PostAsJsonAsync("api/Auth/Login", new { Username = "SuperUser", Password = "OneTwo" });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var entity = Prop(doc.RootElement, "Entity");
        Assert.False(string.IsNullOrEmpty(Prop(entity, "Token").GetString()));
        Assert.False(string.IsNullOrEmpty(Prop(entity, "RefreshToken").GetString()));
    }

    [Fact]
    public async Task Login_InvalidPassword_Returns400()
    {
        var client = factory.CreateClient();
        await EnsureSeededAsync();

        var resp = await client.PostAsJsonAsync("api/Auth/Login", new { Username = "SuperUser", Password = "definitely-wrong" });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithTokenFromLogin_ReturnsNewAccessToken()
    {
        var client = factory.CreateClient();
        await EnsureSeededAsync();

        var login = await client.PostAsJsonAsync("api/Auth/Login", new { Username = "SuperUser", Password = "OneTwo" });
        login.EnsureSuccessStatusCode();
        using var loginDoc = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        var refreshToken = Prop(Prop(loginDoc.RootElement, "Entity"), "RefreshToken").GetString();

        var resp = await client.PostAsJsonAsync("api/Auth/Refresh", new { RefreshToken = refreshToken });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.False(string.IsNullOrEmpty(Prop(Prop(doc.RootElement, "Entity"), "Token").GetString()));
    }

    [Fact]
    public async Task Refresh_WithGarbageToken_Returns400()
    {
        var client = factory.CreateClient();

        var resp = await client.PostAsJsonAsync("api/Auth/Refresh", new { RefreshToken = "not-a-real-refresh-token" });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // The MFA endpoint is gated by [StepUp(Mfa, allowAccessToken:false)] — a normal access token (which the test
    // client carries) must NOT satisfy it. Proves the step-up policy is enforced (i.e. not an open endpoint).
    [Fact]
    public async Task LoginMfa_WithPlainAccessToken_IsRejected()
    {
        var client = factory.CreateClient();

        var resp = await client.PostAsJsonAsync("api/Auth/Login/mfa", new { Code = "000000" });

        Assert.NotEqual(HttpStatusCode.OK, resp.StatusCode);
    }

    // AuthCode requires an authenticated user (the test client is authenticated) and a registered app; an unknown
    // app can't produce a code → 400. Proves the endpoint is wired + reaches AuthCodeService (not 404/500/200).
    [Fact]
    public async Task AuthCode_ForUnknownApp_Returns400()
    {
        var client = factory.CreateClient();

        var resp = await client.PostAsJsonAsync("api/Auth/AuthCode", new { AppId = "no-such-app", CodeChallenge = "abc", ReturnUrl = "http://localhost/back" });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // TokenWithAppIdOnly for an unknown app → 400. Proves the anonymous external-token endpoint is wired.
    [Fact]
    public async Task TokenWithAppIdOnly_ForUnknownApp_Returns400()
    {
        var client = factory.CreateClient();

        var resp = await client.PostAsJsonAsync("api/Auth/TokenWithAppIdOnly", new { AppId = "no-such-app" });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }
}
