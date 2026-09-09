#if IDENTITY_DEVELOPMENT_APP
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using ShiftIdentity.Tests.Infrastructure;
using ShiftSoftware.ShiftIdentity.Blazor.Services;
using ShiftSoftware.ShiftIdentity.Core;
using ShiftSoftware.ShiftIdentity.Core.Authentication;
using ShiftSoftware.ShiftIdentity.Data.Authentication;
using Xunit;

namespace StockPlusPlus.Test.Tests;

public sealed partial class IdentityDevelopmentAppTests
{
    [Fact]
    public async Task Public_requests_hide_unknown_ineligible_and_throttled_targets_and_use_only_the_saved_destination()
    {
        string? expected = null;
        foreach (var identifier in new[] { "unknown@example.invalid", "dev-legacy", "dev-no-email", "dev-basic@example.invalid", "dev-basic" })
        {
            using var response = await client.PostAsJsonAsync("/api/identity/v2/password-reset/request",
                new { identifier, destination = "attacker@example.invalid", redirectUrl = "https://attacker.invalid" });
            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            expected ??= body;
            Assert.Equal(expected, body);
            Assert.DoesNotContain("dev-basic", body);
        }
        var message = Assert.Single(inbox.Messages);
        Assert.Equal("dev-basic@example.invalid", message.Destination);
        Assert.Equal(1, inbox.DeliveryCalls);
        Assert.StartsWith("/Identity/ResetPassword#grant=", message.Link);
        Assert.EndsWith("&purpose=PasswordResetEmail", message.Link);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Unconfirmed_sender_calls_are_not_retried_and_a_new_request_is_required_after_the_cooldown(bool acceptedBeforeFailure)
    {
        inbox.FailAfterAccept = acceptedBeforeFailure;
        (await client.PostAsJsonAsync("/development/inbox/failure/" + (!acceptedBeforeFailure).ToString().ToLowerInvariant(), new { })).EnsureSuccessStatusCode();
        Assert.IsType<SecurityDeliveryRequested>(await Post("password-reset/request", new RequestSecurityEmail("dev-basic")));
        Assert.Equal(1, inbox.DeliveryCalls);
        var failedMessage = acceptedBeforeFailure ? Assert.Single(inbox.Messages) : null;
        if (!acceptedBeforeFailure) Assert.Empty(inbox.Messages);
        var snapshot = await client.GetFromJsonAsync<JsonElement>("/development/inbox");
        Assert.Equal(acceptedBeforeFailure ? 1 : 0, snapshot.GetProperty("messages").GetArrayLength());
        Assert.False(snapshot.TryGetProperty("deliveries", out _));
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/development/inbox/dispatch", new { })).StatusCode);
        if (failedMessage is not null)
            Assert.IsType<AuthenticationRefused>(await Post("security-link/open", new OpenSecurityLinkRequest(Grant(failedMessage), AuthenticationOperationPurpose.PasswordResetEmail)));
        (await client.PostAsJsonAsync("/development/inbox/failure/false", new { })).EnsureSuccessStatusCode();
        inbox.FailAfterAccept = false;
        Assert.IsType<SecurityDeliveryRequested>(await Post("password-reset/request", new RequestSecurityEmail("dev-basic")));
        Assert.Equal(1, inbox.DeliveryCalls); // The shared cooldown also applies to an unconfirmed handoff.
        clock.Advance(TimeSpan.FromSeconds(61));
        await client.GetFromJsonAsync<JsonElement>("/development/inbox");
        Assert.Equal(1, inbox.DeliveryCalls); // Reading the inbox never sends anything.
        Assert.IsType<SecurityDeliveryRequested>(await Post("password-reset/request", new RequestSecurityEmail("dev-basic")));
        Assert.Equal(2, inbox.DeliveryCalls);
        var replacement = Assert.Single(inbox.Messages.Where(x => x.ID != failedMessage?.ID));
        Assert.IsType<SecurityLinkOpened>(await Post("security-link/open", new OpenSecurityLinkRequest(Grant(replacement), AuthenticationOperationPurpose.PasswordResetEmail)));
    }

    [Fact]
    public async Task Signed_in_and_admin_entry_points_report_unconfirmed_sender_handoffs_without_creating_mail()
    {
        inbox.FailDeliveries = true;
        var targetID = (await fixture.GetSyntheticFactorAsync("dev-mfa")).UserID;
        client.DefaultRequestHeaders.Authorization = new("Bearer", (await Login("dev-admin")).Token);
        var adminFailure = Assert.IsType<AuthenticationRefused>(await Post("password-reset/admin", new AdminPasswordResetRequest(targetID)));
        Assert.Equal(AuthenticationFailure.Unavailable, adminFailure.Code);
        client.DefaultRequestHeaders.Authorization = new("Bearer", (await Login("dev-basic")).Token);
        var profileFailure = Assert.IsType<AuthenticationRefused>(await Post("email-verification/request-current", new { }));
        Assert.Equal(AuthenticationFailure.Unavailable, profileFailure.Code);
        Assert.Equal(2, inbox.DeliveryCalls);
        Assert.Empty(inbox.Messages);
    }

    [Fact]
    public async Task Repeated_link_open_and_scanner_GETs_do_not_consume_or_change_SQL_and_completion_ignores_an_unrelated_browser_session()
    {
        var targetSession = await Login("dev-basic");
        var unrelatedSession = await Login("dev-no-email");
        await Post("password-reset/request", new RequestSecurityEmail("dev-basic"));
        var grant = Grant(Assert.Single(inbox.Messages));
        client.DefaultRequestHeaders.Authorization = new("Bearer", unrelatedSession.Token);
        await using var db = fixture.CreateContext();
        var before = await db.Set<AuthenticationOperation>().AsNoTracking().SingleAsync(x => x.Purpose == AuthenticationOperationPurpose.PasswordResetEmail);
        var beforeUser = await db.Users.AsNoTracking().SingleAsync(u => u.Username == "dev-basic");
        var audits = await db.Set<AuthenticationAuditEvent>().CountAsync();
        foreach (var route in new[] { "security-link/open", "password-reset/complete", "email-verification/complete" })
        {
            using var scanner = new HttpRequestMessage(HttpMethod.Get, "/api/identity/v2/" + route);
            scanner.Headers.Add("Purpose", "prefetch");
            Assert.Equal(HttpStatusCode.MethodNotAllowed, (await client.SendAsync(scanner)).StatusCode);
        }
        var opened = Assert.IsType<SecurityLinkOpened>(await Post("security-link/open", new OpenSecurityLinkRequest(grant, AuthenticationOperationPurpose.PasswordResetEmail)));
        Assert.IsType<SecurityLinkOpened>(await Post("security-link/open", new OpenSecurityLinkRequest(grant, AuthenticationOperationPurpose.PasswordResetEmail)));
        var afterOpen = await db.Set<AuthenticationOperation>().AsNoTracking().SingleAsync(x => x.ID == before.ID);
        Assert.Equal(before.RowVersion, afterOpen.RowVersion);
        Assert.Equal(before.HandleDigest, afterOpen.HandleDigest);
        Assert.Equal(audits, await db.Set<AuthenticationAuditEvent>().CountAsync());
        Assert.Equal(beforeUser.PasswordHash, (await db.Users.AsNoTracking().SingleAsync(u => u.ID == beforeUser.ID)).PasswordHash);

        const string nextPassword = "Synthetic reset password 94!";
        Assert.IsType<ReturnToLogin>(await Post("password-reset/complete", new CompletePasswordResetRequest(opened.PageHandle, nextPassword)));
        Assert.IsType<AuthenticationRefused>(await Post("password-reset/complete", new CompletePasswordResetRequest(opened.PageHandle, "Another synthetic password 6!")));
        var changed = await db.Users.AsNoTracking().SingleAsync(u => u.ID == beforeUser.ID);
        Assert.True(HashService.VerifyVersionedPassword(nextPassword, changed.Salt, changed.PasswordHash));
        Assert.True(changed.EmailVerified);
        Assert.IsType<AuthenticationRefused>(await Post("refresh", new RenewSessionRequest(targetSession.RefreshToken)));
        Assert.IsType<SessionIssued>(await Post("refresh", new RenewSessionRequest(unrelatedSession.RefreshToken)));
        Assert.Equal("dev-no-email", (await client.GetFromJsonAsync<AdmissionAccount>("/api/identity/v2/account"))!.Username);
    }

    [Fact]
    public async Task Verification_gate_exempts_no_email_and_keeps_public_verification_reachable_until_explicit_submit()
    {
        (await client.PostAsJsonAsync("/development/verified-email/true", new { })).EnsureSuccessStatusCode();
        var challenge = Assert.IsType<ChallengeRequired>(await Post("login", new PasswordLoginRequest("dev-legacy", fixture.Password, IdentityHttpHost.Pkce().Challenge)));
        Assert.Equal(AuthenticationStep.EmailVerification, challenge.Challenge.Step);
        Assert.IsType<SessionIssued>(await Post("login", new PasswordLoginRequest("dev-no-email", fixture.Password, IdentityHttpHost.Pkce().Challenge)));
        Assert.IsType<SecurityDeliveryRequested>(await Post("email-verification/request", new RequestSecurityEmail("dev-legacy")));
        var message = Assert.Single(inbox.Messages);
        Assert.StartsWith("/Identity/VerifyEmail#grant=", message.Link);
        var opened = Assert.IsType<SecurityLinkOpened>(await Post("security-link/open", new OpenSecurityLinkRequest(Grant(message), AuthenticationOperationPurpose.EmailVerify)));
        await using var db = fixture.CreateContext();
        Assert.False((await db.Users.AsNoTracking().SingleAsync(u => u.Username == "dev-legacy")).EmailVerified);
        Assert.IsType<EmailVerificationCompleted>(await Post("email-verification/complete", new CompleteEmailVerificationRequest(opened.PageHandle)));
        var user = await db.Users.AsNoTracking().SingleAsync(u => u.Username == "dev-legacy");
        var security = await db.Set<UserSecurityState>().AsNoTracking().SingleAsync(x => x.UserID == user.ID);
        Assert.True(user.EmailVerified);
        Assert.True(RecoveryContact.IsEligible(user, security));
        Assert.Equal(RecoveryEmailProvenance.OwnershipVerification, security.RecoveryEmailProvenance);
        Assert.Equal(1, security.SecurityVersion);
        Assert.IsType<SessionIssued>(await Post("login", new PasswordLoginRequest("dev-legacy", fixture.Password, IdentityHttpHost.Pkce().Challenge)));
        client.DefaultRequestHeaders.Authorization = new("Bearer", (await Login("dev-admin")).Token);
        var admin = await client.GetFromJsonAsync<AdmissionAccount>("/api/identity/v2/account");
        Assert.True(admin!.EmailVerified);
        Assert.True(admin.CanManagePasswordReset);
        Assert.True(admin.CanManageEmailVerification);
    }

    [Fact]
    public async Task Signed_in_and_admin_requests_share_the_budget_and_manual_reset_does_not_verify_email()
    {
        var targetID = (await fixture.GetSyntheticFactorAsync("dev-basic")).UserID;
        client.DefaultRequestHeaders.Authorization = new("Bearer", (await Login("dev-basic")).Token);
        Assert.IsType<AuthenticationRefused>(await Post("password-reset/admin", new AdminPasswordResetRequest(targetID, true)));
        Assert.IsType<SecurityDeliveryRequested>(await Post("email-verification/request-current", new { }));
        client.DefaultRequestHeaders.Authorization = new("Bearer", (await Login("dev-admin")).Token);
        Assert.IsType<SecurityDeliveryRequested>(await Post("password-reset/admin", new AdminPasswordResetRequest(targetID, true)));
        var verification = Assert.Single(inbox.Messages);
        clock.Advance(TimeSpan.FromSeconds(61));
        var manual = Assert.IsType<ManualPasswordResetIssued>(await Post("password-reset/admin", new AdminPasswordResetRequest(targetID, true)));
        Assert.Single(inbox.Messages);
        Assert.Equal(1, inbox.DeliveryCalls);
        var opened = Assert.IsType<SecurityLinkOpened>(await Post("security-link/open", new OpenSecurityLinkRequest(manual.Grant, AuthenticationOperationPurpose.PasswordResetManual)));
        Assert.IsType<ReturnToLogin>(await Post("password-reset/complete", new CompletePasswordResetRequest(opened.PageHandle, "Manual chosen password 75!")));
        await using var db = fixture.CreateContext();
        Assert.False((await db.Users.AsNoTracking().SingleAsync(u => u.ID == targetID)).EmailVerified);
        Assert.IsType<AuthenticationRefused>(await Post("security-link/open", new OpenSecurityLinkRequest(Grant(verification), AuthenticationOperationPurpose.EmailVerify)));
    }

    [Theory]
    [InlineData("dev-restricted", AuthenticationStep.NewMfa)]
    [InlineData("dev-required-mfa", AuthenticationStep.ExistingMfa)]
    public async Task Reset_clears_required_password_change_but_preserves_the_mandatory_factor_gate(string username, AuthenticationStep expected)
    {
        (await client.PostAsJsonAsync("/development/mandatory/true", new { })).EnsureSuccessStatusCode();
        await Post("password-reset/request", new RequestSecurityEmail(username));
        var open = Assert.IsType<SecurityLinkOpened>(await Post("security-link/open", new OpenSecurityLinkRequest(Grant(Assert.Single(inbox.Messages)), AuthenticationOperationPurpose.PasswordResetEmail)));
        const string password = "Mandatory journey password 5!";
        Assert.IsType<ReturnToLogin>(await Post("password-reset/complete", new CompletePasswordResetRequest(open.PageHandle, password)));
        var login = Assert.IsType<ChallengeRequired>(await Post("login", new PasswordLoginRequest(username, password, IdentityHttpHost.Pkce().Challenge)));
        Assert.Equal(expected, login.Challenge.Step);
        await using var db = fixture.CreateContext();
        Assert.False((await db.Users.AsNoTracking().SingleAsync(u => u.Username == username)).RequireChangePassword);
    }

    [Fact]
    public async Task Lost_password_and_factor_require_reset_then_a_new_admin_recovery_and_new_factor_confirmation()
    {
        var targetID = (await fixture.GetSyntheticFactorAsync("dev-recovery")).UserID;
        var previous = await Login("dev-recovery");
        client.DefaultRequestHeaders.Authorization = new("Bearer", (await Login("dev-admin")).Token);
        var staleRecovery = Assert.IsType<MfaRecoveryCodeIssued>(await Post("mfa/recovery-code", new IssueMfaRecoveryRequest(targetID, "Synthetic earlier independent check")));
        client.DefaultRequestHeaders.Authorization = null;
        await Post("password-reset/request", new RequestSecurityEmail("dev-recovery"));
        var opened = Assert.IsType<SecurityLinkOpened>(await Post("security-link/open", new OpenSecurityLinkRequest(Grant(Assert.Single(inbox.Messages)), AuthenticationOperationPurpose.PasswordResetEmail)));
        const string password = "Lost both new password 46!";
        Assert.IsType<ReturnToLogin>(await Post("password-reset/complete", new CompletePasswordResetRequest(opened.PageHandle, password)));
        Assert.IsType<AuthenticationRefused>(await Post("refresh", new RenewSessionRequest(previous.RefreshToken)));
        var pkce = IdentityHttpHost.Pkce();
        var restricted = Assert.IsType<ChallengeRequired>(await Post("login", new PasswordLoginRequest("dev-recovery", password, pkce.Challenge)));
        Assert.Equal(AuthenticationStep.MfaRecovery, restricted.Challenge.Step);
        Assert.IsType<AuthenticationRefused>(await Post("mfa/recover", new RecoverMfaRequest("dev-recovery", password, staleRecovery.Code, pkce.Challenge)));
        clock.Advance(TimeSpan.FromSeconds(31)); // Existing MFA proofs are single use, including the admin's earlier login.
        client.DefaultRequestHeaders.Authorization = new("Bearer", (await Login("dev-admin")).Token);
        var recovery = Assert.IsType<MfaRecoveryCodeIssued>(await Post("mfa/recovery-code", new IssueMfaRecoveryRequest(targetID, "Synthetic new independent check")));
        client.DefaultRequestHeaders.Authorization = null;
        var setup = Assert.IsType<ChallengeRequired>(await Post("mfa/recover", new RecoverMfaRequest("dev-recovery", password, recovery.Code, pkce.Challenge)));
        Assert.NotNull(setup.Challenge.NewAuthenticator);
        var secret = OtpNet.Base32Encoding.ToBytes(setup.Challenge.NewAuthenticator.Secret);
        client.DefaultRequestHeaders.Authorization = new("Operation", setup.Challenge.Handle);
        var code = new OtpNet.Totp(secret).ComputeTotp(clock.GetUtcNow().UtcDateTime);
        var confirmed = Assert.IsType<MfaChanged>(await Post("mfa/confirm", new CompleteMfaRequest(code, pkce.Verifier)));
        Assert.IsType<ReturnToLogin>(confirmed.Continuation);
        clock.Advance(TimeSpan.FromSeconds(31));
        client.DefaultRequestHeaders.Authorization = null;
        var loginProof = IdentityHttpHost.Pkce();
        var challenge = Assert.IsType<ChallengeRequired>(await Post("login", new PasswordLoginRequest("dev-recovery", password, loginProof.Challenge)));
        client.DefaultRequestHeaders.Authorization = new("Operation", challenge.Challenge.Handle);
        Assert.IsType<SessionIssued>(await Post("login/mfa", new CompleteMfaRequest((await fixture.GetSyntheticFactorAsync("dev-recovery")).Code!, loginProof.Verifier)));
    }

    private static string Grant(LocalSecurityMessage message) => QueryHelpers.ParseQuery(message.Link[(message.Link.IndexOf('#') + 1)..])["grant"].ToString();
}
#endif
