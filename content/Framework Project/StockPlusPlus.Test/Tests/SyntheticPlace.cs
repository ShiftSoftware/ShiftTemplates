#if shiftFrameworkDevelopmentMode && internalShiftIdentityHosting
using ShiftSoftware.ShiftIdentity.Data.Entities;

namespace StockPlusPlus.Test.Tests;

/// <summary>
/// A country, region, company and branch (with a city) for users a test writes straight to the database. The
/// application never saves a user without a region, company and branch, and sign-in refuses one.
/// </summary>
internal static class SyntheticPlace
{
    public static void AssignTo(params User[] users)
    {
        var country = new Country { Name = "Synthetic Country", CallingCode = "+1" };
        var region = new Region { Name = "Synthetic Region", Country = country };
        var company = new Company { Name = "Synthetic Company" };
        var branch = new CompanyBranch
        {
            Name = "Synthetic Branch", Company = company, Region = region,
            City = new City { Name = "Synthetic City", Region = region }
        };

        foreach (var user in users)
        {
            user.Country = country;
            user.Region = region;
            user.Company = company;
            user.CompanyBranch = branch;
        }
    }
}
#endif
