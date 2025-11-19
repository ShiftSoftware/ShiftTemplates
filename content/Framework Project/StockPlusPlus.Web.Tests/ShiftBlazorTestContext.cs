using Bunit;
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

//#if (internalShiftIdentityHosting)
//        Services.AddShiftIdentityDashboardBlazor(x => { });
//#endif

        Services.AddTypeAuth(o => { });
        this.AddAuthorization();
    }
}