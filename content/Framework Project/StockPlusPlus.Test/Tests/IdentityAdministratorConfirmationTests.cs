#if shiftFrameworkDevelopmentMode && internalShiftIdentityHosting
using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OtpNet;
using ShiftIdentity.Tests.Infrastructure;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.ShiftIdentity.AspNetCore.Authentication;
using ShiftSoftware.ShiftIdentity.Core;
using ShiftSoftware.ShiftIdentity.Core.Authentication;
using ShiftSoftware.ShiftIdentity.Core.DTOs.User;
using ShiftSoftware.ShiftIdentity.Data;
using ShiftSoftware.ShiftIdentity.Data.Authentication;
using ShiftSoftware.ShiftIdentity.Data.Entities;
using StockPlusPlus.Data.DbContext;

namespace StockPlusPlus.Test.Tests;

/// <summary>The normal combined API registration, with fixture-owned SQL and captured email providers.</summary>
[Trait("Category", "IdentityConsumer")]
public sealed class IdentityAdministratorConfirmationTests : IAsyncLifetime
{
    private const string Server = @"Server=.\SQLEXPRESS;Database=master;Integrated Security=true;Encrypt=true;TrustServerCertificate=true;Connect Timeout=5";
    private const string Password = "Synthetic confirmation phrase 38!";
    private readonly string database = "StockPlusPlus_Test_AdminConfirmation_" + Guid.NewGuid().ToString("N");
    private readonly ControlledClock clock = new(DateTimeOffset.UtcNow.AddMinutes(-2));
    private readonly byte[] secret = Enumerable.Range(1, 20).Select(x => (byte)x).ToArray();
    private ConfirmationFactory? factory;
    private bool owned;

    public async ValueTask InitializeAsync()
    {
        await using var sql = new SqlConnection(Server); await sql.OpenAsync(TestContext.Current.CancellationToken);
        await using var check = sql.CreateCommand(); check.CommandText = "SELECT DB_ID(@name)"; check.Parameters.AddWithValue("@name", database);
        Assert.Equal(DBNull.Value, await check.ExecuteScalarAsync(TestContext.Current.CancellationToken));
        owned = true;
        factory = new(new SqlConnectionStringBuilder(Server) { InitialCatalog = database }.ConnectionString, clock);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Normal_host_protects_bulk_contact_change_and_confirms_without_logout(bool mfa)
    {
        using var client = factory!.CreateClient(); client.DefaultRequestHeaders.Authorization = null;
        long actorID, targetID; string key;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ShiftIdentityDbContext>();
            Assert.False(scope.ServiceProvider.GetRequiredService<DB>().Database.HasPendingModelChanges());
            var hash = HashService.GenerateVersionedHash(Password);
            var actor = new User { Username = "confirmation-operator", FullName = "Synthetic Operator", IsActive = true, PasswordHash = hash.PasswordHash, Salt = hash.Salt,
                AccessTree = "{\"ShiftIdentityActions\":{\"Users\":[\"r\",\"w\",\"d\"],\"DataLevelAccess\":{\"Countries\":[\"r\",\"w\",\"d\"],\"Regions\":[\"r\",\"w\",\"d\"],\"Companies\":[\"r\",\"w\",\"d\"],\"Branches\":[\"r\",\"w\",\"d\"]}}}" };
            var target = new User { Username = "confirmation-target", FullName = "Synthetic Target", IsActive = true, PasswordHash = hash.PasswordHash, Salt = hash.Salt, Phone = "+12025550140" };
            db.Users.AddRange(actor, target); await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            actorID = actor.ID; targetID = target.ID;
            var state = UserSecurityExpansion.CreateFor(actor);
            if (mfa) MfaMaterial.ProtectActive(scope.ServiceProvider.GetRequiredService<IdentityMaterialProtector>(), state, secret);
            db.Set<UserSecurityState>().AddRange(state, UserSecurityExpansion.CreateFor(target));
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            key = scope.ServiceProvider.GetRequiredService<IHashIdService>().Encode<UserDTO>(targetID);
        }
        var pkce = IdentityHttpHost.Pkce();
        var login = await Read(await client.PostAsJsonAsync("api/identity/v2/login", new PasswordLoginRequest("confirmation-operator", Password, pkce.Challenge), TestContext.Current.CancellationToken));
        if (mfa)
        {
            var challenge = Assert.IsType<ChallengeRequired>(login);
            login = await Send(client, "api/identity/v2/login/mfa", "Operation", challenge.Challenge.Handle!, new CompleteMfaRequest(Code(), pkce.Verifier));
        }
        var session = Assert.IsType<SessionIssued>(login);
        clock.Advance(TimeSpan.FromMinutes(2));
        session = Assert.IsType<SessionIssued>(await Send(client, "api/identity/v2/refresh", "Bearer", session.Session.Token, new RenewSessionRequest(session.Session.RefreshToken)));
        var selection = new SelectStateDTO<UserListDTO> { Items = [new() { ID = key }] };
        using (var request = Request("api/IdentityUser/VerifyPhones", "Bearer", session.Session.Token, selection))
        using (var refusal = await client.SendAsync(request, TestContext.Current.CancellationToken))
        {
            Assert.Equal(HttpStatusCode.Forbidden, refusal.StatusCode);
            Assert.Equal(AdministratorAuthentication.RequiredMessage, (await refusal.Content.ReadFromJsonAsync<ShiftEntityResponse<object>>(TestContext.Current.CancellationToken))!.Message!.For);
        }
        await AssertTarget(false, 0);
        pkce = IdentityHttpHost.Pkce();
        var pending = Assert.IsType<ChallengeRequired>(await Send(client, "api/identity/v2/admin-confirmation", "Bearer", session.Session.Token, new StartPasswordChangeRequest(pkce.Challenge)));
        Assert.Equal(AuthenticationFailure.InvalidProof, Assert.IsType<AuthenticationRefused>(await Send(client, "api/identity/v2/admin-confirmation/password", "Bearer", session.Session.Token,
            new AdministratorPasswordProofRequest(pending.Challenge.Handle!, "wrong", pkce.Verifier))).Code);
        await AssertTarget(false, 0);
        Assert.IsType<OperationCancelled>(await Send(client, "api/identity/v2/operations/cancel", "Operation", pending.Challenge.Handle!, new CancelOperationRequest(pkce.Verifier)));
        await AssertTarget(false, 0);
        pending = Assert.IsType<ChallengeRequired>(await Send(client, "api/identity/v2/admin-confirmation", "Bearer", session.Session.Token, new StartPasswordChangeRequest(pkce.Challenge)));
        var result = await Send(client, "api/identity/v2/admin-confirmation/password", "Bearer", session.Session.Token,
            new AdministratorPasswordProofRequest(pending.Challenge.Handle!, Password, pkce.Verifier));
        if (mfa)
        {
            pending = Assert.IsType<ChallengeRequired>(result);
            await AssertTarget(false, 0);
            result = await Send(client, "api/identity/v2/admin-confirmation/mfa", "Bearer", session.Session.Token,
                new AdministratorMfaProofRequest(pending.Challenge.Handle!, Code(), pkce.Verifier));
        }
        var confirmed = Assert.IsType<SessionIssued>(result);
        Assert.Equal(session.Session.UserData.ID, confirmed.Session.UserData.ID);
        using (var request = Request("api/IdentityUser/VerifyPhones", "Bearer", confirmed.Session.Token, selection))
        using (var applied = await client.SendAsync(request, TestContext.Current.CancellationToken))
            Assert.True(applied.IsSuccessStatusCode, await applied.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        await AssertTarget(true, 1);

        async Task AssertTarget(bool verified, int audits)
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ShiftIdentityDbContext>();
            Assert.Equal(verified, (await db.Users.SingleAsync(x => x.ID == targetID, TestContext.Current.CancellationToken)).PhoneVerified);
            Assert.Equal(audits, await db.Set<AuthenticationAuditEvent>().CountAsync(x => x.UserID == targetID && x.Outcome == "AdminPhoneVerified", TestContext.Current.CancellationToken));
        }
    }

    private string Code() => new Totp(secret).ComputeTotp(clock.GetUtcNow().UtcDateTime);
    private static HttpRequestMessage Request<T>(string route, string scheme, string credential, T body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, route) { Content = JsonContent.Create(body) };
        request.Headers.Authorization = new(scheme, credential); return request;
    }
    private static async Task<AuthOutcome> Send<T>(HttpClient client, string route, string scheme, string credential, T body)
    {
        using var request = Request(route, scheme, credential, body);
        return await Read(await client.SendAsync(request, TestContext.Current.CancellationToken));
    }
    private static async Task<AuthOutcome> Read(HttpResponseMessage response)
    {
        using (response) return (await response.Content.ReadFromJsonAsync<AuthOutcome>(TestContext.Current.CancellationToken))!;
    }

    public async ValueTask DisposeAsync()
    {
        try { if (factory is not null) await factory.DisposeAsync(); }
        finally
        {
            if (owned)
            {
                if (!Regex.IsMatch(database, @"^StockPlusPlus_Test_AdminConfirmation_[a-f0-9]{32}$")) throw new InvalidOperationException("Invalid fixture database name.");
                SqlConnection.ClearAllPools();
                await using var sql = new SqlConnection(Server); await sql.OpenAsync();
                await using var drop = sql.CreateCommand();
                drop.CommandText = $"IF DB_ID(@name) IS NOT NULL BEGIN ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{database}]; END";
                drop.Parameters.AddWithValue("@name", database); await drop.ExecuteNonQueryAsync();
            }
        }
    }

    private sealed class ConfirmationFactory(string connection, TimeProvider clock) : CustomWebApplicationFactory
    {
        protected override IConfigurationBuilder CreateTestConfigurationBuilder() => base.CreateTestConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["ConnectionStrings:SQLServer_Test"] = connection, ["Settings:MfaSettings:Enabled"] = "true", ["Settings:MfaSettings:Mandatory"] = "false",
            ["Settings:Authority:AdministratorAuthenticationGraceSeconds"] = "120" });
        protected override void ConfigureSecurityEmailProviders(IServiceCollection services)
        {
            base.ConfigureSecurityEmailProviders(services);
            services.AddSingleton(clock);
        }
    }
}
#endif
