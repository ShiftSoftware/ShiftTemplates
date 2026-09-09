using Microsoft.AspNetCore.Components.Authorization;
using ShiftSoftware.ShiftIdentity.Blazor;
using ShiftSoftware.ShiftIdentity.Blazor.Services;

namespace StockPlusPlus.Web.Development;

internal static class IdentityDevelopmentClient
{
    internal static void Configure(IServiceCollection services, string origin, string runID)
    {
        // A raw client keeps refresh and operation credentials out of the normal bearer handler.
        services.AddScoped(_ => new DevelopmentTransport { BaseAddress = new Uri(origin) });
        services.AddScoped(sp => new AdmissionSessionStore(sp.GetRequiredService<DevelopmentTransport>(),
            sp.GetRequiredService<Blazored.LocalStorage.ISyncLocalStorageService>(), "identity-development-" + runID));
        services.AddScoped<IIdentityStore>(sp => sp.GetRequiredService<AdmissionSessionStore>());
        services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<AdmissionSessionStore>());
        services.AddScoped(sp => new AuthenticationFlow(sp.GetRequiredService<DevelopmentTransport>(), sp.GetRequiredService<IIdentityStore>()));
        services.AddScoped(sp => new AdmissionUiContext(sp.GetRequiredService<AuthenticationFlow>(), sp.GetRequiredService<IIdentityStore>(), sp.GetRequiredService<DevelopmentTransport>()));
    }
    private sealed class DevelopmentTransport : HttpClient;
}

public static class IdentityDevelopmentRoutes
{
    public static bool IsAllowed(string path)
    {
        var route = path.Split('?', '#')[0].Trim('/');
        if (new[] { "", "Identity/login", "Identity/UserDataForm", "Identity/UserList", "Identity/ChangePasswordForm",
            "Identity/TotpEnrollmentForm", "Identity/SendResetPasswordLink", "Identity/ResetPassword", "Identity/SendEmailVerificationLink",
            "Identity/VerifyEmail", "development/tools", "development/unavailable" }.Contains(route, StringComparer.OrdinalIgnoreCase)) return true;
        var parts = route.Split('/');
        return parts.Length == 3 && parts[0].Equals("Identity", StringComparison.OrdinalIgnoreCase) &&
            parts[1].Equals("UserForm", StringComparison.OrdinalIgnoreCase) && long.TryParse(parts[2], out var id) && id > 0;
    }
}
