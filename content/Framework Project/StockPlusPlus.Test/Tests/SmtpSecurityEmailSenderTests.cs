using Microsoft.Extensions.Options;
using ShiftSoftware.ShiftIdentity.AspNetCore.Authentication;
using StockPlusPlus.API.Services;
using StockPlusPlus.Test.Infrastructure;
using Xunit;

namespace StockPlusPlus.Test.Tests;

[Trait("Category", "IdentityConsumer")]
public sealed class SmtpSecurityEmailSenderTests
{
    public static TheoryData<string, Action<SecurityEmailSmtpOptions>> Invalid => new()
    {
        { "Host", o => o.Host = "" }, { "Port", o => o.Port = 0 },
        { "Encryption", o => o.Encryption = (SecurityEmailEncryption)999 },
        { "Encryption", o => { o.Encryption = SecurityEmailEncryption.None; o.Host = "smtp.example.invalid"; } },
        { "Encryption", o => { o.Encryption = SecurityEmailEncryption.None; o.Username = "synthetic"; o.Password = "synthetic-secret"; } },
        { "FromAddress", o => o.FromAddress = "Sender <sender@example.invalid>" },
        { "FromAddress", o => o.FromAddress = "a@example.invalid,b@example.invalid" },
        { "Username/Password", o => o.Password = "synthetic-secret" },
        { "TimeoutSeconds", o => o.TimeoutSeconds = 0 }
    };

    [Theory, MemberData(nameof(Invalid))]
    public async Task Invalid_configuration_fails_without_network_or_secret_in_error(string field, Action<SecurityEmailSmtpOptions> mutate)
    {
        var options = new SecurityEmailSmtpOptions { Host = "127.0.0.1", FromAddress = "sender@example.invalid" };
        mutate(options);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => new SmtpSecurityEmailSender(Options.Create(options))
            .SendAsync(Content, TestContext.Current.CancellationToken));
        Assert.Contains(field, error.Message); Assert.DoesNotContain("synthetic-secret", error.ToString());
    }

    private static SecurityEmailContent Content => new(Guid.NewGuid(), "saved@example.invalid", "Security message", "<p>Hello</p>", "Hello");
    private static SmtpSecurityEmailSender Sender(LoopbackSmtpServer server, int timeout = 3, SecurityEmailEncryption encryption = SecurityEmailEncryption.None)
        => new(Options.Create(new SecurityEmailSmtpOptions { Host = "127.0.0.1", Port = server.Port,
            Encryption = encryption, FromAddress = "sender@example.invalid", TimeoutSeconds = timeout }));

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Waits_for_SMTP_acceptance_and_propagates_refusal(bool accepted)
    {
        await using var server = new LoopbackSmtpServer();
        var send = Sender(server).SendAsync(Content, TestContext.Current.CancellationToken);
        var message = await server.Message.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        Assert.False(send.IsCompleted);
        Assert.Equal("<saved@example.invalid>", server.EnvelopeRecipient);
        Assert.Equal("saved@example.invalid", Assert.Single(message.To.Mailboxes).Address);
        Assert.Equal("sender@example.invalid", Assert.Single(message.From.Mailboxes).Address);
        Assert.IsType<MimeKit.MultipartAlternative>(message.Body);
        Assert.NotNull(message.HtmlBody); Assert.NotNull(message.TextBody);
        Assert.Equal("<p>Hello</p>", message.HtmlBody.Trim()); Assert.Equal("Hello", message.TextBody.Trim());
        server.Acceptance.SetResult(accepted);
        if (accepted) await send;
        else Assert.Contains("acceptance was not confirmed", (await Assert.ThrowsAsync<InvalidOperationException>(() => send)).Message);
    }

    [Fact]
    public async Task Required_StartTls_refuses_a_server_without_TLS()
    {
        await using var server = new LoopbackSmtpServer();
        await Assert.ThrowsAsync<InvalidOperationException>(() => Sender(server, encryption: SecurityEmailEncryption.StartTls)
            .SendAsync(Content, TestContext.Current.CancellationToken));
        Assert.Null(server.EnvelopeRecipient); Assert.False(server.Message.Task.IsCompleted);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Cancellation_and_transport_deadline_stop_unconfirmed_IO(bool callerCancellation)
    {
        await using var server = new LoopbackSmtpServer();
        using var stop = new CancellationTokenSource();
        var send = Sender(server, callerCancellation ? 10 : 1).SendAsync(Content, stop.Token);
        await server.Message.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        if (callerCancellation) stop.Cancel();
        var error = await Record.ExceptionAsync(() => send.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        if (callerCancellation) Assert.IsAssignableFrom<OperationCanceledException>(error);
        else Assert.True(error is OperationCanceledException or InvalidOperationException);
    }
}
