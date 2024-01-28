
using ShiftSoftware.ShiftFrameworkTestingTools;
using StockPlusPlus.API;
using StockPlusPlus.Data;
using StockPlusPlus.Shared.ActionTrees;

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
                typeof(StockActionTrees)
            }
        })
    { }
}