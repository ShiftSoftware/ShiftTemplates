using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Azure.Cosmos;
using ShiftSoftware.ShiftIdentity.Core;
using ShiftSoftware.TypeAuth.AspNetCore;
using ShiftSoftware.TypeAuth.Core;
using StockPlusPlus.Shared.DTOs.Service;

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

    // GET: api/<ServiceController>
    [HttpGet]
    [CustomEnableQueryAttribute]
    public IActionResult Get()
    {
        var container = client.GetContainer("Identity", "Services");
        var query = container.GetItemLinqQueryable<ServiceListDTO>(true);

        return Ok(query);
    }
}

public class CustomEnableQueryAttribute : EnableQueryAttribute
{
    public override IQueryable ApplyQuery(IQueryable queryable, ODataQueryOptions queryOptions)
    {
        if (queryOptions.Count?.Value == true)
        {
            // Handle $count manually
            var count = ((IQueryable<object>)queryable).Count(); // Convert to list before counting
            queryOptions.Request.ODataFeature().TotalCount = (long)count; // Cast to long

            // Remove $count from the query options to prevent further processing
            queryOptions = new ODataQueryOptions(queryOptions.Context, queryOptions.Request);
        }

        return base.ApplyQuery(queryable, queryOptions);
    }
}
