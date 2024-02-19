using ShiftSoftware.ShiftIdentity.Blazor;
using ShiftSoftware.ShiftIdentity.Core;
#if(internalShiftIdentityHosting)
using ShiftSoftware.ShiftIdentity.Dashboard.Blazor;
#endif

namespace StockPlusPlus.Web;

public partial class App
{
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
