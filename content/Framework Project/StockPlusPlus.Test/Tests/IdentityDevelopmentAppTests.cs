#if IDENTITY_DEVELOPMENT_APP
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using ShiftIdentity.Tests.Infrastructure;
using ShiftSoftware.ShiftIdentity.Blazor.Services;
using ShiftSoftware.ShiftIdentity.Core.Authentication;
using ShiftSoftware.ShiftIdentity.Core.DTOs;
using ShiftSoftware.ShiftIdentity.Data.Authentication;
using StockPlusPlus.API.Development.IdentityApp;
using StockPlusPlus.Data.DbContext;
using Xunit;

namespace StockPlusPlus.Test.Tests;

[Trait("Category", "IdentityDevelopmentApp")]
public sealed class IdentityDevelopmentAppTests : IAsyncLifetime
{
    private readonly SqlIdentityFixture fixture = new()
    {
        ContextFactory = o => new ConsumerDb(new DbContextOptions<DB>(o.Extensions.ToDictionary(x => x.GetType())))
    };
    private TestServer server = null!;
    private HttpClient client = null!;
    public async ValueTask InitializeAsync()
    {
        await fixture.InitializeAsync(); await IdentityDevelopmentHost.SeedAsync(fixture);
        server = new TestServer(new WebHostBuilder().ConfigureServices(s =>
        {
            IdentityHttpHost.AddAdmissionServices(s, fixture); IdentityHttpHost.AddResourceAuthentication(s);
        }).Configure(app =>
        {
            app.UseRouting(); app.UseAuthentication(); app.UseAuthorization();
            app.UseEndpoints(e => IdentityDevelopmentHost.MapEndpoints(e, fixture));
        }));
        client = server.CreateClient();
    }
    public async ValueTask DisposeAsync() { client.Dispose(); server.Dispose(); await fixture.DisposeAsync(); }

    [Fact]
    public async Task Code_snapshot_uses_one_server_instant_for_every_account_at_a_rollover_boundary()
    {
        var instant = DateTimeOffset.FromUnixTimeSeconds(1800000029).AddMilliseconds(900);
        var clock = new MovingClock(instant);
        fixture.Clock = clock;
        var snapshot = await IdentityDevelopmentHost.CodeSnapshotAsync(fixture);
        Assert.Equal(instant, snapshot.GeneratedAt);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1800000030), snapshot.ExpiresAt);
        Assert.Equal(1, clock.Reads);
        Assert.Equal(7, snapshot.Accounts.Length);
        foreach (var account in snapshot.Accounts.Where(a => a.Code is not null))
        {
            Assert.Matches("^[0-9]{6}$", account.Code!);
            Assert.Equal((await fixture.GetSyntheticFactorAsync(account.Username, instant)).Code, account.Code);
        }
    }

    private sealed class MovingClock(DateTimeOffset instant) : TimeProvider
    {
        public int Reads;
        public override DateTimeOffset GetUtcNow() => instant.AddSeconds(Reads++);
    }

    [Fact]
    public async Task Normal_account_endpoint_returns_real_SQL_state_and_refuses_anonymous_or_wrong_purpose_credentials()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/identity/v2/account")).StatusCode);
        var session = await Login("dev-basic");
        client.DefaultRequestHeaders.Authorization = new("Bearer", session.Token);
        var account = await client.GetFromJsonAsync<AdmissionAccount>("/api/identity/v2/account");
        Assert.Equal("dev-basic", account!.Username); Assert.False(account.HasAuthenticator); Assert.False(account.CanManageRecovery);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/identity/v2/users")).StatusCode);
        client.DefaultRequestHeaders.Authorization = new("Bearer", session.RefreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/identity/v2/account")).StatusCode);
    }

    [Theory]
    [InlineData("/api/auth/login")]
    [InlineData("/api/auth/Refresh")]
    [InlineData("/api/usermanager/ChangePassword")]
    [InlineData("/api/usermanager/StartTotpEnrollment")]
    [InlineData("/api/IdentityUser/AssignRandomPasswords")]
    public async Task Legacy_issuers_and_credential_writers_are_not_mapped(string route)
    {
        client.DefaultRequestHeaders.Authorization = new("Bearer", (await Login("dev-admin")).Token);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync(route, new { })).StatusCode);
    }

    [Fact]
    public async Task Dedicated_admin_permission_controls_user_reads_and_recovery_resets_the_real_account()
    {
        var targetSession = await Login("dev-recovery");
        var targetID = (await fixture.GetSyntheticFactorAsync("dev-recovery")).UserID;
        client.DefaultRequestHeaders.Authorization = new("Bearer", targetSession.Token);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/identity/v2/account/" + fixture.UserID)).StatusCode);
        client.DefaultRequestHeaders.Authorization = new("Bearer", (await Login("dev-admin")).Token);
        Assert.Equal(7, (await client.GetFromJsonAsync<AdmissionAccount[]>("/api/identity/v2/users"))!.Length);
        var result = await Post("mfa/recovery-code", new IssueMfaRecoveryRequest(targetID, "Synthetic independent verification"));
        Assert.IsType<MfaRecoveryCodeIssued>(result);
        var target = await client.GetFromJsonAsync<AdmissionAccount>("/api/identity/v2/account/" + targetID);
        Assert.True(target!.RecoveryRequired); Assert.False(target.HasAuthenticator);
        client.DefaultRequestHeaders.Authorization = new("Bearer", targetSession.Token);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/identity/v2/account")).StatusCode);
        Assert.IsType<AuthenticationRefused>(await Post("refresh", new RenewSessionRequest(targetSession.RefreshToken)));
    }

    [Fact]
    public async Task Password_change_continuation_renews_and_old_session_cannot_read_account_or_refresh()
    {
        var before = await Login("dev-basic");
        var pkce = IdentityHttpHost.Pkce();
        client.DefaultRequestHeaders.Authorization = new("Bearer", before.Token);
        var proof = Assert.IsType<ChallengeRequired>(await Post("password-change", new StartPasswordChangeRequest(pkce.Challenge)));
        client.DefaultRequestHeaders.Authorization = new("Operation", proof.Challenge.Handle);
        var prepared = Assert.IsType<ChallengeRequired>(await Post("password-change/password", new PasswordChangeProofRequest(fixture.Password, pkce.Verifier)));
        client.DefaultRequestHeaders.Authorization = new("Operation", prepared.Challenge.Handle);
        var result = Assert.IsType<PasswordChanged>(await Post("password-change/complete", new CompletePasswordChangeRequest("A different synthetic password 73!", pkce.Verifier)));
        var after = Assert.IsType<SessionIssued>(result.Continuation).Session;
        Assert.IsType<SessionIssued>(await Post("refresh", new RenewSessionRequest(after.RefreshToken)));
        Assert.IsType<AuthenticationRefused>(await Post("refresh", new RenewSessionRequest(before.RefreshToken)));
        client.DefaultRequestHeaders.Authorization = new("Bearer", before.Token);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/identity/v2/account")).StatusCode);
        client.DefaultRequestHeaders.Authorization = new("Bearer", after.Token);
        Assert.Equal("dev-basic", (await client.GetFromJsonAsync<AdmissionAccount>("/api/identity/v2/account"))!.Username);
    }

    private async Task<TokenDTO> Login(string name)
    {
        var proof = IdentityHttpHost.Pkce();
        var result = await Post("login", new PasswordLoginRequest(name, fixture.Password, proof.Challenge));
        if (result is ChallengeRequired challenge)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Operation", challenge.Challenge.Handle);
            result = await Post("login/mfa", new CompleteMfaRequest((await fixture.GetSyntheticFactorAsync(name)).Code!, proof.Verifier));
        }
        return Assert.IsType<SessionIssued>(result).Session;
    }
    private async Task<AuthOutcome> Post<T>(string route, T value)
    {
        using var response = await client.PostAsJsonAsync("/api/identity/v2/" + route, value);
        return (await response.Content.ReadFromJsonAsync<AuthOutcome>())!;
    }
    private sealed class ConsumerDb(DbContextOptions<DB> options) : DB(options)
    {
        protected override void OnModelCreating(ModelBuilder builder) { base.OnModelCreating(builder); builder.ConfigureIdentitySecurity(); }
    }
}
#endif
