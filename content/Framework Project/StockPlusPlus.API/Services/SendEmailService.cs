using ShiftSoftware.ShiftIdentity.Core;
using ShiftSoftware.ShiftIdentity.Core.DTOs.User;

namespace StockPlusPlus.API.Services;

public class SendEmailService : ISendEmailVerification
{
    public async Task SendEmailVerificationAsync(string url, UserDataDTO user)
    {
        Console.WriteLine($"Sending {url} to {user.Email}");
    }
}
