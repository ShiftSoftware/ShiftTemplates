using ShiftSoftware.ShiftIdentity.Core;
using ShiftSoftware.ShiftIdentity.Core.DTOs.User;

namespace StockPlusPlus.API.Services;

public class SendEmailService : ISendEmailVerification, ISendEmailResetPassword
{
    public Task SendEmailResetPasswordAsync(string url, UserDataDTO user)
    {
        Console.WriteLine($"Sending reset password {url} to {user.Email}");
        return Task.CompletedTask;
    }

    public Task SendEmailVerificationAsync(string url, UserDataDTO user)
    {
        Console.WriteLine($"Sending email verification {url} to {user.Email}");
        return Task.CompletedTask;
    }
}
