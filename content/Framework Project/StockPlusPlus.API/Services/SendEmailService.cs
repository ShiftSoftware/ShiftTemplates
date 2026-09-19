using ShiftSoftware.ShiftIdentity.Core;
using ShiftSoftware.ShiftIdentity.Core.DTOs.User;
using ShiftSoftware.ShiftIdentity.AspNetCore.Authentication;
using ShiftSoftware.ShiftIdentity.Core.Authentication;

namespace StockPlusPlus.API.Services;

public class SendEmailService(SmtpSecurityEmailSender smtp) : ISecurityEmailSender, ISendEmailVerification, ISendEmailResetPassword
{
    public Task SendAsync(SecurityEmailContent message, CancellationToken cancellationToken)
        => smtp.SendAsync(message, cancellationToken);

    // Compatibility for hosts without the authority. The old contracts have neither expiry nor cancellation.
    // The shared template does not invent an expiry, and the SMTP transport still applies its own deadline.
    public Task SendEmailResetPasswordAsync(string url, UserDataDTO user)
        => smtp.SendAsync(SecurityEmailTemplate.RenderLegacy(url, user, AuthenticationOperationPurpose.PasswordResetEmail), CancellationToken.None);

    public Task SendEmailVerificationAsync(string url, UserDataDTO user)
        => smtp.SendAsync(SecurityEmailTemplate.RenderLegacy(url, user, AuthenticationOperationPurpose.EmailVerify), CancellationToken.None);
}
