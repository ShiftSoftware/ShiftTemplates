using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
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
            Phone = "+964 750 000 0088",
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
            Assert.Equal("•••• 0088", selected[0].Phone?.Display);
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
                $"api/{endpoint}?$top=1&$filter=Phone/Value eq '07500000088'");
            var protectedBody = await ExpectAsync(protectedSearch, HttpStatusCode.OK);
            Assert.DoesNotContain(contact.Phone, protectedBody);
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
            Assert.Equal("+964 750 000 0088", (await reveal.Content.ReadFromJsonAsync<PiiRevealDTO>())?.Value);
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
        edit.Phone = new PiiFieldDTO { Value = "+964 750 000 0099", Write = "replace" };
        using var replaced = await client.PutAsJsonAsync($"api/sample-contact/{controllerKey}", edit);
        var replacedBody = await ExpectAsync(replaced, HttpStatusCode.OK);
        Assert.DoesNotContain("+964 750 000 0099", replacedBody);

        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<DB>();
        Assert.Equal("+964 750 000 0099", (await verifyDb.SampleContacts.FindAsync(contact.ID))!.Phone);

        using var minimalLoaded = await client.GetAsync($"api/sample-contact-minimal/{minimalKey}");
        await ExpectAsync(minimalLoaded, HttpStatusCode.OK);
        var minimalEdit = (await minimalLoaded.Content.ReadFromJsonAsync<ShiftEntityResponse<SampleContactMinimalDTO>>())!.Entity!;
        minimalEdit.Label = "PII synthetic minimal edited";
        minimalEdit.Phone = new PiiFieldDTO { Value = "ignored", Write = "keep" };
        using var minimalKept = await client.PutAsJsonAsync($"api/sample-contact-minimal/{minimalKey}", minimalEdit);
        await ExpectAsync(minimalKept, HttpStatusCode.OK);
        verifyDb.ChangeTracker.Clear();
        Assert.Equal("+964 750 000 0099", (await verifyDb.SampleContacts.FindAsync(contact.ID))!.Phone);

        minimalEdit = (await minimalKept.Content.ReadFromJsonAsync<ShiftEntityResponse<SampleContactMinimalDTO>>())!.Entity!;
        minimalEdit.Phone = new PiiFieldDTO { Value = "+964 750 000 0100", Write = "replace" };
        using var minimalReplaced = await client.PutAsJsonAsync($"api/sample-contact-minimal/{minimalKey}", minimalEdit);
        var minimalReplacedBody = await ExpectAsync(minimalReplaced, HttpStatusCode.OK);
        Assert.DoesNotContain("+964 750 000 0100", minimalReplacedBody);
        verifyDb.ChangeTracker.Clear();
        Assert.Equal("+964 750 000 0100", (await verifyDb.SampleContacts.FindAsync(contact.ID))!.Phone);

        edit.Phone = new PiiFieldDTO { Value = "+964 750 000 0101", Write = "replace" };
        using var staleSave = await client.PutAsJsonAsync($"api/sample-contact/{controllerKey}", edit);
        await ExpectAsync(staleSave, HttpStatusCode.Conflict);
        verifyDb.ChangeTracker.Clear();
        Assert.Equal("+964 750 000 0100", (await verifyDb.SampleContacts.FindAsync(contact.ID))!.Phone);

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
            Assert.DoesNotContain("+964 750 000 0100", ordinaryBody);

            using var deniedReveal = await ordinaryClient.PostAsync($"api/{endpoint}/{key}/pii/Phone/reveal", null);
            await ExpectAsync(deniedReveal, HttpStatusCode.Forbidden);

            var deniedEdit = (await ordinaryDetail.Content.ReadFromJsonAsync<ShiftEntityResponse<SampleContactDTO>>())!.Entity!;
            deniedEdit.Phone = new PiiFieldDTO { Value = "+964 750 000 0102", Write = "replace" };
            using var deniedSave = await ordinaryClient.PutAsJsonAsync($"api/{endpoint}/{key}", deniedEdit);
            await ExpectAsync(deniedSave, HttpStatusCode.Forbidden);
        }
    }

    [Fact]
    public async Task Pii_annotations_validate_create_clear_and_retry_on_both_generated_routes()
    {
        var grant = JsonSerializer.Serialize(new Dictionary<string, object>
        {
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
        var validationLabel = "PII validation " + Guid.NewGuid().ToString("N");
        try
        {
            foreach (var endpoint in new[] { "sample-contact", "sample-contact-minimal" })
            {
                using var missing = await client.PostAsJsonAsync($"api/{endpoint}", new SampleContactDTO { Label = validationLabel });
                await ExpectAsync(missing, HttpStatusCode.BadRequest);
                await AssertPhoneError(missing);

                using var created = await client.PostAsJsonAsync($"api/{endpoint}", new SampleContactDTO
                {
                    Label = validationLabel, Phone = new() { Value = "+964 750 000 0111", Write = "replace" }
                });
                await ExpectAsync(created, HttpStatusCode.Created);
                var dto = (await created.Content.ReadFromJsonAsync<ShiftEntityResponse<SampleContactDTO>>())!.Entity!;
                var key = dto.ID;

                foreach (var value in new string?[] { null, " ", new string('x', 41) })
                {
                    dto.Phone = new() { Value = value, Write = "replace" };
                    using var invalid = await client.PutAsJsonAsync($"api/{endpoint}/{key}", dto);
                    await ExpectAsync(invalid, HttpStatusCode.BadRequest);
                    await AssertPhoneError(invalid);
                }

                dto.Phone = new() { Value = "+964 750 000 0222", Write = "replace" };
                using var retry = await client.PutAsJsonAsync($"api/{endpoint}/{key}", dto);
                var body = await ExpectAsync(retry, HttpStatusCode.OK);
                Assert.DoesNotContain("+964 750 000 0222", body);
                dto = (await retry.Content.ReadFromJsonAsync<ShiftEntityResponse<SampleContactDTO>>())!.Entity!;
                Assert.Null(dto.Phone!.Value);

                dto.Label = validationLabel + " edited";
                using var kept = await client.PutAsJsonAsync($"api/{endpoint}/{key}", dto);
                await ExpectAsync(kept, HttpStatusCode.OK);
            }
        }
        finally
        {
            using var cleanupScope = factory.Services.CreateScope();
            var db = cleanupScope.ServiceProvider.GetRequiredService<DB>();
            await db.SampleContacts.Where(x => x.Label == validationLabel || x.Label == validationLabel + " edited")
                .ExecuteDeleteAsync();
        }
    }

    private static async Task AssertPhoneError(HttpResponseMessage response)
    {
        var body = (await response.Content.ReadFromJsonAsync<ShiftEntityResponse<SampleContactDTO>>())!;
        Assert.Contains(body.Message!.SubMessages!, x => x.For == "Phone" && x.SubMessages!.Count > 0);
    }

    private static async Task<string> ExpectAsync(HttpResponseMessage response, HttpStatusCode status)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == status,
            $"Expected {(int)status}, got {(int)response.StatusCode}. Body: {body}");
        return body;
    }
}
