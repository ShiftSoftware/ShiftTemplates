using ShiftSoftware.TypeAuth.Core;
using StockPlusPlus.Shared.ActionTrees;
using StockPlusPlus.Shared.DTOs.Car;
using System.Net;
using System.Net.Http.Json;

namespace StockPlusPlus.Test.Tests;

/// <summary>
/// Proves the API — not just the UI — enforces <see cref="StockPlusPlusActionTree.Car"/>: CarController passes the
/// action to the secure controller, which checks Read for list/view, Write for insert/update and Delete for delete.
/// Each client carries only the access tree it is given, so a refused call is refused by the server.
/// </summary>
[Collection("API Collection")]
public class CarPermissionTests
{
    private readonly CustomWebApplicationFactory factory;

    public CarPermissionTests(CustomWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task FullAccessUser_CanListViewCreateUpdateAndDelete()
    {
        using var client = ClientWithCarAccess(Access.Read, Access.Write, Access.Delete);
        var make = UniqueMake();

        var created = await CreateAsync(client, new CarDTO { Make = make, Model = "Corolla", Year = 2020 });

        using (var list = await client.GetAsync(ListUrl(make)))
            Assert.Equal([created.ID], ListedIds(await ExpectStatusAsync(list, HttpStatusCode.OK)));

        using (var view = await client.GetAsync($"api/Car/{created.ID}"))
            Assert.Equal("Corolla", ResponseEntity<CarDTO>(await ExpectStatusAsync(view, HttpStatusCode.OK)).Model);

        created.Model = "Camry";
        using (var update = await client.PutAsJsonAsync($"api/Car/{created.ID}", created))
            Assert.Equal("Camry", ResponseEntity<CarDTO>(await ExpectStatusAsync(update, HttpStatusCode.OK)).Model);

        using (var delete = await client.DeleteAsync($"api/Car/{created.ID}"))
            await ExpectStatusAsync(delete, HttpStatusCode.OK);

        using (var listAfterDelete = await client.GetAsync(ListUrl(make)))
            Assert.Empty(ListedIds(await ExpectStatusAsync(listAfterDelete, HttpStatusCode.OK)));
    }

    [Fact]
    public async Task ReadOnlyUser_CanListAndView_ButTheApiRefusesCreateUpdateAndDelete()
    {
        using var admin = ClientWithCarAccess(Access.Read, Access.Write, Access.Delete);
        using var readOnly = ClientWithCarAccess(Access.Read);
        var make = UniqueMake();

        var existing = await CreateAsync(admin, new CarDTO { Make = make, Model = "Civic", Year = 2018 });

        // Allowed: Read.
        using (var list = await readOnly.GetAsync(ListUrl(make)))
            Assert.Equal([existing.ID], ListedIds(await ExpectStatusAsync(list, HttpStatusCode.OK)));

        using (var view = await readOnly.GetAsync($"api/Car/{existing.ID}"))
            Assert.Equal("Civic", ResponseEntity<CarDTO>(await ExpectStatusAsync(view, HttpStatusCode.OK)).Model);

        // Refused: Write (insert and update) and Delete.
        using (var insert = await readOnly.PostAsJsonAsync("api/Car", new CarDTO { Make = make, Model = "Accord", Year = 2021 }))
            await ExpectStatusAsync(insert, HttpStatusCode.Forbidden);

        var edited = new CarDTO { ID = existing.ID, Make = make, Model = "Hacked", Year = 1999 };
        using (var update = await readOnly.PutAsJsonAsync($"api/Car/{existing.ID}", edited))
            await ExpectStatusAsync(update, HttpStatusCode.Forbidden);

        using (var delete = await readOnly.DeleteAsync($"api/Car/{existing.ID}"))
            await ExpectStatusAsync(delete, HttpStatusCode.Forbidden);

        // The refusals had no effect: still exactly one car, unchanged and not deleted.
        using (var list = await admin.GetAsync(ListUrl(make)))
            Assert.Equal([existing.ID], ListedIds(await ExpectStatusAsync(list, HttpStatusCode.OK)));

        using (var view = await admin.GetAsync($"api/Car/{existing.ID}"))
        {
            var stored = ResponseEntity<CarDTO>(await ExpectStatusAsync(view, HttpStatusCode.OK));
            Assert.Equal("Civic", stored.Model);
            Assert.Equal(2018, stored.Year);
            Assert.False(stored.IsDeleted);
        }
    }

    [Fact]
    public async Task UserWithoutCarAccess_IsRefusedEvenForReading()
    {
        using var admin = ClientWithCarAccess(Access.Read, Access.Write, Access.Delete);
        using var noAccess = ClientWithCarAccess();
        var make = UniqueMake();

        var existing = await CreateAsync(admin, new CarDTO { Make = make, Model = "Golf", Year = 2019 });

        using (var list = await noAccess.GetAsync(ListUrl(make)))
            await ExpectStatusAsync(list, HttpStatusCode.Forbidden);

        using (var view = await noAccess.GetAsync($"api/Car/{existing.ID}"))
            await ExpectStatusAsync(view, HttpStatusCode.Forbidden);
    }

    // The access tree carries only the Car action with the given accesses (none = no Car access at all).
    private HttpClient ClientWithCarAccess(params Access[] access)
    {
        var carActions = new Dictionary<string, object>();
        if (access.Length > 0)
            carActions[nameof(StockPlusPlusActionTree.Car)] = access;

        var accessTreeJson = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            [nameof(StockPlusPlusActionTree)] = carActions,
        });

        return factory.CreateAuthenticatedClient(accessTreeJson);
    }

    // Each test uses its own Make so tests sharing the database never see each other's cars.
    private static string UniqueMake() => "Make-" + Guid.NewGuid().ToString("N");

    // Without the DataGridExport permission a list request must ask for at most 5 rows ($top).
    private static string ListUrl(string make) => $"api/Car?$top=5&$filter=Make eq '{make}'";

    private static async Task<CarDTO> CreateAsync(HttpClient client, CarDTO car)
    {
        using var response = await client.PostAsJsonAsync("api/Car", car);
        return ResponseEntity<CarDTO>(await ExpectStatusAsync(response, HttpStatusCode.Created));
    }

    private static List<string?> ListedIds(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);
        return Property(document.RootElement, "Value")
            .EnumerateArray()
            .Select(row => Property(row, nameof(CarListDTO.ID)).GetString())
            .ToList();
    }

    private static T ResponseEntity<T>(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);
        return JsonSerializer.Deserialize<T>(
            Property(document.RootElement, "Entity").GetRawText(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }

    private static JsonElement Property(JsonElement element, string name)
        => element.EnumerateObject().First(property => string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)).Value;

    private static async Task<string> ExpectStatusAsync(HttpResponseMessage response, HttpStatusCode expected)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(
            response.StatusCode == expected,
            $"Expected HTTP {(int)expected} ({expected}), got {(int)response.StatusCode} ({response.StatusCode}). Body: {body}");
        return body;
    }
}
