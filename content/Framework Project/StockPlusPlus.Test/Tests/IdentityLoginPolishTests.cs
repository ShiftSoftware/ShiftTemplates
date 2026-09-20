#if shiftFrameworkDevelopmentMode && internalShiftIdentityHosting
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShiftIdentity.Tests.Infrastructure;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftIdentity.Core;
using ShiftSoftware.ShiftIdentity.Core.Authentication;
using ShiftSoftware.ShiftIdentity.Core.DTOs;
using ShiftSoftware.ShiftIdentity.Data;
using ShiftSoftware.ShiftIdentity.Data.Authentication;
using ShiftSoftware.ShiftIdentity.Data.Entities;

namespace StockPlusPlus.Test.Tests;

[Trait("Category", "IdentityConsumer")]
public sealed class IdentityLoginPolishTests : IAsyncLifetime
{
    private const string Server = @"Server=.\SQLEXPRESS;Database=master;Integrated Security=true;Encrypt=true;TrustServerCertificate=true;Connect Timeout=5";
    private const string Username = "Synthetic  Login", Password = "  Synthetic login phrase 39!\t";
    private readonly string database = "StockPlusPlus_Test_LoginPolish_" + Guid.NewGuid().ToString("N");
    private LoginFactory? factory;
    private bool owned;

    public async ValueTask InitializeAsync()
    {
        await using var sql = new SqlConnection(Server); await sql.OpenAsync(TestContext.Current.CancellationToken);
        await using var check = sql.CreateCommand(); check.CommandText = "SELECT DB_ID(@name)"; check.Parameters.AddWithValue("@name", database);
        Assert.Equal(DBNull.Value, await check.ExecuteScalarAsync(TestContext.Current.CancellationToken));
        owned = true;
        factory = new(new SqlConnectionStringBuilder(Server) { InitialCatalog = database }.ConnectionString);
    }

    [Theory]
    [InlineData(false, "Synthetic  Login")]
    [InlineData(true, "synthetic-login@example.invalid")]
    public async Task Normal_combined_host_trims_login_and_only_explicit_reset_submission_requests_email(bool staged, string resetIdentifier)
    {
        using var client = factory!.CreateClient(); client.DefaultRequestHeaders.Authorization = null;
        using (var scope = factory.Services.CreateScope())
        {
            Assert.Equal(72000, scope.ServiceProvider.GetRequiredService<ShiftIdentityConfiguration>().Authority.AdministratorAuthenticationGraceSeconds);
            var db = scope.ServiceProvider.GetRequiredService<ShiftIdentityDbContext>();
            var hash = HashService.GenerateVersionedHash(Password);
            var user = new User { Username = Username, FullName = "Synthetic Login User", IsActive = true,
                PasswordHash = hash.PasswordHash, Salt = hash.Salt, Email = "synthetic-login@example.invalid", EmailVerified = false };
            db.Users.Add(user); await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            var state = UserSecurityExpansion.CreateFor(user);
            RecoveryContact.RecordOwnership(user, state, RecoveryEmailProvenance.TrustedAdminAssignment);
            db.Set<UserSecurityState>().Add(state);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        Assert.True(await Login(" \t" + Username + "\r\n", Password));
        Assert.False(await Login(Username, Password.Trim()));
        Assert.False(await Login(" \t ", Password));
        Assert.Empty(factory.SecurityEmails.Messages);
        // The dashboard component tests cover the inert reset page; this host checks the explicit submission.
        using var reset = await client.PostAsJsonAsync("api/identity/v2/password-reset/request", new RequestSecurityEmail(resetIdentifier), TestContext.Current.CancellationToken);
        Assert.IsType<SecurityDeliveryRequested>(await reset.Content.ReadFromJsonAsync<AuthOutcome>(TestContext.Current.CancellationToken));
        var delivered = Assert.Single(factory.SecurityEmails.Messages);
        Assert.False(delivered.Verification); Assert.Equal(Username, delivered.User.Username);

        async Task<bool> Login(string username, string password)
        {
            if (staged)
            {
                using var response = await client.PostAsJsonAsync("api/identity/v2/login", new PasswordLoginRequest(username, password, IdentityHttpHost.Pkce().Challenge), TestContext.Current.CancellationToken);
                var result = await response.Content.ReadFromJsonAsync<AuthOutcome>(TestContext.Current.CancellationToken);
                Assert.True(result is SessionIssued or AuthenticationRefused);
                return result is SessionIssued;
            }
            using var responseLegacy = await client.PostAsJsonAsync("api/Auth/Login", new LoginDTO { Username = username, Password = password }, TestContext.Current.CancellationToken);
            var envelope = await responseLegacy.Content.ReadFromJsonAsync<ShiftEntityResponse<TokenDTO>>(TestContext.Current.CancellationToken);
            Assert.Equal(responseLegacy.IsSuccessStatusCode, envelope?.Entity is not null);
            return responseLegacy.IsSuccessStatusCode;
        }
    }

    public async ValueTask DisposeAsync()
    {
        try { if (factory is not null) await factory.DisposeAsync(); }
        finally
        {
            if (owned)
            {
                if (!Regex.IsMatch(database, @"^StockPlusPlus_Test_LoginPolish_[a-f0-9]{32}$")) throw new InvalidOperationException("Invalid fixture database name.");
                SqlConnection.ClearAllPools();
                await using var sql = new SqlConnection(Server); await sql.OpenAsync();
                await using var drop = sql.CreateCommand();
                drop.CommandText = $"IF DB_ID(@name) IS NOT NULL BEGIN ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{database}]; END; SELECT DB_ID(@name);";
                drop.Parameters.AddWithValue("@name", database);
                Assert.Equal(DBNull.Value, await drop.ExecuteScalarAsync());
            }
        }
    }

    private sealed class LoginFactory(string connection) : CustomWebApplicationFactory
    {
        protected override IConfigurationBuilder CreateTestConfigurationBuilder() => base.CreateTestConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["ConnectionStrings:SQLServer_Test"] = connection });
    }
}
#endif
