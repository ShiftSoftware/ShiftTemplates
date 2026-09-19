using ShiftSoftware.ShiftIdentity.Core;
using ShiftSoftware.ShiftIdentity.Core.DTOs.User;

namespace StockPlusPlus.API.Services;

public class SendEmailService : ISendEmailVerification, ISendEmailResetPassword
{
    public Task SendEmailResetPasswordAsync(string url, UserDataDTO user)
    {
        // Sample sender only. A real host accepts delivery here; never log a security link.
        Console.WriteLine("Password reset email accepted by the sample sender.");
        return Task.CompletedTask;
    }

    public Task SendEmailVerificationAsync(string url, UserDataDTO user)
    {
        Console.WriteLine("Email verification accepted by the sample sender.");
        return Task.CompletedTask;
    }
}
