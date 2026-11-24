using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Reflection;
using Xunit;

namespace StockPlusPlus.Test.Tests;

// Guards the end state of moving ShiftIdentity entirely off MVC controllers, to attribute-driven CRUD and hand-written
// minimal-API endpoint groups. This test fails if ANY ControllerBase subclass reappears in the two ShiftIdentity
// assemblies that used to host controllers. There is deliberately NO allow-list — the move left exactly zero; if a
// view-rendering controller ever has to be kept, add it here explicitly, with a comment that says why. DB-free (pure
// reflection).
public class NoIdentityControllersTests
{
    // Marker types force the two assemblies to load. ShiftIdentity.AspNetCore held the API + MVC Auth controllers;
    // ShiftIdentity.Dashboard.AspNetCore held User/UserManager/IdentityPublicUser/IdentitySync/ReverseTypeAuthLookup
    // (and CompanyCalendar). All are now minimal-API endpoint groups under Endpoints/.
    private static readonly Assembly[] AssembliesThatHeldControllers = new[]
    {
        typeof(Microsoft.AspNetCore.Builder.ShiftIdentityAuthEndpoints).Assembly,
        typeof(ShiftSoftware.ShiftIdentity.Dashboard.AspNetCore.Extentsions.EndpointRouteBuilderExtensions).Assembly,
    };

    [Fact]
    public void ShiftIdentity_Has_No_ControllerBase_Subclasses()
    {
        var controllers = AssembliesThatHeldControllers
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
            .Select(t => t.FullName!)
            .OrderBy(n => n)
            .ToList();

        Assert.True(controllers.Count == 0,
            "Phase 5 migrated ShiftIdentity fully to minimal APIs — no ControllerBase subclasses should remain. Found: "
            + string.Join(", ", controllers));
    }
}
