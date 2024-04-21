using ShiftSoftware.ShiftIdentity.Core;
using ShiftSoftware.ShiftIdentity.Core.DTOs.User;

namespace StockPlusPlus.API.Services;

public class SendEmailService : ISendEmailVerification, ISendEmailResetPassword
{
    public async Task SendEmailResetPasswordAsync(string url, UserDataDTO user)
    {
        Console.WriteLine($"Sending reset password {url} to {user.Email}");
    }

    public async Task SendEmailVerificationAsync(string url, UserDataDTO user)
    {
        Console.WriteLine($"Sending email verification {url} to {user.Email}");
    }
}
