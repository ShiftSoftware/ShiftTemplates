using Bunit;
using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ShiftBlazor.Extensions;
using ShiftSoftware.ShiftIdentity.Blazor.Extensions;
using ShiftSoftware.ShiftIdentity.Dashboard.Blazor.Extensions;
using ShiftSoftware.TypeAuth.Blazor.Extensions;
using StockPlusPlus.Shared.Localization;

namespace StockPlusPlus.Web.Tests;

public class ShiftBlazorTestContext : BunitContext
{
    public ShiftBlazorTestContext()
    {
        Services.AddMockHttpClient();

        Services.AddShiftBlazor(config =>
        {
            config.ShiftConfiguration = options =>
            {
                options.BaseAddress = "http://localhost";
                //options.ApiPath = "/api";
                options.UserListEndpoint = "/odata/PublicUser";
            };
        });

        JSInterop.Mode = JSRuntimeMode.Loose;

//        Services.AddShiftIdentity("StockPlusPlus-Dev", "", "", false, typeof(Identity));

        // Pages inject ShiftIdentityLocalizer directly; register just the localizer
        // (full AddShiftIdentity pulls auth/storage services these render tests don't need).
        Services.AddTransient(x => new ShiftSoftware.ShiftIdentity.Core.Localization.ShiftIdentityLocalizer(x, typeof(ShiftSoftwareLocalization.Identity.Resource)));

//#if (internalShiftIdentityHosting)
//        Services.AddShiftIdentityDashboardBlazor(x => { });
//#endif

        Services.AddTypeAuth(o => { });
        this.AddAuthorization();
    }
}