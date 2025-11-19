using Bunit;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Reflection;
using Xunit;

namespace StockPlusPlus.Web.Tests;

public class PageRenderTests : ShiftBlazorTestContext
{
    [Fact]
    public void ShouldRenderPagesCorrectly()
    {
        List<Type> pages = [];

        var mainPages = typeof(StockPlusPlus.Web.App)
            .Assembly
            .GetTypes()
            .Where(types => types.GetCustomAttribute<RouteAttribute>() != null);

        //var identityPages = typeof(ShiftSoftware.ShiftIdentity.Dashboard.Blazor.ShiftIdentityDashboarBlazorMaker)
        //    .Assembly
        //    .GetTypes()
        //    .Where(types => types.GetCustomAttribute<RouteAttribute>() != null);

        pages.AddRange(mainPages);
        //pages.AddRange(identityPages);

        Assert.All(pages, componentType =>
        {
            using var ctx = new ShiftBlazorTestContext();
            ctx.RenderTree.Add<IncludeMudProviders>();
            ctx.Render<DynamicComponent>(p => p.Add(c => c.Type, componentType));
            ctx.DisposeAsync();
        });
    }
}
