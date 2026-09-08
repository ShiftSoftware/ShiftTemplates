using ShiftSoftware.ShiftFrameworkTestingTools;
using ShiftSoftware.ShiftIdentity.Core;
using Microsoft.Extensions.Configuration;
using StockPlusPlus.API;
using StockPlusPlus.Data.DbContext;

#if (includeSampleApp)
using StockPlusPlus.Shared.ActionTrees;
#endif

namespace StockPlusPlus.Test;

public class CustomWebApplicationFactory : ShiftCustomWebApplicationFactory<WebMarker, DB>
{
    public CustomWebApplicationFactory() : base(
        "SQLServer_Test",
        new ShiftCustomWebApplicationBearerAuthSettings
        {
            Enabled = true,
            // The API validates RSA-signed tokens (AddShiftIdentity), so the test token must be
            // signed with the private key — the public key would silently produce 401s.
            TokenKeySettingKey = "Settings:TokenSettings:PrivateKey",
            TokenIssuerSettingKey = "Settings:TokenSettings:Issuer",
            TypeAuthActions = new List<Type>()
            {
                // Framework tree: DataGridExport permits list GETs without a $top restriction.
                typeof(ShiftSoftware.ShiftEntity.Core.GeneralActionTree),
#if (includeSampleApp)
                typeof(StockPlusPlusActionTree),
                typeof(ShiftIdentityActions)
#endif
            }
        })
    { }

    // Verification runs must explicitly select a test override, such as SHIFT_TEST_ConnectionStrings__SQLServer_Test.
    protected override IConfigurationBuilder CreateTestConfigurationBuilder()
        => base.CreateTestConfigurationBuilder().AddEnvironmentVariables("SHIFT_TEST_");
}
