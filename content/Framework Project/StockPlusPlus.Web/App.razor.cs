using ShiftSoftware.ShiftIdentity.Blazor;
using ShiftSoftware.ShiftIdentity.Core;
#if(internalShiftIdentityHosting)
using ShiftSoftware.ShiftIdentity.Dashboard.Blazor;
#endif

namespace StockPlusPlus.Web;

public partial class App
{
    [Microsoft.AspNetCore.Components.Inject] private IServiceProvider Services { get; set; } = null!;
    [Microsoft.AspNetCore.Components.Inject] private Microsoft.AspNetCore.Components.NavigationManager Navigation { get; set; } = null!;
    private ShiftSoftware.ShiftIdentity.Blazor.Services.AdmissionUiContext? AdmissionContext =>
        Services.GetService<ShiftSoftware.ShiftIdentity.Blazor.Services.AdmissionUiContext>();
    private async Task CheckDevelopmentRoute(Microsoft.AspNetCore.Components.Routing.NavigationContext navigation)
    {
        if (AdmissionContext is null) return;
        await AdmissionContext.Flow.CancelAsync();
        if (!Development.IdentityDevelopmentRoutes.IsAllowed(navigation.Path))
            Navigation.NavigateTo("development/unavailable", replace: true);
    }
    private ShiftIdentityHostingTypes shiftIdentityHostingTypes;
    private List<System.Reflection.Assembly> additionalAssemblies;

    public App()
    {
#if (internalShiftIdentityHosting)
    additionalAssemblies = new() { typeof(ShiftIdentityBlazorMaker).Assembly, typeof(ShiftIdentityDashboarBlazorMaker).Assembly };
    shiftIdentityHostingTypes = ShiftIdentityHostingTypes.Internal;
#endif
#if (externalShiftIdentityHosting)
        additionalAssemblies = new() { typeof(ShiftIdentityBlazorMaker).Assembly };
        shiftIdentityHostingTypes = ShiftIdentityHostingTypes.External;
#endif
    }
}
