using Bunit;
using Bunit.TestDoubles;
using ShiftSoftware.ShiftBlazor.Extensions;
using ShiftSoftware.TypeAuth.Blazor.Extensions;
using StockPlusPlus.Shared.Localization;
using Syncfusion.Blazor;
using ShiftSoftware.ShiftIdentity.Blazor.Extensions;
using ShiftSoftware.ShiftIdentity.Dashboard.Blazor.Extensions;

namespace StockPlusPlus.Web.Tests;

public class ShiftBlazorTestContext : TestContext
{
    public ShiftBlazorTestContext()
    {
        Services.AddMockHttpClient();

        Services.AddShiftBlazor(config =>
        {
            config.ShiftConfiguration = options =>
            {
                options.BaseAddress = "http://localhost";
                options.ApiPath = "/api";
                options.UserListEndpoint = "/odata/PublicUser";
            };
        });

        JSInterop.Mode = JSRuntimeMode.Loose;

        //Services.AddShiftIdentity("StockPlusPlus-Dev", "", "", false, typeof(Identity));

        //#if (internalShiftIdentityHosting)
        //Services.AddShiftIdentityDashboardBlazor(x => {});
        //#endif

        Services.AddTypeAuth(o => { });
        Services.AddSyncfusionBlazor();

        this.AddTestAuthorization();
    }
}