using System.Security.Claims;
using System.Text.Json;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;
using ShiftSoftware.ShiftBlazor.Components;
using ShiftSoftware.ShiftBlazor.Extensions;
using ShiftSoftware.TypeAuth.Blazor.Extensions;
using ShiftSoftware.TypeAuth.Core;
using StockPlusPlus.Shared.ActionTrees;
using StockPlusPlus.Shared.DTOs;
using StockPlusPlus.Web.Pages.Samples.SideBySideForms;
using Xunit;

namespace StockPlusPlus.Web.Tests;

/// <summary>
/// Samples/SideBySideForms: the same form body shown twice, each pane loaded with a different country.
/// The legacy page (ShiftForm&lt;,&gt; + TheItem) ends with both panes holding the SAME record, so a pane can save
/// another pane's data under its own key; the fixed page (form context) keeps each pane on its own record.
/// </summary>
public class SideBySideFormsTests : BunitContext
{
    private readonly MockHttpMessageHandler api;

    public SideBySideFormsTests()
    {
        api = Services.AddMockHttpClient();

        Services.AddShiftBlazor(config =>
        {
            config.ShiftConfiguration = options => options.BaseAddress = "http://localhost";
        });

        Services.AddTransient(x => new ShiftSoftware.ShiftIdentity.Core.Localization.ShiftIdentityLocalizer(x, typeof(ShiftSoftwareLocalization.Identity.Resource)));

        JSInterop.Mode = JSRuntimeMode.Loose;

        // The panes load only with Read access to Country, which TypeAuth takes from the user's access-tree claim.
        Services.AddTypeAuth(o => o.AddActionTree<StockPlusPlusActionTree>());
        var accessTree = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            [nameof(StockPlusPlusActionTree)] = new Dictionary<string, object>
            {
                [nameof(StockPlusPlusActionTree.Country)] = new[] { Access.Read, Access.Write, Access.Delete },
            },
        });
        this.AddAuthorization()
            .SetAuthorized("tester")
            .SetClaims(new Claim(TypeAuthClaimTypes.AccessTree, accessTree));

        RenderTree.Add<IncludeMudProviders>();

        Country("A", "Iraq");
        Country("B", "Turkey");
    }

    [Fact]
    public void LegacyPage_EndsWithBothPanesShowingTheSameCountry()
    {
        var cut = Render<LegacySideBySideCountries>(p => p.Add(x => x.Left, "A").Add(x => x.Right, "B"));

        var (left, right) = Panes(cut);
        cut.WaitForAssertion(() => Assert.True(left.Instance.Value.ID is not null && right.Instance.Value.ID is not null));

        // Both panes were asked for different records...
        Assert.Equal("A", left.Instance.Key);
        Assert.Equal("B", right.Instance.Key);

        // ...but they share the page's TheItem, so the last load wins everywhere: same record, same text box value.
        Assert.Same(left.Instance.Value, right.Instance.Value);
        Assert.Equal(NameShownIn(left), NameShownIn(right));

        // So one pane now holds the OTHER record under its own key. Its Save would PUT Country/{its key} with that body.
        var wrongPane = left.Instance.Value.ID == "A" ? right : left;
        Assert.NotEqual(wrongPane.Instance.Key, wrongPane.Instance.Value.ID);
    }

    [Fact]
    public void LegacyPage_SavingOnePaneWritesTheOtherPanesRecordUnderItsKey()
    {
        string? putUrl = null, putBody = null;
        api.When(HttpMethod.Put, "http://localhost/Country/*").Respond(request =>
        {
            putUrl = request.RequestUri!.ToString();
            putBody = request.Content!.ReadAsStringAsync().Result;
            return Json(new { Entity = new { ID = "A", Name = "Iraq" } });
        });

        var cut = Render<LegacySideBySideCountries>(p => p.Add(x => x.Left, "A").Add(x => x.Right, "B"));
        var (left, right) = Panes(cut);
        cut.WaitForAssertion(() => Assert.True(left.Instance.Value.ID is not null && right.Instance.Value.ID is not null));

        var wrongPane = left.Instance.Value.ID == "A" ? right : left;
        var otherRecord = wrongPane.Instance.Value;

        // Save the pane exactly as the user would: Edit, then Save, without touching any field.
        wrongPane.Find("button[title*='Edit' i], button[aria-label*='Edit' i]").Click();
        cut.WaitForAssertion(() => Assert.Equal(ShiftSoftware.ShiftBlazor.Enums.FormModes.Edit, wrongPane.Instance.Mode));

        // Mode is shared too (@bind-Mode to the page's Mode): pressing Edit in one pane put BOTH panes in edit mode.
        var otherPane = wrongPane == left ? right : left;
        cut.WaitForAssertion(() => Assert.Equal(ShiftSoftware.ShiftBlazor.Enums.FormModes.Edit, otherPane.Instance.Mode));

        wrongPane.Find("form").Submit();
        cut.WaitForAssertion(() => Assert.NotNull(putUrl));

        // The request targets the pane's own key but carries the other pane's record: saving it would overwrite
        // one country with the other's data.
        Assert.Equal($"http://localhost/Country/{wrongPane.Instance.Key}", putUrl);
        var sent = Body(putBody);
        Assert.Equal(otherRecord.ID, sent.ID);
        Assert.Equal(otherRecord.Name, sent.Name);
        Assert.NotEqual(wrongPane.Instance.Key, sent.ID);
    }

    [Fact]
    public void FixedPage_EachPaneShowsAndKeepsItsOwnCountry()
    {
        var cut = Render<SideBySideCountries>(p => p.Add(x => x.Left, "A").Add(x => x.Right, "B"));

        var (left, right) = Panes(cut);
        cut.WaitForAssertion(() =>
        {
            Assert.Equal("A", left.Instance.Value.ID);
            Assert.Equal("B", right.Instance.Value.ID);
        });

        Assert.NotSame(left.Instance.Value, right.Instance.Value);
        Assert.Equal("Iraq", NameShownIn(left));
        Assert.Equal("Turkey", NameShownIn(right));
    }

    [Fact]
    public void FixedPage_SavingAPaneWritesOnlyItsOwnRecord()
    {
        string? putUrl = null, putBody = null;
        api.When(HttpMethod.Put, "http://localhost/Country/*").Respond(request =>
        {
            putUrl = request.RequestUri!.ToString();
            putBody = request.Content!.ReadAsStringAsync().Result;
            return Json(new { Entity = new { ID = "A", Name = "Iraq" } });
        });

        var cut = Render<SideBySideCountries>(p => p.Add(x => x.Left, "A").Add(x => x.Right, "B"));
        var (left, right) = Panes(cut);
        cut.WaitForAssertion(() =>
        {
            Assert.Equal("A", left.Instance.Value.ID);
            Assert.Equal("B", right.Instance.Value.ID);
        });

        left.Find("button[title*='Edit' i], button[aria-label*='Edit' i]").Click();
        cut.WaitForAssertion(() => Assert.Equal(ShiftSoftware.ShiftBlazor.Enums.FormModes.Edit, left.Instance.Mode));

        // Only the left pane changed mode: the fixed page has no shared Mode.
        Assert.Equal(ShiftSoftware.ShiftBlazor.Enums.FormModes.View, right.Instance.Mode);

        left.Find("form").Submit();
        cut.WaitForAssertion(() => Assert.NotNull(putUrl));

        Assert.Equal("http://localhost/Country/A", putUrl);
        var sent = Body(putBody);
        Assert.Equal("A", sent.ID);
        Assert.Equal("Iraq", sent.Name);
    }

    private void Country(string key, string name)
        => api.When(HttpMethod.Get, $"http://localhost/Country/{key}")
              .Respond(_ => Json(new { Entity = new { ID = key, Name = name } }));

    private static CountryDTO Body(string? json)
        => JsonSerializer.Deserialize<CountryDTO>(json!, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

    private static HttpResponseMessage Json(object content) => new(System.Net.HttpStatusCode.OK)
    {
        Content = new StringContent(JsonSerializer.Serialize(content), System.Text.Encoding.UTF8, "application/json"),
    };

    private static (IRenderedComponent<ShiftEntityForm<CountryDTO>> Left, IRenderedComponent<ShiftEntityForm<CountryDTO>> Right)
        Panes<TPage>(IRenderedComponent<TPage> cut) where TPage : class, Microsoft.AspNetCore.Components.IComponent
    {
        var forms = cut.FindComponents<ShiftEntityForm<CountryDTO>>();
        Assert.Equal(2, forms.Count);
        return (forms[0], forms[1]);
    }

    private static string? NameShownIn(IRenderedComponent<ShiftEntityForm<CountryDTO>> pane)
        => pane.Find("input").GetAttribute("value");
}
