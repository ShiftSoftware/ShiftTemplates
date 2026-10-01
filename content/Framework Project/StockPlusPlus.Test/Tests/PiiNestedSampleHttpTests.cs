using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.TypeAuth.Core;
using StockPlusPlus.Data.DbContext;
using StockPlusPlus.Shared.ActionTrees;
using StockPlusPlus.Shared.DTOs.SampleContact;

namespace StockPlusPlus.Test.Tests;

[Collection("API Collection")]
public class PiiNestedSampleHttpTests(CustomWebApplicationFactory factory)
{
    [Theory]
    [InlineData("sample-contact")]
    [InlineData("sample-contact-minimal")]
    public async Task Protected_phone_lists_create_reorder_reveal_replace_add_remove_and_retry(string endpoint)
    {
        var label = "PII nested " + Guid.NewGuid().ToString("N");
        using var client = factory.CreateAuthenticatedClient(Grants(true));
        using var ordinary = factory.CreateAuthenticatedClient(Grants(false));
        try
        {
            using var created = await client.PostAsJsonAsync("api/" + endpoint, new SampleContactDTO
            {
                Label = label, Phone = new() { Value = "synthetic-primary", Write = "replace" },
                Phones = [new() { Label = "First", Number = new() { Value = "synthetic-first-1111", Write = "replace" } },
                          new() { Label = "Second", Number = new() { Value = "synthetic-second-2222", Write = "replace" } }]
            });
            var dto = await Expect(created, HttpStatusCode.Created);
            Assert.All(dto.Phones, x => Assert.Null(x.Number!.Value));
            var key = dto.ID;
            dto.Phones.Reverse();
            dto.Phones[0].Number!.Value = "tampered";
            using var kept = await ordinary.PutAsJsonAsync($"api/{endpoint}/{key}", dto);
            dto = await Expect(kept, HttpStatusCode.OK);
            await AssertStorage(label, ("First", "synthetic-first-1111"), ("Second", "synthetic-second-2222"));

            using var reveal = await client.PostAsync($"api/{endpoint}/{key}/pii/Phones[{dto.Phones.Single(x => x.Label == "Second").ID}].Number/reveal", null);
            Assert.Equal(HttpStatusCode.OK, reveal.StatusCode);
            Assert.Equal("synthetic-second-2222", (await reveal.Content.ReadFromJsonAsync<PiiRevealDTO>())!.Value);
            Assert.True(reveal.Headers.CacheControl!.NoStore);
            using var deniedReveal = await ordinary.PostAsync($"api/{endpoint}/{key}/pii/Phones[{dto.Phones[0].ID}].Number/reveal", null);
            Assert.Equal(HttpStatusCode.Forbidden, deniedReveal.StatusCode);

            dto.Phones[0].Number = new() { Value = "synthetic-edited-3333", Write = "replace" };
            using var deniedSave = await ordinary.PutAsJsonAsync($"api/{endpoint}/{key}", dto);
            Assert.Equal(HttpStatusCode.Forbidden, deniedSave.StatusCode);
            dto.Phones[0].Number = new() { Value = null, Write = "replace" };
            using var invalid = await client.PutAsJsonAsync($"api/{endpoint}/{key}", dto);
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
            var error = (await invalid.Content.ReadFromJsonAsync<ShiftEntityResponse<SampleContactDTO>>())!.Message!;
            Assert.Contains(error.SubMessages!, x => x.For == "Phones[0].Number");
            await AssertStorage(label, ("First", "synthetic-first-1111"), ("Second", "synthetic-second-2222"));

            dto.Phones[0].Number = new() { Value = "synthetic-edited-3333", Write = "replace" };
            var retainedLabel = dto.Phones[0].Label;
            dto.Phones.RemoveAt(1);
            dto.Phones.Add(new() { Label = "Added", Number = new() { Value = "synthetic-added-4444", Write = "replace" } });
            using var saved = await client.PutAsJsonAsync($"api/{endpoint}/{key}", dto);
            dto = await Expect(saved, HttpStatusCode.OK);
            await AssertStorage(label, (retainedLabel, "synthetic-edited-3333"), ("Added", "synthetic-added-4444"));
            dto.Phones[0].ID = "999999";
            using var foreign = await client.PutAsJsonAsync($"api/{endpoint}/{key}", dto);
            Assert.Equal(HttpStatusCode.BadRequest, foreign.StatusCode);
        }
        finally
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DB>();
            await db.SampleContactPhones.Where(x => x.SampleContact.Label == label).ExecuteDeleteAsync();
            await db.SampleContacts.Where(x => x.Label == label).ExecuteDeleteAsync();
        }
    }

    private async Task AssertStorage(string label, params (string Label, string Value)[] expected)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DB>();
        var phones = await db.SampleContactPhones.Where(x => x.SampleContact.Label == label).ToListAsync();
        Assert.Equal(expected.Length, phones.Count);
        foreach (var item in expected) Assert.Equal(item.Value, phones.Single(x => x.Label == item.Label).Number);
    }

    private static async Task<SampleContactDTO> Expect(HttpResponseMessage response, HttpStatusCode expected)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == expected, body);
        Assert.DoesNotContain("synthetic-", body);
        Assert.DoesNotContain("tampered", body);
        return JsonSerializer.Deserialize<ShiftEntityResponse<SampleContactDTO>>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web))!.Entity!;
    }

    private static string Grants(bool pii)
    {
        var grants = new Dictionary<string, object>
        {
            [nameof(StockPlusPlusActionTree)] = new Dictionary<string, object>
            { [nameof(StockPlusPlusActionTree.SampleContacts)] = new[] { Access.Read, Access.Write } }
        };
        if (pii) grants[nameof(PiiActionTree)] = new Dictionary<string, object>
            { [nameof(PiiActionTree.Reveal)] = new[] { Access.Maximum } };
        return JsonSerializer.Serialize(grants);
    }
}
