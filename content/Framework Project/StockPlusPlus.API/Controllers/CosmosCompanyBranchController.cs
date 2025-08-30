using AutoMapper;
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

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace StockPlusPlus.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CosmosCompanyBranchController : ControllerBase
{
    private readonly CosmosClient client;
    private readonly IDefaultDataLevelAccess defaultDataLevelAccess;
    private readonly IMapper mapper;

    public CosmosCompanyBranchController(CosmosClient client, IDefaultDataLevelAccess defaultDataLevelAccess, IMapper mapper)
    {
        this.client = client;
        this.defaultDataLevelAccess = defaultDataLevelAccess;
        this.mapper = mapper;
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

        var listDto = mapper
            .ProjectTo<CompanyBranchListDTO>(query)
            .ApplyDefaultSoftDeleteFilter(oDataQueryOptions);

        var result = await ODataIqueryable.GetOdataDTOFromIQueryableAsync(
            listDto,
            oDataQueryOptions,
            Request,
            false
        );

        return Ok(result);
    }
}