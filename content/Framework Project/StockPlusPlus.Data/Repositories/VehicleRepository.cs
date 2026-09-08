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

    public override VehicleDTO MapToView(Vehicle entity, MappingContext context = default)
    {
        return new VehicleDTO
        {
            VIN = entity.VIN,
            Company = entity.CompanyID.ToSelectDTO(),
            IntermediaryCompany = entity.IntermediaryCompanyID.ToSelectDTO(),
        }.MapBaseFields(entity);
    }

    public override Vehicle MapToEntity(VehicleDTO dto, Vehicle existing, MappingContext context = default)
    {
        existing.VIN = dto.VIN;
        existing.CompanyID = dto.Company.ToNullableForeignKey();
        existing.IntermediaryCompanyID = dto.IntermediaryCompany.ToNullableForeignKey();
        return existing;
    }

    public override IQueryable<VehicleListDTO> MapToList(
        IQueryable<Vehicle> queryable,
        MappingContext context = default)
    {
        return queryable.Select(vehicle => new VehicleListDTO
        {
            ID = vehicle.ID.ToString(),
            IsDeleted = vehicle.IsDeleted,
            VIN = vehicle.VIN,
            CompanyID = vehicle.CompanyID.HasValue ? vehicle.CompanyID.Value.ToString() : null,
            IntermediaryCompanyID = vehicle.IntermediaryCompanyID.HasValue
                ? vehicle.IntermediaryCompanyID.Value.ToString()
                : null,
        });
    }

    public override void CopyEntity(Vehicle source, Vehicle target, MappingContext context = default)
    {
        source.CopyBaseFields(target);
        target.VIN = source.VIN;
        target.CompanyID = source.CompanyID;
        target.IntermediaryCompanyID = source.IntermediaryCompanyID;
    }
}
