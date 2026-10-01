using System.Text.Json;
using Bunit;
using RichardSzalay.MockHttp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ShiftSoftware.ShiftBlazor.Components;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.Pii;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.TypeAuth.Core;
using StockPlusPlus.Shared.ActionTrees;
using StockPlusPlus.Shared.DTOs.SampleContact;
using StockPlusPlus.Web.Pages.SampleContact;
using Xunit;

namespace StockPlusPlus.Web.Tests;

public class PiiNestedContactFormTests : ShiftBlazorTestContext
{
    private MockHttpMessageHandler Grant()
    {
        var mock = Services.AddMockHttpClient();
        Services.AddShiftEntityPii();
        Services.RemoveAll<ITypeAuthService>();
        var grants = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            [nameof(PiiActionTree)] = new Dictionary<string, object>
            { [nameof(PiiActionTree.Reveal)] = new[] { Access.Maximum } },
            [nameof(StockPlusPlusActionTree)] = new Dictionary<string, object>
            { [nameof(StockPlusPlusActionTree.SampleContacts)] = new[] { Access.Read, Access.Write } }
        });
        Services.AddSingleton<ITypeAuthService>(new TypeAuthContextBuilder().AddAccessTree(grants)
            .AddActionTree<PiiActionTree>().AddActionTree<StockPlusPlusActionTree>().Build());
        return mock;
    }

    [Fact]
    public async Task Sample_phone_list_shows_required_errors_and_allows_creating_a_phone()
    {
        Grant();
        var cut = Render<SampleContactForm>();
        cut.FindComponents<ShiftPiiField>()[1].Find("input").Input("primary");
        var list = cut.FindComponent<MultiItemField<SampleContactPhoneDTO>>();
        await cut.InvokeAsync(() => list.Instance.CreateNew());
        var form = cut.FindComponent<ShiftEntityForm<SampleContactDTO>>();
        var phone = cut.FindComponents<ShiftPiiField>().Single(x => x.Instance.Label == "Additional phone");
        // Like the other fields of a phone row, the phone is checked when it changes. The server checks the save.
        phone.Find("input").Input("x");
        phone.Find("input").Input("");
        Assert.Contains("Additional phone is required.", phone.Markup);
        phone.Find("input").Input("additional");
        Assert.DoesNotContain("Additional phone is required.", phone.Markup);
        Assert.True(await cut.InvokeAsync(() => form.Instance.Validate()));
    }

    [Fact]
    public async Task Sample_existing_phone_reveals_by_child_id_and_displays_nested_validation()
    {
        var mock = Grant();
        mock.When(HttpMethod.Get, "http://localhost/sample-contact/7").RespondJson(new ShiftEntityResponse<SampleContactDTO>(new()
        {
            ID = "7", Label = "Synthetic", Phone = new() { Display = "•••• 1111", Write = "keep" },
            Phones = [new() { ID = "22", Number = new() { Display = "•••• 2222", Write = "keep" } }]
        }));
        mock.When(HttpMethod.Post, "http://localhost/sample-contact/7/pii/" + Uri.EscapeDataString("Phones[22].Number") + "/reveal")
            .RespondJson(new PiiRevealDTO { Value = "stored-phone" });
        var cut = Render<SampleContactForm>(p => p.Add(x => x.Key, "7"));
        cut.WaitForAssertion(() => Assert.Contains("Additional phone", cut.Markup));
        var form = cut.FindComponent<ShiftEntityForm<SampleContactDTO>>();
        Assert.True(await cut.InvokeAsync(() => form.Instance.Validate()));
        await cut.InvokeAsync(() => form.Instance.EditItem());
        var phone = cut.FindComponents<ShiftPiiField>().Single(x => x.Instance.Label == "Additional phone");
        phone.Find("button").Click();
        phone.WaitForAssertion(() => Assert.Equal("stored-phone", phone.Find("input").GetAttribute("value")));
        phone.Find("input").Input("");
        Assert.Contains("Additional phone is required.", phone.Markup);
        phone.Find("input").Input("corrected");
        Assert.DoesNotContain("Additional phone is required.", phone.Markup);
        Assert.True(await cut.InvokeAsync(() => form.Instance.Validate()));
    }
}
