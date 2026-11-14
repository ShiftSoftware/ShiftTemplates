using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.ShiftIdentity.Core;
using ShiftSoftware.ShiftIdentity.Core.DTOs.Company;
using StockPlusPlus.Data.DbContext;
using StockPlusPlus.Data.Entities;
using StockPlusPlus.Shared.DTOs.Vehicle;

namespace StockPlusPlus.Data.Repositories;

/// <summary>
/// Demonstrates an explicit v2 dimension that replaces the standard Company marker rule. Both company columns are
/// keys in the same dimension, so access to either company grants access on both query and row paths.
/// Mapping is AUTOMATIC: ShiftMapper declares the four maps from the type arguments, so nothing is written here.
/// </summary>
public class VehicleRepository : ShiftRepository<DB, Vehicle, VehicleListDTO, VehicleDTO>
{
    public VehicleRepository(DB db) : base(db, options =>
    {
        options.DataLevelAccess(access =>
        {
            access.On(ShiftIdentityActions.DataLevelAccess.Companies)
                .Keys(x => x.CompanyID, x => x.IntermediaryCompanyID)
                .HashId<CompanyDTO>()
                .Self(ShiftSoftware.ShiftEntity.Core.Constants.CompanyIdClaim);
        });
    })
    {
    }
}
