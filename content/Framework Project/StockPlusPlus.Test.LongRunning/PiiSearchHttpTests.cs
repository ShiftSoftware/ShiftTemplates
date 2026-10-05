using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.SqlClient;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.TypeAuth.Core;
using StockPlusPlus.Data.DbContext;
using StockPlusPlus.Data.Entities;
using StockPlusPlus.Shared.ActionTrees;
using StockPlusPlus.Shared.DTOs.SampleContact;
using Xunit;

namespace StockPlusPlus.Test.LongRunning;

/// <summary>Run explicitly with a disposable SQLServer_Test connection override.</summary>
public class PiiSearchHttpTests(DisposablePiiFactory factory) : IClassFixture<DisposablePiiFactory>
{
    [Theory]
    [InlineData("sample-contact")]
    [InlineData("sample-contact-minimal")]
    public async Task Exact_and_partial_search_share_masked_counts_pages_and_export(string endpoint)
    {
        var label = "Synthetic search " + Guid.NewGuid().ToString("N");
        using var writer = factory.CreateAuthenticatedClient(Grants(reveal: true, search: true));
        using var reader = factory.CreateAuthenticatedClient(Grants(reveal: false, search: false));
        using var noRead = factory.CreateAuthenticatedClient(Grants(reveal: true, search: true, read: false));
        try
        {
            foreach (var phone in new[] { "07500000088", "07500000099" })
            {
                using var response = await writer.PostAsJsonAsync("api/" + endpoint, new SampleContactDTO
                {
                    Label = label, Phone = new() { Value = phone, Write = "replace" },
                    Name = new() { Value = "Ada Synthetic", Write = "replace" },
                    Phones = [new() { Label = "Foreign", Number = new() { Value = "+1 (202) 555-0123", Write = "replace" } }]
                });
                Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            }
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<DB>();
                var stored = await db.SampleContacts.Where(x => x.Label == label).Include(x => x.Phones).ToListAsync();
                Assert.Equal(new[] { "+964 750 000 0088", "+964 750 000 0099" }, stored.Select(x => x.Phone).Order());
                Assert.All(stored, r => Assert.Equal("+1 202-555-0123", Assert.Single(r.Phones).Number));
            }

            foreach (var filter in new[]
            {
                "Phone/Value eq '07500000088'", "contains(Phone/Display,'0750-000-0088')",
                "startswith(Phone/Display,'+964 750 000 0088')", "endswith(Phone/Value,'07500000088')"
            })
                await Check(reader, filter, 1);
            await Check(reader, "contains(Name/Display,'Ada')", 0);
            await Check(reader, "contains(Name/Display,'Ada Synthetic')", 2, top: 1);
            await Check(reader, "Phone/Value eq '07500000100'", 0);
            await Check(writer, "contains(Phone/Display,'0088')", 1);
            await Check(writer, "contains(Phone/Display,'750')", 2, top: 1);
            await Check(writer, "contains(Name/Display,'Ada')", 2);
            await Check(reader, "Name/Display eq 'Ada Synthetic'", 2, top: null); // ordinary export query

            foreach (var filter in new[] { "not contains(Name/Value,'Ada')", "Name/Value ne 'Ada'", "substring(Name/Value,1) eq 'da'", "Phone/Value eq null", "contains(Phone/Value,'0088')" })
            {
                using var response = await reader.GetAsync(Url(filter));
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            }
            foreach (var client in new[] { reader, writer })
            {
                using var ordered = await client.GetAsync(Url("Name/Value eq 'Ada Synthetic'") + "&$orderby=Phone/Value");
                Assert.Equal(HttpStatusCode.BadRequest, ordered.StatusCode);
            }
            using var denied = await noRead.GetAsync(Url("contains(Phone/Display,'750')"));
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        }
        finally
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DB>();
            await db.SampleContactPhones.Where(x => x.SampleContact.Label == label).ExecuteDeleteAsync();
            await db.SampleContacts.Where(x => x.Label == label).ExecuteDeleteAsync();
        }

        string Url(string filter, int? top = 2) => $"api/{endpoint}?$count=true&$filter=" +
            Uri.EscapeDataString($"Label eq '{label}' and ({filter})") + (top is null ? "" : "&$top=" + top);

        async Task Check(HttpClient client, string filter, int count, int? top = 2)
        {
            using var response = await client.GetAsync(Url(filter, top));
            var text = await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode == HttpStatusCode.OK, text);
            Assert.DoesNotContain("+964", text);
            Assert.DoesNotContain("Ada Synthetic", text);
            var result = JsonSerializer.Deserialize<ODataDTO<SampleContactListDTO>>(text, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
            Assert.Equal(count, result.Count);
            Assert.Equal(Math.Min(count, top ?? count), result.Value.Count);
        }
    }

    [Theory]
    [InlineData("sample-contact", false, false)]
    [InlineData("sample-contact", true, false)]
    [InlineData("sample-contact", false, true)]
    [InlineData("sample-contact", true, true)]
    [InlineData("sample-contact-minimal", false, false)]
    [InlineData("sample-contact-minimal", true, false)]
    [InlineData("sample-contact-minimal", false, true)]
    [InlineData("sample-contact-minimal", true, true)]
    public async Task Search_and_reveal_grants_are_independent_for_queries_reveal_and_writes(string endpoint, bool reveal, bool search)
    {
        var label = "Synthetic permissions " + Guid.NewGuid().ToString("N");
        using var setup = factory.CreateAuthenticatedClient(Grants(reveal: true, search: false));
        using var client = factory.CreateAuthenticatedClient(Grants(reveal, search));
        try
        {
            using var created = await setup.PostAsJsonAsync("api/" + endpoint, new SampleContactDTO
            {
                Label = label,
                Name = new() { Value = "Ada Synthetic", Write = "replace" },
                Phone = new() { Value = "07500000088", Write = "replace" },
                Email = new() { Value = "ada@example.test", Write = "replace" }
            });
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            var contact = (await created.Content.ReadFromJsonAsync<ShiftEntityResponse<SampleContactDTO>>())!.Entity!;
            var url = $"api/{endpoint}/{contact.ID}";

            foreach (var (filter, count) in new[]
            {
                ("Phone/Display eq '07500000088'", 1),
                ("contains(Name/Display,'Ada')", search ? 1 : 0)
            })
            {
                using var response = await client.GetAsync($"api/{endpoint}?$top=1&$filter=" +
                    Uri.EscapeDataString($"Label eq '{label}' and ({filter})"));
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                var text = await response.Content.ReadAsStringAsync();
                Assert.DoesNotContain("+964", text);
                Assert.DoesNotContain("Ada Synthetic", text);
                var result = JsonSerializer.Deserialize<ODataDTO<SampleContactListDTO>>(text, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
                Assert.Equal(count, result.Count);
                Assert.Equal(count, result.Value.Count);
            }
            using var phoneFragment = await client.GetAsync($"api/{endpoint}?$top=1&$filter=" +
                Uri.EscapeDataString($"Label eq '{label}' and contains(Phone/Display,'0088')"));
            Assert.Equal(search ? HttpStatusCode.OK : HttpStatusCode.BadRequest, phoneFragment.StatusCode);
            using var revealed = await client.PostAsync(url + "/pii/Phone/reveal", null);
            Assert.Equal(reveal ? HttpStatusCode.OK : HttpStatusCode.Forbidden, revealed.StatusCode);
            if (reveal)
            {
                Assert.Equal("+964 750 000 0088", (await revealed.Content.ReadFromJsonAsync<PiiRevealDTO>())!.Value);
                Assert.True(revealed.Headers.CacheControl?.NoStore);
            }

            // Keep remains available without either grant; replacement and clear need Reveal only.
            contact.Label = label;
            using var kept = await client.PutAsJsonAsync(url, contact);
            Assert.Equal(HttpStatusCode.OK, kept.StatusCode);
            contact = (await kept.Content.ReadFromJsonAsync<ShiftEntityResponse<SampleContactDTO>>())!.Entity!;
            contact.Phone = new() { Value = "07500000099", Write = "replace" };
            using var replaced = await client.PutAsJsonAsync(url, contact);
            Assert.Equal(reveal ? HttpStatusCode.OK : HttpStatusCode.Forbidden, replaced.StatusCode);
            Assert.DoesNotContain("+964", await replaced.Content.ReadAsStringAsync());

            using var loaded = await client.GetAsync(url);
            contact = (await loaded.Content.ReadFromJsonAsync<ShiftEntityResponse<SampleContactDTO>>())!.Entity!;
            contact.Email = new() { Value = null, Write = "replace" };
            using var cleared = await client.PutAsJsonAsync(url, contact);
            Assert.Equal(reveal ? HttpStatusCode.OK : HttpStatusCode.Forbidden, cleared.StatusCode);

            using var scope = factory.Services.CreateScope();
            var stored = await scope.ServiceProvider.GetRequiredService<DB>().SampleContacts.SingleAsync(x => x.Label == label);
            Assert.Equal(reveal ? "+964 750 000 0099" : "+964 750 000 0088", stored.Phone);
            Assert.Equal(reveal ? null : "ada@example.test", stored.Email);
        }
        finally
        {
            using var scope = factory.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<DB>().SampleContacts.Where(x => x.Label == label).ExecuteDeleteAsync();
        }
    }

    private static string Grants(bool reveal, bool search, bool read = true)
    {
        var grants = new Dictionary<string, object>
        {
            [nameof(GeneralActionTree)] = new Dictionary<string, object> { [nameof(GeneralActionTree.DataGridExport)] = new[] { Access.Maximum } },
            [nameof(StockPlusPlusActionTree)] = new Dictionary<string, object>
            { [nameof(StockPlusPlusActionTree.SampleContacts)] = read ? new[] { Access.Read, Access.Write } : new[] { Access.Write } }
        };
        grants[nameof(PiiActionTree)] = new Dictionary<string, Access[]>
        {
            [nameof(PiiActionTree.Reveal)] = reveal ? [Access.Maximum] : [],
            [nameof(PiiActionTree.PartialSearch)] = search ? [Access.Maximum] : []
        };
        return JsonSerializer.Serialize(grants);
    }
}

public sealed class DisposablePiiFactory : CustomWebApplicationFactory
{
    public DisposablePiiFactory()
    {
        var connection = Environment.GetEnvironmentVariable("SHIFT_TEST_ConnectionStrings__SQLServer_Test");
        if (string.IsNullOrWhiteSpace(connection) || !new SqlConnectionStringBuilder(connection).InitialCatalog.StartsWith("PiiSearch_", StringComparison.Ordinal))
            throw new InvalidOperationException("Set SHIFT_TEST_ConnectionStrings__SQLServer_Test to a disposable database named PiiSearch_*. This test deletes and recreates that database.");
    }
}
