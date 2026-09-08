using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.ShiftIdentity.Core;
using ShiftSoftware.ShiftIdentity.Core.DTOs.Company;
using ShiftSoftware.TypeAuth.Core;
using StockPlusPlus.Data.DbContext;
using StockPlusPlus.Data.Entities;
using StockPlusPlus.Shared.ActionTrees;
using StockPlusPlus.Shared.DTOs.Vehicle;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;

namespace StockPlusPlus.Test.Tests;

/// <summary>
/// Phase 5 SQL Server proof for v2 data-level access. One scoped HTTP lifecycle keeps the canonical two-leg
/// scenario legible while proving the query and row paths use the same company-OR policy.
/// </summary>
[Collection("API Collection")]
public class VehicleDataLevelAccessTests
{
    private const long DealerA = 1;
    private const long DealerB = 2;
    private const long Distributor = 3;
    private const long Intermediary = 4;

    private readonly CustomWebApplicationFactory factory;

    public VehicleDataLevelAccessTests(CustomWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task CompanyOr_IsEnforcedEndToEndOnSqlServer()
    {
        Vehicle dealerAOwned;
        Vehicle intermediaryOwned;
        Vehicle dealerAViaIntermediary;
        Vehicle distributorViaIntermediary;
        Vehicle unassignedViaIntermediary;
        string dealerACompanyKey;
        string intermediaryCompanyKey;
        string dealerAKey;
        string dealerAViaIntermediaryKey;
        string distributorViaIntermediaryKey;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DB>();
            var hashIds = scope.ServiceProvider.GetRequiredService<IHashIdService>();

            dealerAOwned = new Vehicle
            {
                VIN = "DLA-1-DEALER-A-OWNED",
                CompanyID = DealerA,
            };
            var dealerBOwned = new Vehicle
            {
                VIN = "DLA-2-DEALER-B-OWNED",
                CompanyID = DealerB,
            };
            intermediaryOwned = new Vehicle
            {
                VIN = "DLA-3-INTERMEDIARY-OWNED",
                CompanyID = Intermediary,
            };
            dealerAViaIntermediary = new Vehicle
            {
                VIN = "DLA-4-DEALER-A-VIA-INTERMEDIARY",
                CompanyID = DealerA,
                IntermediaryCompanyID = Intermediary,
            };
            distributorViaIntermediary = new Vehicle
            {
                VIN = "DLA-5-DISTRIBUTOR-VIA-INTERMEDIARY",
                CompanyID = Distributor,
                IntermediaryCompanyID = Intermediary,
            };
            unassignedViaIntermediary = new Vehicle
            {
                VIN = "DLA-6-UNASSIGNED-VIA-INTERMEDIARY",
                IntermediaryCompanyID = Intermediary,
            };
            var unassigned = new Vehicle
            {
                VIN = "DLA-7-UNASSIGNED",
            };

            db.Vehicles.AddRange(
                dealerAOwned,
                dealerBOwned,
                intermediaryOwned,
                dealerAViaIntermediary,
                distributorViaIntermediary,
                unassignedViaIntermediary,
                unassigned);
            await db.SaveChangesAsync();

            dealerACompanyKey = hashIds.Encode<CompanyDTO>(DealerA);
            intermediaryCompanyKey = hashIds.Encode<CompanyDTO>(Intermediary);
            dealerAKey = hashIds.Encode<VehicleDTO>(dealerAOwned.ID);
            dealerAViaIntermediaryKey = hashIds.Encode<VehicleDTO>(dealerAViaIntermediary.ID);
            distributorViaIntermediaryKey = hashIds.Encode<VehicleDTO>(distributorViaIntermediary.ID);
        }

        var rowAccess = new[] { Access.Read, Access.Write, Access.Delete };
        var accessTreeJson = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            [nameof(GeneralActionTree)] = new Dictionary<string, object>
            {
                [nameof(GeneralActionTree.DataGridExport)] = new[] { Access.Maximum },
            },
            [nameof(StockPlusPlusActionTree)] = new Dictionary<string, object>
            {
                [nameof(StockPlusPlusActionTree.Vehicle)] = rowAccess,
            },
            [nameof(ShiftIdentityActions)] = new Dictionary<string, object>
            {
                [nameof(ShiftIdentityActions.DataLevelAccess)] = new Dictionary<string, object>
                {
                    [nameof(ShiftIdentityActions.DataLevelAccess.Companies)] = new Dictionary<string, object>
                    {
                        [TypeAuthContext.SelfReferenceKey] = rowAccess,
                    },
                },
            },
        });

        using var client = factory.CreateAuthenticatedClient(
            accessTreeJson,
            new[] { new Claim(ShiftSoftware.ShiftEntity.Core.Constants.CompanyIdClaim, intermediaryCompanyKey) });

        // Query path: company 4 sees rows where CompanyID == 4 OR IntermediaryCompanyID == 4, translated by SQL Server.
        using (var listResponse = await client.GetAsync("api/Vehicle?$top=5"))
        {
            var listBody = await ExpectStatusAsync(listResponse, HttpStatusCode.OK);
            using var listDocument = JsonDocument.Parse(listBody);
            var visibleVins = Property(listDocument.RootElement, "Value")
                .EnumerateArray()
                .Select(row => Property(row, nameof(VehicleListDTO.VIN)).GetString())
                .OrderBy(vin => vin)
                .ToArray();

            Assert.Equal(
                new[]
                {
                    intermediaryOwned.VIN,
                    dealerAViaIntermediary.VIN,
                    distributorViaIntermediary.VIN,
                    unassignedViaIntermediary.VIN,
                }.OrderBy(vin => vin),
                visibleVins);
        }

        // Find: a row reached through the intermediary leg is visible; Dealer A's unrelated row is concealed as 404.
        VehicleDTO editableVehicle;
        using (var findAllowed = await client.GetAsync($"api/Vehicle/{dealerAViaIntermediaryKey}"))
        {
            var body = await ExpectStatusAsync(findAllowed, HttpStatusCode.OK);
            editableVehicle = ResponseEntity<VehicleDTO>(body);
        }

        using (var findDenied = await client.GetAsync($"api/Vehicle/{dealerAKey}"))
            await ExpectErrorAsync(
                findDenied,
                HttpStatusCode.NotFound,
                "Not Found",
                $"Can't find entity with ID '{dealerAKey}'");

        // Insert: either matching leg permits the write; two out-of-scope legs are rejected by row authorization.
        using (var insertAllowed = await client.PostAsJsonAsync("api/Vehicle", new VehicleDTO
        {
            VIN = "DLA-8-INSERT-ALLOWED",
            Company = Select(dealerACompanyKey),
            IntermediaryCompany = Select(intermediaryCompanyKey),
        }))
        {
            var body = await ExpectStatusAsync(insertAllowed, HttpStatusCode.Created);
            Assert.Equal("DLA-8-INSERT-ALLOWED", ResponseEntity<VehicleDTO>(body).VIN);
        }

        using (var insertDenied = await client.PostAsJsonAsync("api/Vehicle", new VehicleDTO
        {
            VIN = "DLA-9-INSERT-DENIED",
            Company = Select(dealerACompanyKey),
        }))
            await ExpectErrorAsync(insertDenied, HttpStatusCode.Forbidden, "Forbidden", "Can Not Create Item");

        // Edit: retaining an accessible leg succeeds; moving both legs outside the scope is rejected before save.
        editableVehicle.VIN = "DLA-4-EDIT-ALLOWED";
        using (var editAllowed = await client.PutAsJsonAsync(
            $"api/Vehicle/{dealerAViaIntermediaryKey}", editableVehicle))
        {
            var body = await ExpectStatusAsync(editAllowed, HttpStatusCode.OK);
            editableVehicle = ResponseEntity<VehicleDTO>(body);
        }

        editableVehicle.Company = Select(dealerACompanyKey);
        editableVehicle.IntermediaryCompany = null;
        using (var editDenied = await client.PutAsJsonAsync(
            $"api/Vehicle/{dealerAViaIntermediaryKey}", editableVehicle))
            await ExpectErrorAsync(editDenied, HttpStatusCode.Forbidden, "Forbidden", "Can Not Update Item");

        // Delete: an intermediary-leg row can be deleted; an unrelated Dealer A row remains concealed as 404.
        using (var deleteAllowed = await client.DeleteAsync($"api/Vehicle/{distributorViaIntermediaryKey}"))
        {
            var body = await ExpectStatusAsync(deleteAllowed, HttpStatusCode.OK);
            Assert.Equal(distributorViaIntermediary.VIN, ResponseEntity<VehicleDTO>(body).VIN);
        }

        using (var deleteDenied = await client.DeleteAsync($"api/Vehicle/{dealerAKey}"))
            await ExpectErrorAsync(
                deleteDenied,
                HttpStatusCode.NotFound,
                "Not Found",
                $"Can't find entity with ID '{dealerAKey}'");
    }

    private static ShiftEntitySelectDTO Select(string value) => new() { Value = value };

    private static T ResponseEntity<T>(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);
        return JsonSerializer.Deserialize<T>(
            Property(document.RootElement, "Entity").GetRawText(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }

    private static JsonElement Property(JsonElement element, string name)
        => element.EnumerateObject()
            .First(property => string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            .Value;

    private static async Task<string> ExpectStatusAsync(HttpResponseMessage response, HttpStatusCode expected)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(
            response.StatusCode == expected,
            $"Expected HTTP {(int)expected} ({expected}), got {(int)response.StatusCode} ({response.StatusCode}). Body: {body}");
        return body;
    }

    private static async Task ExpectErrorAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        string expectedTitle,
        string expectedBody)
    {
        var body = await ExpectStatusAsync(response, expectedStatus);
        using var document = JsonDocument.Parse(body);
        var message = Property(document.RootElement, "Message");

        Assert.Equal(expectedTitle, Property(message, "Title").GetString());
        Assert.Equal(expectedBody, Property(message, "Body").GetString());
    }
}
