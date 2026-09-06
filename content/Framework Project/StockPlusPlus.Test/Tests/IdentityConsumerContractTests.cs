#if shiftFrameworkDevelopmentMode && internalShiftIdentityHosting
using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using OtpNet;
using ShiftIdentity.Tests.Infrastructure;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftIdentity.Core.Authentication;
using ShiftSoftware.ShiftIdentity.Core.DTOs;
using ShiftSoftware.ShiftIdentity.Data.Authentication;
using StockPlusPlus.Data.DbContext;

namespace StockPlusPlus.Test.Tests;

/// <summary>Dev-only consumer contract. Does not instantiate CustomWebApplicationFactory or read application configuration.</summary>
[Trait("Category", "IdentityConsumer")]
public sealed class IdentityConsumerContractTests : IAsyncLifetime
{
    private readonly SqlIdentityFixture fixture = new()
    {
        ContextFactory = options => new ConsumerIdentityContext(new DbContextOptions<DB>(
            options.Extensions.ToDictionary(x => x.GetType())))
    };

    public ValueTask InitializeAsync() => fixture.InitializeAsync();
    public ValueTask DisposeAsync() => fixture.DisposeAsync();

    [Fact]
    public async Task Consumer_database_composes_shared_admission_and_single_use_mfa()
    {
        await fixture.ResetAsync(mfa: true);
        await using (var db = fixture.CreateContext())
        {
            Assert.IsAssignableFrom<DB>(db);
            Assert.NotNull(db.Model.FindEntityType(typeof(StockPlusPlus.Data.Entities.Product)));
            Assert.NotNull(db.Model.FindEntityType(typeof(UserSecurityState)));
        }
        using var first = new IdentityHttpHost(fixture);
        using var second = new IdentityHttpHost(fixture);
        var pkce = IdentityHttpHost.Pkce();
        var pending = Assert.IsType<ChallengeRequired>(await first.LoginAsync(fixture, pkce.Challenge));
        var code = new Totp(fixture.FactorSecret).ComputeTotp();
        var results = await Task.WhenAll(
            first.CompleteAsync(pending.Challenge.Handle!, code, pkce.Verifier),
            second.CompleteAsync(pending.Challenge.Handle!, code, pkce.Verifier));
        var issued = Assert.Single(results.OfType<SessionIssued>());
        Assert.Single(results.OfType<AuthenticationRefused>());
        Assert.IsType<SessionIssued>(await second.RefreshAsync(issued.Session.RefreshToken));
        await using var verify = fixture.CreateContext();
        Assert.Equal(1, await verify.Set<AuthenticationOperation>().CountAsync(x => x.State == AuthenticationOperationState.Completed));
    }

    [Fact]
    public async Task Consumer_existing_routes_keep_the_legacy_envelope_and_staged_routes_remain_absent()
    {
        await fixture.ResetAsync();
        using var host = new LegacyIdentityHttpHost<ConsumerIdentityContext>(fixture);
        using var response = await host.Client.PostAsJsonAsync("/api/Auth/Login",
            new LoginDTO { Username = fixture.Username, Password = fixture.Password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var legacy = await response.Content.ReadFromJsonAsync<ShiftEntityResponse<TokenDTO>>();
        Assert.False(string.IsNullOrEmpty(legacy!.Entity!.RefreshToken));
        using var refresh = await host.Client.PostAsJsonAsync("/api/Auth/Refresh", new { legacy.Entity.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        using var staged = await host.Client.PostAsJsonAsync("/api/identity/v2/login",
            new PasswordLoginRequest(fixture.Username, fixture.Password, IdentityHttpHost.Pkce().Challenge));
        Assert.Equal(HttpStatusCode.NotFound, staged.StatusCode);
    }

    private sealed class ConsumerIdentityContext(DbContextOptions<DB> options) : DB(options)
    {
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.ConfigureIdentitySecurity();
        }
    }
}
#endif
