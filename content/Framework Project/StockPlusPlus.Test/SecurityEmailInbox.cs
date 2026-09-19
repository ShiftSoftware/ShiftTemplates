using System.Collections.Concurrent;
using ShiftSoftware.ShiftIdentity.Core;
using ShiftSoftware.ShiftIdentity.Core.DTOs.User;

namespace StockPlusPlus.Test;

/// <summary>Test providers behind the real host's framework sink. No email leaves the process.</summary>
public sealed class SecurityEmailInbox : ISendEmailVerification, ISendEmailResetPassword
{
    public sealed record Delivery(bool Verification, string Link, UserDataDTO User);
    private readonly ConcurrentQueue<Delivery> messages = new();
    public Delivery[] Messages => messages.ToArray();
    public Task SendEmailVerificationAsync(string url, UserDataDTO user) => Accept(true, url, user);
    public Task SendEmailResetPasswordAsync(string url, UserDataDTO user) => Accept(false, url, user);
    private Task Accept(bool verification, string link, UserDataDTO user)
    {
        if (user.Email?.EndsWith("@example.invalid", StringComparison.OrdinalIgnoreCase) != true)
            throw new InvalidOperationException("The test inbox accepts reserved synthetic addresses only.");
        messages.Enqueue(new(verification, link, user));
        return Task.CompletedTask;
    }
}
