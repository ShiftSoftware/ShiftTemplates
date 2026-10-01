using Microsoft.AspNetCore.Components;
using StockPlusPlus.Web.Pages.SampleContact;

namespace StockPlusPlus.Test.Tests;

public class SampleContactPopupRouteTests
{
    [Fact]
    public void List_and_form_have_distinct_component_name_routes()
    {
        var listRoute = Assert.Single(typeof(SampleContactList).GetCustomAttributes(typeof(RouteAttribute), false))
            as RouteAttribute;
        var formRoute = Assert.Single(typeof(SampleContactForm).GetCustomAttributes(typeof(RouteAttribute), false))
            as RouteAttribute;

        Assert.Equal($"/{nameof(SampleContactList)}", listRoute!.Template);
        Assert.Equal($"/{nameof(SampleContactForm)}/{{Key?}}", formRoute!.Template);
    }
}
