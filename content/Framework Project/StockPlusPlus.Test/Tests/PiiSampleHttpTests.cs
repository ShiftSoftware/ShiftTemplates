using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.ShiftEntity.Web;
using ShiftSoftware.TypeAuth.Core;
using StockPlusPlus.Data.DbContext;
using StockPlusPlus.Data.Entities;
using StockPlusPlus.Shared.ActionTrees;
using StockPlusPlus.Shared.DTOs.SampleContact;

namespace StockPlusPlus.Test.Tests;

[Collection("API Collection")]
public class PiiSampleHttpTests(CustomWebApplicationFactory factory)
{
    [Fact]
    public async Task Controller_and_minimal_routes_protect_read_search_reveal_and_save()
    {
        var contact = new SampleContact
        {
            Label = "PII synthetic contact",
            Name = "Ada Example",
            Phone = "synthetic-phone-0088",
            Email = "ada@example.test",
            Address = "42 Example Street",
            Identifier = "synthetic-identifier"
        };
        string controllerKey;
        string minimalKey;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DB>();
            db.SampleContacts.Add(contact);
            await db.SaveChangesAsync();
            var hashIds = scope.ServiceProvider.GetRequiredService<IHashIdService>();
            controllerKey = hashIds.Encode<SampleContactDTO>(contact.ID);
            minimalKey = hashIds.Encode<SampleContactMinimalDTO>(contact.ID);
        }

        var grant = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            [nameof(GeneralActionTree)] = new Dictionary<string, object>
            {
                [nameof(GeneralActionTree.DataGridExport)] = new[] { Access.Maximum }
            },
            [nameof(PiiActionTree)] = new Dictionary<string, object>
            {
                [nameof(PiiActionTree.Reveal)] = new[] { Access.Maximum }
            },
            [nameof(StockPlusPlusActionTree)] = new Dictionary<string, object>
            {
                [nameof(StockPlusPlusActionTree.SampleContacts)] = new[] { Access.Read, Access.Write }
            }
        });
        using var client = factory.CreateAuthenticatedClient(grant);

        using (var selectionScope = factory.Services.CreateScope())
        {
            var selectionHttp = new DefaultHttpContext { RequestServices = selectionScope.ServiceProvider };
            var selectionHandler = new ShiftEntityCrudHandler<
                ShiftRepository<DB, SampleContact, SampleContactListDTO, SampleContactDTO>,
                SampleContact, SampleContactListDTO, SampleContactDTO>();
            var selected = await selectionHandler.GetSelectedListDTOsAsync(selectionHttp,
                new SelectStateDTO<SampleContactListDTO> { All = true });
            Assert.Single(selected);
            Assert.Null(selected[0].Phone?.Value);
            Assert.Equal("••••", selected[0].Phone?.Display);
        }

        foreach (var endpoint in new[] { "sample-contact", "sample-contact-minimal" })
        {
            using var list = await client.GetAsync($"api/{endpoint}?$top=1");
            var body = await ExpectAsync(list, HttpStatusCode.OK);
            Assert.Contains("synthetic contact", body);
            Assert.DoesNotContain(contact.Phone, body);
            Assert.DoesNotContain(contact.Email, body);
            Assert.DoesNotContain(contact.Identifier, body);

            using var normalSearch = await client.GetAsync(
                $"api/{endpoint}?$top=1&$filter=Label eq 'PII synthetic contact'");
            await ExpectAsync(normalSearch, HttpStatusCode.OK);

            using var protectedSearch = await client.GetAsync(
                $"api/{endpoint}?$top=1&$filter=Phone/Value eq 'synthetic-phone-0088'");
            await ExpectAsync(protectedSearch, HttpStatusCode.BadRequest);
        }

        foreach (var (endpoint, key) in new[]
        {
            ("sample-contact", controllerKey), ("sample-contact-minimal", minimalKey)
        })
        {
            using var detail = await client.GetAsync($"api/{endpoint}/{key}");
            var body = await ExpectAsync(detail, HttpStatusCode.OK);
            Assert.DoesNotContain(contact.Phone, body);
            Assert.DoesNotContain(contact.Email, body);
            Assert.DoesNotContain(contact.Identifier, body);

            using var reveal = await client.PostAsync($"api/{endpoint}/{key}/pii/Phone/reveal", null);
            await ExpectAsync(reveal, HttpStatusCode.OK);
            Assert.Equal("synthetic-phone-0088", (await reveal.Content.ReadFromJsonAsync<PiiRevealDTO>())?.Value);
            Assert.True(reveal.Headers.CacheControl?.NoStore);

            using var denied = await client.PostAsync($"api/{endpoint}/{key}/pii/Identifier/reveal", null);
            await ExpectAsync(denied, HttpStatusCode.NotFound);
        }

        using var loaded = await client.GetAsync($"api/sample-contact/{controllerKey}");
        await ExpectAsync(loaded, HttpStatusCode.OK);
        var edit = (await loaded.Content.ReadFromJsonAsync<ShiftEntityResponse<SampleContactDTO>>())!.Entity!;
        edit.Label = "PII synthetic edited";
        edit.Phone = new PiiFieldDTO { Display = "malicious display", Value = "injected", Write = "keep" };
        using var kept = await client.PutAsJsonAsync($"api/sample-contact/{controllerKey}", edit);
        var keptBody = await ExpectAsync(kept, HttpStatusCode.OK);
        Assert.DoesNotContain(contact.Phone, keptBody);
        Assert.DoesNotContain("injected", keptBody);

        edit = (await kept.Content.ReadFromJsonAsync<ShiftEntityResponse<SampleContactDTO>>())!.Entity!;
        edit.Phone = new PiiFieldDTO { Value = "synthetic-phone-0099", Write = "replace" };
        using var replaced = await client.PutAsJsonAsync($"api/sample-contact/{controllerKey}", edit);
        var replacedBody = await ExpectAsync(replaced, HttpStatusCode.OK);
        Assert.DoesNotContain("synthetic-phone-0099", replacedBody);

        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<DB>();
        Assert.Equal("synthetic-phone-0099", (await verifyDb.SampleContacts.FindAsync(contact.ID))!.Phone);

        using var minimalLoaded = await client.GetAsync($"api/sample-contact-minimal/{minimalKey}");
        await ExpectAsync(minimalLoaded, HttpStatusCode.OK);
        var minimalEdit = (await minimalLoaded.Content.ReadFromJsonAsync<ShiftEntityResponse<SampleContactMinimalDTO>>())!.Entity!;
        minimalEdit.Label = "PII synthetic minimal edited";
        minimalEdit.Phone = new PiiFieldDTO { Value = "ignored", Write = "keep" };
        using var minimalKept = await client.PutAsJsonAsync($"api/sample-contact-minimal/{minimalKey}", minimalEdit);
        await ExpectAsync(minimalKept, HttpStatusCode.OK);
        verifyDb.ChangeTracker.Clear();
        Assert.Equal("synthetic-phone-0099", (await verifyDb.SampleContacts.FindAsync(contact.ID))!.Phone);

        minimalEdit = (await minimalKept.Content.ReadFromJsonAsync<ShiftEntityResponse<SampleContactMinimalDTO>>())!.Entity!;
        minimalEdit.Phone = new PiiFieldDTO { Value = "synthetic-phone-0100", Write = "replace" };
        using var minimalReplaced = await client.PutAsJsonAsync($"api/sample-contact-minimal/{minimalKey}", minimalEdit);
        var minimalReplacedBody = await ExpectAsync(minimalReplaced, HttpStatusCode.OK);
        Assert.DoesNotContain("synthetic-phone-0100", minimalReplacedBody);
        verifyDb.ChangeTracker.Clear();
        Assert.Equal("synthetic-phone-0100", (await verifyDb.SampleContacts.FindAsync(contact.ID))!.Phone);

        edit.Phone = new PiiFieldDTO { Value = "stale-attempt", Write = "replace" };
        using var staleSave = await client.PutAsJsonAsync($"api/sample-contact/{controllerKey}", edit);
        await ExpectAsync(staleSave, HttpStatusCode.Conflict);
        verifyDb.ChangeTracker.Clear();
        Assert.Equal("synthetic-phone-0100", (await verifyDb.SampleContacts.FindAsync(contact.ID))!.Phone);

        var readWriteOnly = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            [nameof(GeneralActionTree)] = new Dictionary<string, object>
            {
                [nameof(GeneralActionTree.DataGridExport)] = new[] { Access.Maximum }
            },
            [nameof(StockPlusPlusActionTree)] = new Dictionary<string, object>
            {
                [nameof(StockPlusPlusActionTree.SampleContacts)] = new[] { Access.Read, Access.Write }
            }
        });
        using var ordinaryClient = factory.CreateAuthenticatedClient(readWriteOnly);
        foreach (var (endpoint, key) in new[]
        {
            ("sample-contact", controllerKey), ("sample-contact-minimal", minimalKey)
        })
        {
            using var ordinaryDetail = await ordinaryClient.GetAsync($"api/{endpoint}/{key}");
            var ordinaryBody = await ExpectAsync(ordinaryDetail, HttpStatusCode.OK);
            Assert.DoesNotContain("synthetic-phone-0100", ordinaryBody);

            using var deniedReveal = await ordinaryClient.PostAsync($"api/{endpoint}/{key}/pii/Phone/reveal", null);
            await ExpectAsync(deniedReveal, HttpStatusCode.Forbidden);

            var deniedEdit = (await ordinaryDetail.Content.ReadFromJsonAsync<ShiftEntityResponse<SampleContactDTO>>())!.Entity!;
            deniedEdit.Phone = new PiiFieldDTO { Value = "unauthorized", Write = "replace" };
            using var deniedSave = await ordinaryClient.PutAsJsonAsync($"api/{endpoint}/{key}", deniedEdit);
            await ExpectAsync(deniedSave, HttpStatusCode.Forbidden);
        }
    }

    private static async Task<string> ExpectAsync(HttpResponseMessage response, HttpStatusCode status)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == status,
            $"Expected {(int)status}, got {(int)response.StatusCode}. Body: {body}");
        return body;
    }
}
