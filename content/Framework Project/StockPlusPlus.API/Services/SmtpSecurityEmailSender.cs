using System.Net;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using ShiftSoftware.ShiftIdentity.AspNetCore.Authentication;

namespace StockPlusPlus.API.Services;

public enum SecurityEmailEncryption { StartTls, SslOnConnect, None }

/// <summary>SMTP settings for this host. Credentials belong in local secrets or deployment configuration.</summary>
public sealed class SecurityEmailSmtpOptions
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public SecurityEmailEncryption Encryption { get; set; } = SecurityEmailEncryption.StartTls;
    public string FromAddress { get; set; } = "";
    public string FromName { get; set; } = "Account security";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public int TimeoutSeconds { get; set; } = 3;

    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(Host) || Host.Any(char.IsWhiteSpace) || Host.Contains('/') || Host.Contains('@')) Invalid(nameof(Host));
        if (Port is < 1 or > 65535) Invalid(nameof(Port));
        if (!Enum.IsDefined(Encryption)) Invalid(nameof(Encryption));
        if (!MailboxAddress.TryParse(FromAddress, out var from) || from.Address != FromAddress || !FromAddress.Contains('@')) Invalid(nameof(FromAddress));
        if (string.IsNullOrEmpty(Username) != string.IsNullOrEmpty(Password)) Invalid("Username/Password");
        if (TimeoutSeconds is < 1 or > 120) Invalid(nameof(TimeoutSeconds));
        // Clear text is for a fixture-owned loopback server only, never for credentials or remote delivery.
        if (Encryption == SecurityEmailEncryption.None &&
            (!(Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || IPAddress.TryParse(Host, out var ip) && IPAddress.IsLoopback(ip))
             || !string.IsNullOrEmpty(Username))) Invalid(nameof(Encryption));
    }

    private static void Invalid(string name) => throw new InvalidOperationException("Security email SMTP configuration requires a valid " + name + ".");
}

/// <summary>This host's SMTP transport. Other hosts can implement ISecurityEmailSender with their own delivery provider.</summary>
public sealed class SmtpSecurityEmailSender(IOptions<SecurityEmailSmtpOptions> options) : ISecurityEmailSender
{
    public async Task SendAsync(SecurityEmailContent message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var settings = options.Value;
        settings.Validate();
        if (!MailboxAddress.TryParse(message.Destination, out var recipient) || recipient.Address != message.Destination || !message.Destination.Contains('@'))
            throw new InvalidOperationException("The admitted security email destination must be one mailbox address.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(settings.TimeoutSeconds));
        using var mail = new MimeMessage();
        mail.From.Add(new MailboxAddress(settings.FromName, settings.FromAddress));
        mail.To.Add(recipient);
        mail.Subject = message.Subject;
        mail.Body = new BodyBuilder { HtmlBody = message.HtmlBody, TextBody = message.TextBody }.ToMessageBody();
        using var client = new SmtpClient { Timeout = settings.TimeoutSeconds * 1000 };
        var encryption = settings.Encryption switch
        {
            SecurityEmailEncryption.StartTls => SecureSocketOptions.StartTls,
            SecurityEmailEncryption.SslOnConnect => SecureSocketOptions.SslOnConnect,
            _ => SecureSocketOptions.None
        };
        try
        {
            await client.ConnectAsync(settings.Host, settings.Port, encryption, deadline.Token);
            if (!string.IsNullOrEmpty(settings.Username))
                await client.AuthenticateAsync(settings.Username, settings.Password, deadline.Token);
            await client.SendAsync(mail, deadline.Token);
            // SendAsync has received the SMTP acceptance response. Dispose closes the connection without another
            // awaited exchange: a failed QUIT must not turn confirmed acceptance into an unconfirmed handoff.
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception)
        {
            // A server response may contain recipient data or message content. Do not attach or log it.
            throw new InvalidOperationException("SMTP security email acceptance was not confirmed. Check the host's SMTP configuration and connectivity.");
        }
    }
}
