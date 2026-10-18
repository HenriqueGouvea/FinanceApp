# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Tech Stack
- **Framework:** .NET 10 (MAUI)
- **Runtime:** Android 13.0 (API 33) - Galaxy Nexus Emulator
- **Architecture:** MVVM (CommunityToolkit.Mvvm)
- **Database:** SQLite-net-pcl (Fluent mapping via CreateFlags)

## Build & Run
- Build: `dotnet build`
- Primary Target: `net10.0-android`
- Deploy to emulator: `dotnet build -t:Run -f net10.0-android`

## Testing
**Run all tests:** `dotnet test FinanceApp.Tests/FinanceApp.Tests.csproj`

**Run a single test:** `dotnet test FinanceApp.Tests/FinanceApp.Tests.csproj --filter "FullyQualifiedName~MethodName_Scenario_ExpectedResult"`

**Three-tier strategy:**
- **Service tests** (`FinanceApp.Tests/Services/`) — hit a real in-memory SQLite via `TestDatabaseContext`. No mocks.
- **ViewModel tests** (`FinanceApp.Tests/ViewModels/`) — mock `IFinanceService` with NSubstitute. No DB.
- **E2E tests** (`FinanceApp.UITests/`) — drive the real Android app and assert on UI and the on-device DB. See "E2E tests" below.

**Definition of done:** a feature or bug fix is not complete with unit tests alone. Any change that adds or alters user-visible behaviour (a new screen, popup, gesture, list or label reaction, or a new DB write triggered from the UI) must also add or update E2E tests covering the happy path and the key variations (e.g. each delete scope, each recurrence type). Then build the APK and run the E2E suite (or the affected test classes) before reporting the work as done. Pure service/ViewModel refactors with no UI impact only need unit tests.

**Infrastructure:**
- `TestDatabaseContext` (`FinanceApp.Tests/Infrastructure/`) — implements `IDatabaseContext` using a GUID-named SQLite in-memory database (`file:{guid}?mode=memory&cache=shared`). The connection is opened once in the constructor and kept alive for the test lifetime.
- `FinanceApp.csproj` includes `net10.0` as an additional TFM alongside the MAUI platform targets so the test project can reference it. `OutputType` and `UseMaui` are conditional on non-`net10.0` builds.

**Isolation:** xUnit instantiates a new test class per test method. Each test declares `TestDatabaseContext` and `FinanceService` (or `NSubstitute` mocks) as constructor-injected fields, giving a fresh isolated environment per test with zero setup boilerplate.

**Naming convention:** `MethodName_Scenario_ExpectedResult` (e.g., `SaveEntryAsync_RecurrentEntry_AppearsFromStartDateOnwards`).

**Date normalization:** Always use `new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Local)` for `StartDate` in tests to match the `DateTimeKind.Local` comparisons inside `IsActiveForMonth`.

**Member ordering inside test classes:** private fields → constructor → `[Fact]` methods (grouped by the service method under test).

### E2E tests
`FinanceApp.UITests/` — Appium 2 + UiAutomator2 driving the Android app on an emulator (`emulator-5554` must be running). Not part of the default `dotnet test` above. Full setup in `FinanceApp.UITests/README.md`.

**Run:**
1. Build the APK (required after any app change; Fast Deployment APKs can't be used): `dotnet build FinanceApp/FinanceApp.csproj -f net10.0-android -c Debug -p:EmbedAssembliesIntoApk=true -o artifacts/e2e-apk`
2. `dotnet test FinanceApp.UITests/FinanceApp.UITests.csproj` (single class: `--filter "FullyQualifiedName~DeleteEntryTests"`). The full suite takes ~10 minutes.

**Structure:**
- `Tests/` — one class per feature area (`AddEntryTests`, `EditEntryTests`, `DeleteEntryTests`, `EntryActionsMenuTests`, `MonthNavigationTests`, `SectionToggleTests`, `CategoryTests`). Each class derives from `E2ETestBase` and must carry `[Collection(E2ECollection.Name)]` and `[Trait("Category", "E2E")]`. Shared arrange steps (e.g. creating the entry under test) go in the constructor.
- `Pages/` — page objects (`MainPageObject`, `AddEntryPopupObject`, `CategoriesPageObject`). Tests never call Appium directly; add a page object method instead. User-facing pt-BR texts the tests select by live in `AppLabels`.
- `Infrastructure/` — `AppSession` (driver, waits, gestures, diagnostics), `DeviceDatabase` + `TableRows` (DB assertions), `AppFormat` (expected money/month/count strings and enum values).

**Rules:**
- Locate elements by `AutomationId` (Android `resource-id`). Any element a test interacts with or asserts on needs an `AutomationId` in XAML, named after its role (e.g. `OutcomeTotalLabel`, `SaveEntryButton`).
- Never hardcode formatted values: use `AppFormat.Money`, `AppFormat.MonthTitle`, `AppFormat.EntryCount`.
- Always wait (`WaitForVisible`, `WaitForText`, `WaitForGone`), never sleep. The month `CarouselView` keeps neighbour months in the tree, so only consider *displayed* elements.
- Assert both the UI reaction and the persisted state (`GetEntryRowsAsync`, `GetMonthlyEntryRowsAsync`, `GetCategoryRowsAsync`) when a flow writes to the DB. Row types in `TableRows.cs` mirror the tables and must be updated when the schema changes.
- Every test gets a fresh Appium session with app data wiped (empty DB, default categories seeded), so tests must not depend on each other.
- On a timeout, a screenshot + page source are saved to `FinanceApp.UITests/TestResults/diagnostics/`. Check them first when debugging.

## Project Structure
- **Data/:** `DatabaseContext.cs` — SQLite singleton connection + table creation via `GetConnectionAsync()`. `IDatabaseContext` is the interface used for DI and test substitution.
- **Models/:** Clean POCOs (no persistence attributes). `FinancialEntry` is the template/parent; `FinancialMonthlyEntry` is the physical override; `MonthlyEntryProjection` is the non-persisted read model returned to ViewModels.
- **Services/:** All DB access is mediated by `IFinanceService` / `FinanceService`. Business logic lives here, not in ViewModels.
- **ViewModels/:** UI state only. Depend on `IFinanceService` via constructor injection.
- **Views/:** XAML pages and `CommunityToolkit.Maui.Views.Popup` subclasses.
- **Controls/:** Reusable code-only UI controls (e.g. `SlideOutPanel`, the animated entry actions menu revealed by long press).
- **Converters/:** Value converters use a `static readonly Instance` singleton pattern (e.g., `EntryTypeToColorConverter.Instance`). They are not registered in DI — reference them directly in XAML via `{x:Static}`.

## DI Registration
All registrations are in `MauiProgram.cs`:
- `IDatabaseContext` → Singleton (resolved by `DatabaseContext`)
- `IFinanceService`, `ICategoryService`, `IPreferencesService` → Singleton
- `IMessenger` → Singleton (`WeakReferenceMessenger.Default`), used to broadcast `CategoriesChangedMessage` so `MainViewModel` refreshes
- `MainViewModel`, `AddEntryViewModel`, `CategoriesViewModel`, `CategoryEditViewModel`, `MainPage`, `AddEntryPopup`, `CategoriesPage`, `CategoryEditPopup` → Transient

Never manually instantiate services or ViewModels. If a new service or VM is added, register it in `MauiProgram.cs`.

## Data Flow Architecture
```
SQLite (FinanceDataV3.db3)
  └─ DatabaseContext (Singleton, lazy-init connection)
       └─ FinanceService (all queries via GetConnectionAsync())
            └─ MainViewModel / MonthViewModel (no direct DB access)
                 └─ MainPage / AddEntryPopup (XAML binding only)
```

## Key Architectural Patterns

**Observable Properties:** Use `[ObservableProperty]` from CommunityToolkit.Mvvm. The backing field uses `camelCase` with NO `_` prefix (e.g., `[ObservableProperty] string title;` generates `Title`). The `_` prefix is reserved for non-observable private fields injected via constructor.

**Popup → ViewModel Communication:** `AddEntryPopup` receives `AddEntryViewModel` via DI. After a save, the ViewModel invokes a callback delegate (`SetSaveCallback`) that the parent ViewModel registered, so `MainViewModel` refreshes the active `MonthViewModel` without coupling Views to ViewModels.

**Popup Resolution at Runtime:** Popups are resolved from the DI container in code-behind via `Handler?.MauiContext?.Services.GetService<T>()`, not via constructor injection into the page. This is the required pattern because `MainPage` is Transient and the popup must be a fresh instance each open.

**Table Creation:** Use `CreateFlags.ImplicitPK | CreateFlags.AutoIncPK` in `DatabaseContext.GetConnectionAsync()` so domain models stay free of SQLite attributes.

**Async DB Access:** All DB operations are async. `GetConnectionAsync()` uses a lazy-init pattern — call it at the start of every service method.

**Fixed Timeline:** The dashboard uses a fixed range of ±24 months from the current date, pre-populated during `InitializeAsync`. Do not use dynamic scroll extension (lazy loading of months) to maintain CarouselView stability.

## Coding Standards (SOLID & DRY)
- **Null Safety:** Initialize strings with `string.Empty` (e.g., `public string Name { get; set; } = string.Empty;`).
- **DI:** Always resolve dependencies via Constructor Injection. Avoid manual instantiation of services/VMs.
- **Comments:** Avoid comments for self-explanatory code. If complex logic requires explanation, **comment in English**.

## Naming Conventions
- **Private fields:** camelCase with `_` prefix (e.g., `_financeService`).
- **Observable Properties:** camelCase WITHOUT `_` prefix when using `[ObservableProperty]`.
- **Methods/Classes:** PascalCase.

## Persistence Strategy
- **Current:** All DB access via `IFinanceService` using the `DatabaseContext` Singleton.
- **Future-Proofing:** Keep Models clean of persistence logic to facilitate future separation between DTOs and Entities.
- **Fluent Mapping:** Use `CreateFlags.ImplicitPK | CreateFlags.AutoIncPK` in `CreateTableAsync` to maintain clean domain models.

## Business Logic & Vision
- **Monthly Perspective:** The app focuses on a monthly financial view.
- **Automated Distribution:** Incomes and outcomes (Installments or Recurrent) must be automatically distributed across months.
- **Entry Types:**
  - `OneTime`: Single occurrence.
  - `Installments`: Fixed number of payments spread over N months.
  - `Recurrent`: Monthly recurring entries without a fixed end date.
- **Future Roadmap:** Transitioning to a distributed architecture (Web App + MAUI) consuming a centralized Web API. Logic must be decoupled from local storage to facilitate this migration.

## Domain Model Reference (Vision)
The system will evolve into a projection-based model:

- **FinancialEntry:** The "Template" or "Parent" entry. Can be Income or Outcome.
  - **Recurrence:** `OneTime`, `Installments` (fixed end date), or `Recurrent` (perpetual).
  - **Flexibility:** Supports various intervals (Monthly, Weekly, Biweekly or custom).
  - **Automated Projection:** The system projects `FinancialMonthlyEntry` instances based on the `FinancialEntry` rules (e.g., a 5th-of-the-month DueDate).
  - **PaymentSource:** Linked to `BankAccount` or `CreditCard`.

- **FinancialMonthlyEntry:** The "Instance" or "Occurrence".
  - Represents the actual state of a `FinancialEntry` in a specific `Month`.
  - **Status:** `Pending`, `Paid`, or `PartiallyPaid`.
  - **PartiallyPaid Logic:** For progressive expenses (e.g., Supermarket). Tracks accumulated spending against a `TotalAmount` throughout the month.
  - **Lazy Persistence:** Only persisted when the user modifies a specific month. Otherwise, it exists only as a virtual projection computed at runtime.

- **Month:** The Period Container.
  - Aggregates `FinancialMonthlyEntry` instances for a specific Year/Month.
  - Acts as the root for monthly reporting and dashboard views.

- **Categorization:** Dynamic mapping via `FinancialCategoryEntry`. Allows a `FinancialEntry` to belong to multiple categories for granular reporting.

- **Payment & Tracking:**
  - `FinancialEntryStatus`: Lifecycle of the parent (InProgress, Cancelled, Finished).
  - `FinancialMonthlyEntryStatus`: Immediate state of the month's obligation.

## Merge Logic (Projection Engine)
When loading a month, the service must merge two sources:
1. **Physical records:** `FinancialMonthlyEntry` rows in the DB for that period.
2. **Virtual projections:** `FinancialEntry` templates active in that period that have no corresponding physical instance.

A `FinancialEntry` template is "active" in a given month when:
- `OneTime`: `StartDate` falls within that month.
- `Installments`: month is between `StartDate` and `StartDate + TotalInstallments months`.
- `Recurrent`: month is on or after `StartDate` (no end date).
- For every type, when `EndDate` is set, the month must also be on or before `EndDate` (set by the "this and future months" delete scope).

The merge result is a unified list consumed by `MonthViewModel`. Only physical `FinancialMonthlyEntry` records represent user-confirmed state; virtual projections are computed on-the-fly and not written to the DB unless the user acts on them.
