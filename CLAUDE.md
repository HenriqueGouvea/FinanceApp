# FinanceApp - Guidelines & Architecture

## Tech Stack
- **Framework:** .NET 10 (MAUI)
- **Runtime:** Android 13.0 (API 33) - Galaxy Nexus Emulator
- **Architecture:** MVVM (CommunityToolkit.Mvvm)
- **Database:** SQLite-net-pcl (Fluent mapping via CreateFlags)

## Project Structure
- **Data:** Database context, connection management, and DB initialization.
- **Models:** Domain entities (Clean POCOs, avoiding infra attributes).
- **Services:** Business logic and data persistence orchestration.
- **ViewModels:** UI State and logic.
- **Views:** XAML pages and Popups.

## Coding Standards (SOLID & DRY)
- **Clean Code:** Always follow SOLID, DRY, and YAGNI principles.
- **Null Safety:** Initialize strings with `string.Empty` (e.g., `public string Name { get; set; } = string.Empty;`).
- **DI:** Always resolve dependencies via Constructor Injection. Avoid manual instantiation of services/VMs.
- **Comments:** Avoid comments for self-explanatory code. If a complex logic requires explanation, **comment in English**.

## Naming Conventions
- **Private fields:** camelCase with `_` prefix (e.g., `_financeService`).
- **Observable Properties:** camelCase WITHOUT prefix when using `[ObservableProperty]` (e.g., `[ObservableProperty] string title;` will generate `Title`).
- **Methods/Classes:** PascalCase.

## Persistence Strategy
- **Current:** All DB access via `IFinanceService` using the `DatabaseContext` Singleton.
- **Future-Proofing:** Prepare for Domain Model Mapping. Keep Models clean of persistence logic to facilitate future separation between DTOs and Entities.
- **Fluent Mapping:** Use `CreateFlags.ImplicitPK | CreateFlags.AutoIncPK` in `CreateTableAsync` to maintain clean domain models.

## Workflow & Build
- Build Command: `dotnet build`
- Primary Target: `net10.0-android`

## Business Logic & Vision
- **Monthly Perspective:** The app focuses on a monthly financial view.
- **Automated Distribution:** Incomes and outcomes (Installments or Recurrent) must be automatically distributed across months.
- **Entry Types:** - `OneTime`: Single occurrence.
  - `Installments`: Fixed number of payments spread over N months.
  - `Recurrent`: Monthly recurring entries without a fixed end date.
- **Future Roadmap:** Transitioning to a distributed architecture (Web App + MAUI) consuming a centralized Web API. Logic must be decoupled from local storage to facilitate this migration.

## Domain Model Reference (Vision)
The system will evolve into a sophisticated projection-based model:

- **FinancialEntry:** The "Template" or "Parent" entry. Can be Income or Outcome. 
  - **Recurrence:** `OneTime`, `Installments` (fixed end date), or `Recurrent` (perpetual).
  - **Flexibility:** Supports various intervals (Monthly, Weekly, Biweekly or custom).
  - **Automated Projection:** The system projects `FinancialMonthlyEntry` instances based on the `FinancialEntry` rules (e.g., a 5th-of-the-month DueDate).
  - **PaymentSource:** Linked to `BankAccount` or `CreditCard`.

- **FinancialMonthlyEntry:** The "Instance" or "Occurrence". 
  - Represents the actual state of a `FinancialEntry` in a specific `Month`.
  - **Status:** `Pending`, `Paid`, or `PartiallyPaid`.
  - **PartiallyPaid Logic:** Specifically for progressive expenses (e.g., Supermarket). Allows tracking accumulated spending against a `TotalAmount` throughout the month.

- **Month:** The Period Container.
  - Aggregates `FinancialMonthlyEntry` instances for a specific Year/Month.
  - Acts as the root for monthly reporting and dashboard views.

- **Categorization:** Dynamic mapping via `FinancialCategoryEntry`. Allows a `FinancialEntry` to belong to multiple categories for granular reporting.

- **Payment & Tracking:** - `FinancialEntryStatus`: Lifecycle of the parent (InProgress, Cancelled, Finished).
  - `FinancialMonthlyEntryStatus`: Immediate state of the month's obligation.