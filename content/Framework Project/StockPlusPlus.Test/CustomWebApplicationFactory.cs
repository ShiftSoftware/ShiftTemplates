
using ShiftSoftware.ShiftFrameworkTestingTools;
using StockPlusPlus.API;
using StockPlusPlus.Data;
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
            TokenKeySettingKey = "Settings:TokenSettings:Key",
            TokenIssuerSettingKey = "Settings:TokenSettings:Issuer",
            TypeAuthActions = new List<Type>()
            {
#if (includeSampleApp)
                typeof(StockActionTrees)
#endif
            }
        })
    { }
}