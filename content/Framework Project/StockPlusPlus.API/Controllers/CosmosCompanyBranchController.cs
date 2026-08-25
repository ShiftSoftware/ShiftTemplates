using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Azure.Cosmos;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model.Replication.IdentityModels;
using ShiftSoftware.ShiftEntity.Web.Services;
using ShiftSoftware.ShiftIdentity.Core;
using ShiftSoftware.ShiftIdentity.Core.DTOs.CompanyBranch;
using ShiftSoftware.TypeAuth.AspNetCore;
using ShiftSoftware.TypeAuth.Core;
using StockPlusPlus.Data.Projections;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace StockPlusPlus.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CosmosCompanyBranchController : ControllerBase
{
    private readonly CosmosClient client;
    private readonly IDefaultDataLevelAccess defaultDataLevelAccess;

    public CosmosCompanyBranchController(CosmosClient client, IDefaultDataLevelAccess defaultDataLevelAccess)
    {
        this.client = client;
        this.defaultDataLevelAccess = defaultDataLevelAccess;
    }

    [HttpGet]
    [TypeAuth<ShiftIdentityActions>(nameof(ShiftIdentityActions.CompanyBranches), Access.Read)]
    public async Task<IActionResult> Get(ODataQueryOptions<CompanyBranchListDTO> oDataQueryOptions)
    {
        var container = client.GetContainer("Identity", "CompanyBranches");

        var query = container
            .GetItemLinqQueryable<CompanyBranchModel>(true)
            .Where(x => x.ItemType == CompanyBranchContainerItemTypes.Branch)
            .ApplyDefaultRegionFilter(defaultDataLevelAccess)
            .ApplyDefaultCityFilter(defaultDataLevelAccess)
            .ApplyDefaultCompanyFilter(defaultDataLevelAccess)
            .ApplyDefaultBranchFilter(defaultDataLevelAccess);

        // Cosmos documents are not SQL entities, so there is no repository (and no generated mapper) behind
        // this shape — the projection is written out explicitly and pinned by CosmosProjectionParityTests.
        var result = await query
            .Select(CompanyBranchProjections.ToListDTO)
            .ToOdataDTO(oDataQueryOptions, Request, false);

        return Ok(result);
    }
}