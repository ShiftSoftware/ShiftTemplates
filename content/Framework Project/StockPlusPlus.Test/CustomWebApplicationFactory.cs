using ShiftSoftware.ShiftFrameworkTestingTools;
using ShiftSoftware.ShiftIdentity.Core;
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
            TokenKeySettingKey = "Settings:TokenSettings:PublicKey",
            TokenIssuerSettingKey = "Settings:TokenSettings:Issuer",
            TypeAuthActions = new List<Type>()
            {
#if (includeSampleApp)
                typeof(StockPlusPlusActionTree),
                typeof(ShiftIdentityActions)
#endif
            }
        })
    { }
}