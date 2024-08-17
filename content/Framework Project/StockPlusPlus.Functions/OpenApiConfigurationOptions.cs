using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;

namespace StockPlusPlus.Functions;
public class OpenApiConfigurationOptions : DefaultOpenApiConfigurationOptions
{
    public override List<OpenApiServer> Servers { get; set; } = new List<OpenApiServer>()
    {
        new OpenApiServer() {
            Url = "https://localhost:7143/api",
            Description = "Production API",
        }
    };
    public override bool IncludeRequestingHostName { get; set; } = true;
    public override OpenApiVersionType OpenApiVersion { get; set; } = OpenApiVersionType.V3;

    public override OpenApiInfo Info { get; set; } = new OpenApiInfo()
    {
        Title = "Functions API Documentation",
        Description = "Functions APIs Documentation",
        Version = "1",
    };
}