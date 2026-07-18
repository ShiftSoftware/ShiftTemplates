using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftIdentity.Core.DTOs.User;
using System.Collections.Generic;
using System.Text.Json;

namespace StockPlusPlus.Test.Tests;

// REGRESSION: minimal-API endpoints (attribute-driven CRUD + hand-written endpoint groups like UserEndpoints)
// must serialize with the SAME JSON options as MVC controllers, or a client built against the controller casing
// reads empty fields off minimal-API responses. The framework configures the naming policy on both pipelines
// (ShiftEntity.Web AddShiftEntityHashIdJsonSupport / shared web core); these tests lock that parity in place.
[Collection("API Collection")]
public class MinimalApiJsonParityTests
{
    private readonly CustomWebApplicationFactory factory;

    public MinimalApiJsonParityTests(CustomWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public void MinimalApiJson_NamingPolicy_MatchesMvc()
    {
        using var scope = factory.Services.CreateScope();
        var minimal = scope.ServiceProvider.GetRequiredService<IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>>().Value.SerializerOptions;
        var mvc = scope.ServiceProvider.GetRequiredService<IOptions<Microsoft.AspNetCore.Mvc.JsonOptions>>().Value.JsonSerializerOptions;

        Assert.Equal(mvc.PropertyNamingPolicy, minimal.PropertyNamingPolicy);
    }

    // Concrete proof for the AssignRandomPasswords/VerifyEmails response shape: the data serializes under the app's
    // convention (PascalCase here, JsonNamingPolicy = null) via the minimal-API options — not camelCase.
    [Fact]
    public void MinimalApi_SerializesResponse_LikeMvcController()
    {
        using var scope = factory.Services.CreateScope();
        var minimal = scope.ServiceProvider.GetRequiredService<IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>>().Value.SerializerOptions;

        var response = new ShiftEntityResponse<IEnumerable<UserInfoDTO>>
        {
            Additional = new Dictionary<string, object>
            {
                ["Users"] = new List<UserInfoDTO> { new UserInfoDTO { Username = "alice", PlainTextPassword = "secret" } }
            }
        };

        var json = JsonSerializer.Serialize(response, minimal);

        Assert.Contains("\"Additional\"", json);
        Assert.Contains("\"Username\":\"alice\"", json);
        Assert.Contains("\"PlainTextPassword\":\"secret\"", json);
    }
}
