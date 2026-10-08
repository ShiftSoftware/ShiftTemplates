# Rule-driven banners (`ShiftBanners`)

Warning banners above every page, driven by a rules file instead of code.

| Piece | File |
|---|---|
| Rules | `wwwroot/banners.json` |
| Registration | `Program.cs`: `builder.Services.AddShiftBlazorBanners();` |
| Placement + condition predicate | `Pages/Samples/Banners/BannerLayout.razor` |
| Wiring the layout | `App.razor` (`MainLayout="mainLayout"`) and `App.razor.cs` (sample only) |

`BannerLayout` is a nested layout: it renders `<ShiftBanners />` and then the page, inside the framework's
`ShiftMainLayout`, so the framework layout is used as-is.

## The rules in this sample

| Rule | Pages | Shown when | Languages |
|---|---|---|---|
| `invoice-month-end` | `/InvoiceList`, `/InvoiceForm/**` | the user can write invoices (`include`) | en, ar, ku |
| `product-prices` | `/ProductList`, `/ProductForm/**` | the user can **not** write products (`exclude`) | en, ar — Kurdish users see English |
| `development-build` | every page (`/**`) | running in Development (`include`) | en |

## How a rule is decided

1. **Path.** Each pattern in `paths` is matched against the page path (query string ignored, case-insensitive).
   `*` stays within one segment, `?` is one character, `**` spans segments — `/InvoiceForm/**` covers
   `/InvoiceForm` and `/InvoiceForm/12`.
2. **Conditions.** `include` / `exclude` are just names. `ShiftBanners` asks the `IsConditionMet` predicate the app
   passes in (here: TypeAuth and the host environment). With `include`, at least one must be met; any met `exclude`
   hides the rule.
3. **Language.** The text for the user's culture (`ar-IQ`), else its language (`ar`), else `en`; a rule with neither
   is not shown.

Change `banners.json` and reload the app: no rebuild is needed.
