using System.Runtime.CompilerServices;
using Bunit;

namespace StockPlusPlus.Web.Tests;

/// <summary>
/// bUnit gives WaitForAssertion, WaitForState and WaitForElement one second by default, which the first render of a
/// MudBlazor component can exceed on a loaded machine. WaitFor* returns as soon as its condition holds, so a generous
/// budget costs a passing test nothing. The property is static, so this runs once when the test assembly loads.
/// </summary>
internal static class BunitDefaults
{
    [ModuleInitializer]
    internal static void Apply() => BunitContext.DefaultWaitTimeout = TimeSpan.FromSeconds(30);
}
