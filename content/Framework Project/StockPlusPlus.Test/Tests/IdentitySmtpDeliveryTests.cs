#if shiftFrameworkDevelopmentMode && internalShiftIdentityHosting
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using ShiftSoftware.ShiftIdentity.AspNetCore.Authentication;
using ShiftSoftware.ShiftIdentity.Core;
using ShiftSoftware.ShiftIdentity.Core.Authentication;
using ShiftSoftware.ShiftIdentity.Data;
using ShiftSoftware.ShiftIdentity.Data.Authentication;
using ShiftSoftware.ShiftIdentity.Data.Entities;
using StockPlusPlus.API.Services;
using StockPlusPlus.Test.Infrastructure;

namespace StockPlusPlus.Test.Tests;

/// <summary>The ordinary Program, default sink and normal sender, with an owned SQL database and loopback SMTP.</summary>
[Trait("Category", "IdentityConsumer")]
public sealed class IdentitySmtpDeliveryTests : IAsyncLifetime
{
    private const string Server = @"Server=.\SQLEXPRESS;Database=master;Integrated Security=true;Encrypt=true;TrustServerCertificate=true;Connect Timeout=5";
    private readonly string database = "StockPlusPlus_Test_Smtp_" + Guid.NewGuid().ToString("N");
    private readonly LoopbackSmtpServer smtp = new();
    private SmtpFactory? factory;
    private bool owned;

    public async ValueTask InitializeAsync()
    {
        await using var sql = new SqlConnection(Server);
        await sql.OpenAsync(TestContext.Current.CancellationToken);
        await using var check = sql.CreateCommand();
        check.CommandText = "SELECT DB_ID(@name)"; check.Parameters.AddWithValue("@name", database);
        Assert.Equal(DBNull.Value, await check.ExecuteScalarAsync(TestContext.Current.CancellationToken));
        owned = true;
        factory = new SmtpFactory(new SqlConnectionStringBuilder(Server) { InitialCatalog = database }.ConnectionString, smtp.Port);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public async Task Normal_host_renders_admitted_metadata_and_records_actual_SMTP_acceptance(bool verification, bool accepted)
    {
        using var client = factory!.CreateClient();
        client.DefaultRequestHeaders.Authorization = null;
        const string username = "smtp-synthetic-user";
        const string destination = "saved-smtp@example.invalid";
        long userID;
        using (var scope = factory.Services.CreateScope())
        {
            Assert.IsType<SendEmailService>(scope.ServiceProvider.GetRequiredService<ISecurityEmailSender>());
            Assert.Equal("HostSecurityEmailSink", scope.ServiceProvider.GetRequiredService<ISecurityEmailSink>().GetType().Name);
            var db = scope.ServiceProvider.GetRequiredService<ShiftIdentityDbContext>();
            var hash = HashService.GenerateVersionedHash("Original synthetic phrase 84!");
            var user = new User { Username = username, FullName = "Synthetic <Name> & Family", Email = destination,
                IsActive = true, PasswordHash = hash.PasswordHash, Salt = hash.Salt, AccessTree = "{}" };
            db.Users.Add(user); await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            userID = user.ID;
            var state = UserSecurityExpansion.CreateFor(user);
            RecoveryContact.RecordOwnership(user, state, RecoveryEmailProvenance.TrustedAdminAssignment);
            db.Set<UserSecurityState>().Add(state); await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        var kind = verification ? "email-verification" : "password-reset";
        var request = client.PostAsJsonAsync("api/identity/v2/" + kind + "/request", new RequestSecurityEmail(username), TestContext.Current.CancellationToken);
        var mail = await smtp.Message.Task.WaitAsync(TimeSpan.FromSeconds(20), TestContext.Current.CancellationToken);
        Assert.NotNull(mail.HtmlBody); Assert.NotNull(mail.TextBody);
        Assert.False(request.IsCompleted);
        Assert.Equal(destination, Assert.Single(mail.To.Mailboxes).Address);
        Assert.Equal("<" + destination + ">", smtp.EnvelopeRecipient);
        Assert.Contains("Synthetic &lt;Name&gt; &amp; Family", mail.HtmlBody);
        Assert.Contains(username, mail.TextBody);
        DateTimeOffset expiry;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ShiftIdentityDbContext>();
            var operation = await db.Set<AuthenticationOperation>().SingleAsync(x => x.UserID == userID, TestContext.Current.CancellationToken);
            expiry = operation.ExpiresAt;
            Assert.Equal(AuthenticationOperationState.AwaitingExplicitSubmit, operation.State);
            Assert.False((await db.Users.SingleAsync(x => x.ID == userID, TestContext.Current.CancellationToken)).EmailVerified);
        }
        Assert.Contains(expiry.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture), mail.HtmlBody);
        Assert.Contains(expiry.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture), mail.TextBody);
        var url = new Uri(Regex.Match(mail.TextBody, @"https://[^\s]+").Value);
        Assert.Equal("dashboard.example.invalid", url.Host); Assert.Equal("", url.Query);
        Assert.EndsWith(verification ? "/VerifyEmail" : "/ResetPassword", url.AbsolutePath);
        smtp.Acceptance.SetResult(accepted);
        using var response = await request;
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.IsType<SecurityDeliveryRequested>(await response.Content.ReadFromJsonAsync<AuthOutcome>(TestContext.Current.CancellationToken));
        var grant = Uri.UnescapeDataString(url.Fragment.Split("#grant=")[1].Split('&')[0]);
        var purpose = verification ? AuthenticationOperationPurpose.EmailVerify : AuthenticationOperationPurpose.PasswordResetEmail;
        using var opened = await client.PostAsJsonAsync("api/identity/v2/security-link/open", new OpenSecurityLinkRequest(grant, purpose), TestContext.Current.CancellationToken);
        var outcome = await opened.Content.ReadFromJsonAsync<AuthOutcome>(TestContext.Current.CancellationToken);
        if (!accepted) { Assert.IsType<AuthenticationRefused>(outcome); return; }
        var page = Assert.IsType<SecurityLinkOpened>(outcome);
        using var completed = verification
            ? await client.PostAsJsonAsync("api/identity/v2/email-verification/complete", new CompleteEmailVerificationRequest(page.PageHandle), TestContext.Current.CancellationToken)
            : await client.PostAsJsonAsync("api/identity/v2/password-reset/complete", new CompletePasswordResetRequest(page.PageHandle, "New synthetic phrase 85!"), TestContext.Current.CancellationToken);
        var result = await completed.Content.ReadFromJsonAsync<AuthOutcome>(TestContext.Current.CancellationToken);
        if (verification) Assert.IsType<EmailVerificationCompleted>(result); else Assert.IsType<ReturnToLogin>(result);
        using var replay = await client.PostAsJsonAsync("api/identity/v2/security-link/open", new OpenSecurityLinkRequest(grant, purpose), TestContext.Current.CancellationToken);
        Assert.IsType<AuthenticationRefused>(await replay.Content.ReadFromJsonAsync<AuthOutcome>(TestContext.Current.CancellationToken));
    }

    public async ValueTask DisposeAsync()
    {
        try { if (factory is not null) await factory.DisposeAsync(); }
        finally
        {
            try { await smtp.DisposeAsync(); }
            finally
            {
                if (owned)
                {
                    // The random name was verified absent before this factory used it. Never use configured databases.
                    if (!Regex.IsMatch(database, @"^StockPlusPlus_Test_Smtp_[a-f0-9]{32}$")) throw new InvalidOperationException("Invalid fixture database name.");
                    SqlConnection.ClearAllPools();
                    await using var sql = new SqlConnection(Server); await sql.OpenAsync();
                    await using var drop = sql.CreateCommand();
                    drop.CommandText = $"IF DB_ID(@name) IS NOT NULL BEGIN ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{database}]; END";
                    drop.Parameters.AddWithValue("@name", database); await drop.ExecuteNonQueryAsync();
                }
            }
        }
    }

    private sealed class SmtpFactory(string connection, int port) : CustomWebApplicationFactory
    {
        protected override IConfigurationBuilder CreateTestConfigurationBuilder() => base.CreateTestConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:SQLServer_Test"] = connection });

        protected override void ConfigureSecurityEmailProviders(IServiceCollection services)
        {
            // Preserve Program's providers. Replace all bound options so no external SMTP configuration can be used.
            services.RemoveAll<IConfigureOptions<SecurityEmailSmtpOptions>>();
            services.RemoveAll<IPostConfigureOptions<SecurityEmailSmtpOptions>>();
            services.AddSingleton<IOptions<SecurityEmailSmtpOptions>>(Options.Create(new SecurityEmailSmtpOptions
            {
                Host = "127.0.0.1", Port = port, Encryption = SecurityEmailEncryption.None,
                FromAddress = "sender@example.invalid", TimeoutSeconds = 3
            }));
        }
    }
}
#endif
