using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;

namespace StockPlusPlus.Server.Components;

public partial class App
{
    // Pages marked [ExcludeFromInteractiveRouting] (e.g. the cookie-auth login page in
    // ShiftIdentity.Blazor.Server) get a null render mode so they render with static SSR.
    // Static SSR is the only render mode where SignInAsync's Set-Cookie header reaches
    // the browser; everything else runs interactive.
#if (webAssemblyRenderMode)
    private IComponentRenderMode? PageRenderMode =>
        HttpContext.AcceptsInteractiveRouting() ? new InteractiveWebAssemblyRenderMode() : null;
#elif (autoRenderMode)
    private IComponentRenderMode? PageRenderMode =>
        HttpContext.AcceptsInteractiveRouting() ? new InteractiveAutoRenderMode() : null;
#else
    private IComponentRenderMode? PageRenderMode =>
        HttpContext.AcceptsInteractiveRouting() ? new InteractiveServerRenderMode() : null;
#endif
}
