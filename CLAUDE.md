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

## Project Structure
- **Data/:** `DatabaseContext.cs` — SQLite singleton connection + table creation via `GetConnectionAsync()`.
- **Models/:** Clean POCOs (no persistence attributes). `FinancialEntry` is the current domain entity.
- **Services/:** All DB access is mediated by `IFinanceService` / `FinanceService`. Business logic lives here, not in ViewModels.
- **ViewModels/:** UI state only. Depend on `IFinanceService` via constructor injection.
- **Views/:** XAML pages and `CommunityToolkit.Maui.Views.Popup` subclasses.

## DI Registration
All registrations are in `MauiProgram.cs`:
- `IDatabaseContext` → Singleton (resolved by `DatabaseContext`)
- `IFinanceService` → Singleton
- `MainViewModel`, `AddEntryViewModel`, `MainPage`, `AddEntryPopup` → Transient

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

**Table Creation:** Use `CreateFlags.ImplicitPK | CreateFlags.AutoIncPK` in `DatabaseContext.GetConnectionAsync()` so domain models stay free of SQLite attributes.

**Async DB Access:** All DB operations are async. `GetConnectionAsync()` uses a lazy-init pattern — call it at the start of every service method.

**Fixed Timeline:** The dashboard uses a fixed range of ±24 months from the current date, pre-populated during InitializeAsync. Do not use dynamic scroll extension (lazy loading of months) to maintain CarouselView stability.

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

## Testing

**Project:** `FinanceApp.Tests/` — xUnit + FluentAssertions + sqlite-net-pcl, targets `net10.0`.

**Run tests:** `dotnet test FinanceApp.Tests/FinanceApp.Tests.csproj`

**Infrastructure:**
- `IDatabaseContext` — interface extracted from `DatabaseContext` so tests can supply their own implementation.
- `TestDatabaseContext` (`FinanceApp.Tests/Infrastructure/`) — implements `IDatabaseContext` using a GUID-named SQLite in-memory database (`file:{guid}?mode=memory&cache=shared`). The connection is opened once in the constructor and kept alive for the test lifetime to prevent the in-memory DB from being destroyed.
- `FinanceApp.csproj` includes `net10.0` as an additional TFM (alongside the MAUI platform targets) so the test project can reference it. `OutputType` and `UseMaui` are conditional on non-`net10.0` builds.

**Isolation:** xUnit instantiates a new test class per test method. Each test declares `TestDatabaseContext` and `FinanceService` as constructor-injected fields, so every test automatically gets a fresh, isolated DB with zero setup boilerplate.

**Naming convention:** `MethodName_Scenario_ExpectedResult` (e.g., `SaveEntryAsync_RecurrentEntry_AppearsFromStartDateOnwards`).

**Date normalization:** Always use `new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Local)` for `StartDate` in tests to match the `DateTimeKind.Local` comparisons inside `IsActiveForMonth`.

**Member ordering inside test classes:** private fields → constructor → `[Fact]` methods (grouped by the service method under test).

## Merge Logic (Projection Engine)
When loading a month, the service must merge two sources:
1. **Physical records:** `FinancialMonthlyEntry` rows in the DB for that period.
2. **Virtual projections:** `FinancialEntry` templates active in that period that have no corresponding physical instance.

A `FinancialEntry` template is "active" in a given month when:
- `OneTime`: `StartDate` falls within that month.
- `Installments`: month is between `StartDate` and `StartDate + TotalInstallments months`.
- `Recurrent`: month is on or after `StartDate` (no end date).

The merge result is a unified list consumed by `MonthViewModel`. Only physical `FinancialMonthlyEntry` records represent user-confirmed state; virtual projections are computed on-the-fly and not written to the DB unless the user acts on them.
