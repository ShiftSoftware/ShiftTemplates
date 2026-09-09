using StockPlusPlus.Web.Development;
using Xunit;

namespace StockPlusPlus.Web.Tests;

public sealed class IdentityDevelopmentRoutesTests
{
    [Theory]
    [InlineData("Identity/login?ReturnUrl=/", true)]
    [InlineData("Identity/UserDataForm", true)]
    [InlineData("Identity/ChangePasswordForm", true)]
    [InlineData("Identity/TotpEnrollmentForm", true)]
    [InlineData("Identity/UserForm/42", true)]
    [InlineData("Identity/UserList", true)]
    [InlineData("Identity/UserForm", false)]
    [InlineData("Identity/UserForm/42/Edit", false)]
    [InlineData("Identity/UserImportForm", false)]
    [InlineData("Identity/ResetPassword", false)]
    [InlineData("Identity/Auth/AuthCode", false)]
    [InlineData("Identity/SendResetPasswordLink", false)]
    [InlineData("ProductList", false)]
    [InlineData("CosmosCompanyBranchList", false)]
    public void Development_navigation_only_reaches_migrated_screens(string route, bool allowed) =>
        Assert.Equal(allowed, IdentityDevelopmentRoutes.IsAllowed(route));
}
