using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Azure.Cosmos;
using Microsoft.OData.UriParser;
using ShiftSoftware.ShiftEntity.Web.Services;
using ShiftSoftware.ShiftIdentity.Core;
using ShiftSoftware.ShiftIdentity.Core.DTOs.Country;
using ShiftSoftware.TypeAuth.AspNetCore;
using ShiftSoftware.TypeAuth.Core;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace StockPlusPlus.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[TypeAuth<ShiftIdentityActions>(nameof(ShiftIdentityActions.Services), Access.Read)]
public class ServiceController : ControllerBase
{
    private readonly CosmosClient client;

    public ServiceController(CosmosClient client)
    {
        this.client = client;
    }

   
    [HttpGet]
    public async Task<IActionResult> Get(ODataQueryOptions<CountryDTO> oDataQueryOptions)
    {

        var container = client.GetContainer("Identity", "Countries");

        var query = container.GetItemLinqQueryable<CountryDTO>(true).Where(x => true);

        //Copied directly from GetOdataListingNew in ShiftEntityWeb, below should be implemented on the framework level.

        var typeAuthService = this.HttpContext.RequestServices.GetRequiredService<ITypeAuthService>();

        if (!typeAuthService.CanRead(ShiftIdentityActions.Countries))
            return Forbid();

        bool isFilteringByIsDeleted = false;

        FilterClause? filterClause = oDataQueryOptions.Filter?.FilterClause;

        if (filterClause is not null)
        {
            var visitor = new SoftDeleteQueryNodeVisitor();

            var visited = filterClause.Expression.Accept(visitor);

            isFilteringByIsDeleted = visitor.IsFilteringByIsDeleted;
        }

        if (!isFilteringByIsDeleted)
            query = query.Where(x => x.IsDeleted == false);

        var result = await ODataIqueryable.GetOdataDTOFromIQueryableAsync(query, oDataQueryOptions, Request, false);

        return Ok(result);
    }
}