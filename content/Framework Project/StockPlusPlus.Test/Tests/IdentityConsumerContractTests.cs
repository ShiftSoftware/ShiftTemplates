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

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Consumer_composes_password_change_with_MFA_and_versioned_renewal(bool required, bool mfa)
    {
        var clock = new ControlledClock(DateTimeOffset.UtcNow); fixture.Clock = clock;
        await fixture.ResetAsync(mfa);
        using var host = new IdentityHttpHost(fixture);
        var loginPkce = IdentityHttpHost.Pkce();
        var initial = await host.LoginAsync(fixture, loginPkce.Challenge);
        if (mfa) initial = await host.CompleteAsync(Assert.IsType<ChallengeRequired>(initial).Challenge.Handle!,
            new Totp(fixture.FactorSecret).ComputeTotp(clock.GetUtcNow().UtcDateTime), loginPkce.Verifier);
        var old = Assert.IsType<SessionIssued>(initial);
        var pkce = IdentityHttpHost.Pkce();
        AuthenticationChallenge pending;
        if (required)
        {
            await using var db = fixture.CreateContext();
            await db.Users.Where(x => x.ID == fixture.UserID).ExecuteUpdateAsync(x => x.SetProperty(u => u.RequireChangePassword, true));
            pending = Assert.IsType<ChallengeRequired>(await host.LoginAsync(fixture, pkce.Challenge)).Challenge;
        }
        else
        {
            pending = Assert.IsType<ChallengeRequired>(await host.StartPasswordChangeAsync(old.Session.Token, pkce.Challenge)).Challenge;
            pending = Assert.IsType<ChallengeRequired>(await host.ProvePasswordAsync(pending.Handle!, fixture.Password, pkce.Verifier)).Challenge;
            if (mfa)
            {
                clock.Advance(TimeSpan.FromSeconds(30));
                pending = Assert.IsType<ChallengeRequired>(await host.PasswordMfaAsync(pending.Handle!,
                    new Totp(fixture.FactorSecret).ComputeTotp(clock.GetUtcNow().UtcDateTime), pkce.Verifier)).Challenge;
            }
        }
        var outcome = await host.ChangePasswordAsync(pending.Handle!, "Consumer synthetic password 94!", pkce.Verifier);
        if (required && mfa)
        {
            clock.Advance(TimeSpan.FromSeconds(30));
            outcome = await host.PasswordMfaAsync(Assert.IsType<ChallengeRequired>(outcome).Challenge.Handle!,
                new Totp(fixture.FactorSecret).ComputeTotp(clock.GetUtcNow().UtcDateTime), pkce.Verifier);
        }
        var session = Assert.IsType<SessionIssued>(Assert.IsType<PasswordChanged>(outcome).Continuation);
        Assert.IsType<SessionIssued>(await host.RefreshAsync(session.Session.RefreshToken));
        Assert.IsType<AuthenticationRefused>(await host.RefreshAsync(old.Session.RefreshToken));
        await using var verify = fixture.CreateContext();
        Assert.Equal(2, (await verify.Set<UserSecurityState>().SingleAsync()).SecurityVersion);
        Assert.False((await verify.Users.SingleAsync(x => x.ID == fixture.UserID)).RequireChangePassword);
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
