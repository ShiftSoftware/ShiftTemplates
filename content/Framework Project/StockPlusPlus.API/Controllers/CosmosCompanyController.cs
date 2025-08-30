using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Azure.Cosmos;
using Microsoft.OData.UriParser;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model.Flags;
using ShiftSoftware.ShiftEntity.Web.Services;
using ShiftSoftware.ShiftIdentity.Core;
using ShiftSoftware.ShiftIdentity.Core.DTOs.Company;
using ShiftSoftware.TypeAuth.AspNetCore;
using ShiftSoftware.TypeAuth.Core;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace StockPlusPlus.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CosmosCompanyController : ControllerBase
{
    private readonly CosmosClient client;
    private readonly IDefaultDataLevelAccess defaultDataLevelAccess;

    public CosmosCompanyController(CosmosClient client, IDefaultDataLevelAccess defaultDataLevelAccess)
    {
        this.client = client;
        this.defaultDataLevelAccess = defaultDataLevelAccess;
    }

    public class CosmosCompanyListDTO : CompanyListDTO, IEntityHasCompany<CosmosCompanyListDTO>
    {
        public long? CompanyID { get; set; }
    }

    [HttpGet]
    [TypeAuth<ShiftIdentityActions>(nameof(ShiftIdentityActions.Companies), Access.Read)]
    public async Task<IActionResult> Get(ODataQueryOptions<CosmosCompanyListDTO> oDataQueryOptions)
    {
        var container = client.GetContainer("Identity", "Companies");

        var query = container.GetItemLinqQueryable<CosmosCompanyListDTO>(true).Where(x => true);

        query = this.defaultDataLevelAccess.ApplyDefaultFilterOnCompanies(query);

        query = ODataIqueryable.ApplyDefaultSoftDeleteFilter(query, oDataQueryOptions);

        var result = await ODataIqueryable.GetOdataDTOFromIQueryableAsync(query, oDataQueryOptions, Request, false);

        return Ok(result);
    }
}