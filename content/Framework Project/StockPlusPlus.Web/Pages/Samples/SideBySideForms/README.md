# Side-by-side forms: the `ShiftForm<,>` + `TheItem` bug

Two pages show the **same form body twice, side by side**, each pane loaded with a different country:

| Page | Route | Pattern |
|---|---|---|
| `LegacySideBySideCountries.razor` | `/Samples/LegacySideBySideCountries/{Left}/{Right}` | Legacy: `@inherits ShiftForm<,>`, body reads `TheItem` |
| `SideBySideCountries.razor` | `/Samples/SideBySideCountries/{Left}/{Right}` | Fixed: no `@inherits`, body reads `context.Item` |

`{Left}` and `{Right}` are two country keys, as shown in the ID column of `/CountryList`.

## What you see on the legacy page

Open it with two different countries, e.g. Iraq on the left and Turkey on the right:

- **Both panes show the same country** — whichever finished loading last (Turkey here).
- **Pressing Edit in one pane puts both panes in edit mode.**
- **Saving the left pane overwrites Iraq with Turkey's data.** The pane still has Iraq's key, so Save sends
  `PUT Country/{Iraq's key}` — but the body is `{ "id": "{Turkey's key}", "name": "Turkey" }`.

The first two are confusing; the third silently corrupts data.

## Why

`@inherits ShiftForm<LegacySideBySideCountries, CountryDTO>` turns `TheItem`, `Mode` and `FormContainer` into
**properties of the page**. A page has one of each, and both panes are bound to them:

```razor
<ShiftEntityForm @bind-Value="TheItem" @bind-Mode="Mode" @ref="FormContainer" Key="Left" ...>
<ShiftEntityForm @bind-Value="TheItem" @bind-Mode="Mode" @ref="FormContainer" Key="Right" ...>
```

What happens when the page opens:

1. Each `ShiftEntityForm` fetches its own record by `Key` and stores it with `SetValue`.
2. `SetValue` also raises `ValueChanged`, which `@bind-Value="TheItem"` writes into the page's **one** `TheItem`.
3. The page re-renders and passes `TheItem` back as `Value` to **both** forms.

So the last record to arrive ends up in every pane — the page's `TheItem`, the left form's `Value` and the right
form's `Value` are literally the same object. The body adds to it: `@bind-Value="TheItem.Name"` reads the page's
item, not the pane's, so even a pane that had its own record would display the other one.

Save uses the pane's **`Key`** for the URL but its **`Value`** for the body (`ShiftEntityForm.ItemUrl` and
`ValidSubmitHandler`), which is how one pane writes the other pane's record under its own key.

`@bind-Mode="Mode"` does the same to the mode: Edit in one pane sets the page's `Mode`, which both panes receive.
`@ref="FormContainer"` can hold only one of the two forms, so `ShiftForm`'s `Task` / `Disabled` tracking follows
just one pane (read from the code, not covered by a test).

### A second trap: `ShiftForm<,>` drops cascading parameters

`ShiftForm.SetParametersAsync` keeps only non-cascading parameters (`else if (!parameter.Cascading)`).
`[SupplyParameterFromQuery]` values are delivered as cascading parameters, so on a `ShiftForm<,>` page they are
**silently null** — the first version of this sample read `?Left=...&Right=...` and both panes opened empty in
Create mode. `[CascadingParameter]`s are dropped the same way. Both pages therefore take their keys from the route.

## The fix

Don't inherit `ShiftForm<,>` (it is `[Obsolete]`; its message describes these steps). Give each pane its own
state and let the body read the pane's context:

```razor
<ShiftEntityForm @bind-Value="_left"  Key="Left"  ... ChildContent="FormBody" />
<ShiftEntityForm @bind-Value="_right" Key="Right" ... ChildContent="FormBody" />

@code {
    private CountryDTO _left = new();
    private CountryDTO _right = new();

    private RenderFragment<FormChildContext<CountryDTO>> FormBody => context =>
        @<MudTextFieldExtended ReadOnly="context.ReadOnly"
                               @bind-Value="context.Item.Name"
                               For="@(() => context.Item.Name)" />;
}
```

- Each pane binds its **own** field, so a load in one pane can't reach the other.
- `context` is the pane's `FormChildContext<T>`: `context.Item` is that form's `Value`, and `context.Mode`,
  `context.ReadOnly` and `context.Disabled` are that form's state. The same body works in both panes because
  each form hands it a different context.
- There is no shared `Mode`: each form keeps its own.

Migrating a single-form page is the same move: remove the `@inherits`, add `[Parameter] public object? Key`, bind
`@bind-Key` and `@bind-Value` to the page's own fields, and replace `TheItem.X` / `Mode` / `ReadOnly` /
`Disabled` with `context.Item.X` / `context.Mode` / `context.ReadOnly` / `context.Disabled`
(see `Pages/ProductBrand/ProductBrandForm.razor`).

## Tests

`StockPlusPlus.Web.Tests/SideBySideFormsTests.cs` renders both pages with bUnit against a mocked API
(A = Iraq, B = Turkey):

| Test | Shows |
|---|---|
| `LegacyPage_EndsWithBothPanesShowingTheSameCountry` | both panes hold the same object and show the same name; one pane's key does not match its record |
| `LegacyPage_SavingOnePaneWritesTheOtherPanesRecordUnderItsKey` | Edit in one pane puts both in edit mode; Save sends `PUT Country/{its key}` with the other record |
| `FixedPage_EachPaneShowsAndKeepsItsOwnCountry` | left shows Iraq, right shows Turkey |
| `FixedPage_SavingAPaneWritesOnlyItsOwnRecord` | only the edited pane enters edit mode; Save sends `PUT Country/A` with Iraq |

The legacy tests pass **because** the bug is there: they pin the behaviour this sample documents.
