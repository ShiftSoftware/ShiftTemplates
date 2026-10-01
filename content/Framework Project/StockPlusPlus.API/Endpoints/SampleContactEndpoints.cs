using Microsoft.AspNetCore.Routing;
using ShiftSoftware.ShiftEntity.Web.Endpoints;
using StockPlusPlus.Data.Entities;
using StockPlusPlus.Data.Repositories;
using StockPlusPlus.Shared.ActionTrees;
using StockPlusPlus.Shared.DTOs.SampleContact;

namespace StockPlusPlus.API.Endpoints;

public static class SampleContactEndpoints
{
    public static IEndpointRouteBuilder MapSampleContactMinimalApi(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapShiftEntitySecureCrud<SampleContactMinimalRepository, SampleContact,
            SampleContactMinimalListDTO, SampleContactMinimalDTO>(
            "api/sample-contact-minimal", StockPlusPlusActionTree.SampleContacts);
        return endpoints;
    }
}
