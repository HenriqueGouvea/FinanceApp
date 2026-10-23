![Build & Test Status](https://github.com/HenriqueGouvea/FinanceApp/actions/workflows/main.yml/badge.svg)
![Tests Passed](https://img.shields.io/badge/tests-passed-brightgreen)
![Coverage](https://img.shields.io/badge/coverage-85%25-blueviolet)

# FinanceApp Mobile

A professional personal finance management application built with **.NET MAUI** and **.NET 9**, focusing on a monthly perspective and automated expense distribution.

## 🚀 The Vision

This project goes beyond simple CRUD operations. It is designed to handle the complexity of personal finances, such as:
- **Monthly Navigation:** A seamless Carousel-based UI to manage finances per period.
- **Smart Projections:** Automated distribution of installments and recurring entries across months.
- **State-Driven UI:** Using the latest MVVM patterns to ensure a fluid and reactive user experience.

## 🛠 Tech Stack

- **Framework:** .NET MAUI (.NET 9)
- **Architecture:** MVVM (Model-View-ViewModel)
- **Toolkit:** CommunityToolkit.Mvvm
- **Database:** SQLite with Entity Framework Core
- **Patterns:** Dependency Injection, Repository Pattern, and Clean Code.
- **Unit Tests:** xUnit, FluentAssertions, NSubstitute (ViewModels with mocked services)
- **Integration Tests:** xUnit against SQLite In-Memory, validating the Projection Engine and services.
- **E2E Tests:** Appium 2 + UiAutomator2 driver, Appium .NET client (`Appium.WebDriver`), xUnit, FluentAssertions, and sqlite-net for on-device database assertions.

## 🏗 Architectural Principles

The development follows strict guidelines documented in our `CLAUDE.md` to ensure consistency:
- **SOLID** principles are at the core of every feature.
- **English-only** codebase and documentation.
- **Zero-comment policy** for self-explanatory code.
- **AI-Assisted Workflow:** Developed using **Claude Code** for high-precision refactoring and architectural alignment.

## 📱 Project Roadmap & Features

### Phase 1: Foundation & Core Logic
- [x] **Dynamic Month Navigation:** Sliding window navigation moving back and forth within a fixed range of 24 months.
- [x] **Single Entry Management:** Ability to add and track one-time expenses.
- [x] **Recurrence Engine:** Implement the 3 recurrence types (OneTime, Installments, Recurrent) with automated distribution.
- [x] **Separation between income and outcome entries:** Differentiate visually and functionally between incomes and expenses.
- [x] **Edit modal:** Opens when the user taps an entry. Edits Description, Amount, and Category on the template. EntryType and Recurrence are intentionally locked — see architectural constraint below.
- [x] **Delete entry:** Long-press (1s) an entry to slide in an actions menu from the right (currently a delete button; room for more actions). Tapping anywhere else slides it back out. OneTime entries are removed entirely; Recurrent/Installments entries offer three scopes: *this month only* (cancelled `FinancialMonthlyEntry`), *this and future months* (sets `EndDate` on the template), or *all months* (removes the template and its monthly records).
- [x] **Category Management:** Dynamic category CRUD (name, emoji icon, color) on a "Categorias" page reached via the hamburger menu. Entries show their category chip in the monthly list. Deleting a category keeps its entries and leaves them uncategorized. The former hardcoded categories are seeded on first run and existing entries are re-linked by name.
- [x] **Income and outcome UI improvements:** Change the UI of the income and outcome list so that it becomes easier to identify that they are two different sessions. In addition, it should be clear that the sessions can be expanded/collapsed.
- [x] **UX improvements:** Description field auto-focused on popup open; income/outcome selection is a swipe/tap pill toggle defaulting to Outcome; the amount field only appears after the recurrence is chosen and is labelled accordingly ("Valor total", "Valor mensal" or "Valor de cada parcela", with a live installment total).
- [x] **Improve total amount for Installment entries:** Show the total amount of the installment in the list, not just the current month amount.
- [x] **E2E tests:** Appium 2 + UiAutomator2 suite in `FinanceApp.UITests` that drives the real Android app on an emulator. It uses page objects located by `AutomationId`, asserts on the app's on-device SQLite database, and has a GitHub Actions workflow that runs on every merge to `master`. Starts with the add-entry flow; see [FinanceApp.UITests/README.md](FinanceApp.UITests/README.md) to run it.
- [x] **Improve CI automation:** Claude opens the pull request itself at the end of each task with the GitHub CLI (`gh`), using the PR template in `.github/pull_request_template.md` (summary, test results, roadmap item), then checks its CI. The E2E workflow now runs on every merge to `master`.
- [x] **Status control:** The long-press actions menu gains a "Pagar" (outcomes) / "Receber" (incomes) action that marks the entry as paid for that month only, and a "Pendente" action to undo it. Paid entries show a filled check icon and a "Pago"/"Recebido" label; pending entries show an empty ring. The status is lazily persisted as a `FinancialMonthlyEntry` for that month, so other months of a recurrent or installment entry stay pending.
- [x] **Separate income and outcome categories:** Each category now has a type (Despesa/Receita), chosen with a toggle in the category popup. The "Categorias" page lists them in two groups, and the entry popup only offers categories matching the selected entry type. Existing categories are migrated as outcome categories, and "Salário" and "Investimentos" are seeded as income categories.
- [x] **Improve month navigation:** Tapping the month title (marked with a ▾) opens a month/year picker: a year switcher and a 3×4 month grid limited to the ±24-month range, plus a "Mês atual" shortcut. When away from the current month, a floating "Mês atual" pill appears at the bottom of the screen, with an arrow pointing toward the current month, to jump back in one tap.

### Phase 2: Advanced Features for the mobile app
- [x] **Add currency mask to the amount fields:** Every amount in the app (entry rows, section totals, month summary, installment totals) is shown as Brazilian currency, e.g. "R$ 1.000,00" (negative balances as "-R$ 1.000,00"). The amount field in the entry popup is masked the same way while typing: digits fill in from the cents, like a banking app (typing 1, 5, 0, 0 gives "R$ 15,00").
- [x] **Improve the 'recorrente' option:** The recurrence picker now reads "Todo mês (recorrente)" so its meaning is clear to every user.
- [x] **Improve the month picker button:** The month title is now a large tappable pill with a filled ▾ badge that opens the month/year picker, flanked by 48dp "‹" / "›" buttons that step one month (disabled at the ends of the ±24-month range). Below it, a hint ("Toque no mês para escolher ou deslize para os lados") teaches both the picker and swiping; it is dismissed for good the first time the user changes month.
- [x] **Entry paid checks are red:** The paid check icon and the "Pago"/"Recebido" label now use the same green for incomes and outcomes, instead of the outcome red that could read as an error.
- [ ] **Add paid/pending amount to the month summary:**The month summary currently has only income, outcomes and balance. It should also show the pending and paid entries on that month.
- [ ] **Partial payment and estimated amount:** Add the option to add the partial payment of an entry like supermarket or restaurants where users spend this value progressively during the month. This value ('valor já gasto' or some other better label) should be shown in the list and the user can set it when open the entry edit window. In addition, entries may be marked as estimated amount for entries that have different amount on months but the user can estimate, for example, energy or water bill, supermarket, restaurants, etc. These entries should have an icon marking them as estimated amount.
- [ ] **Payment source engine:** Add a new page accessed via the hamburger menu with a CRUD for the payment sources. When adding a new entry there should have the option to link to a payment source which can be a bank account or a credit card. It's any place where the user's payment comes from.
- [ ] **Due date:**Add due date to the entries with the possibility to add reminders using push notifications and/or within the app in the month summary.
- [ ] **Advanced Reporting:** Charts and insights by category and period.
- [ ] **Add a setup page for new users:** This page should be shown for the first time a user opens the app, asking things like name, picture (optional), salary, house spends (if it's the case), and other information to configure a pre-existent data for his finance. It also should as for an optional password to open the app.
- [ ] **Create a market identity and splash screen:** Create a logo, coloring, marketing identity, etc.
- [ ] **Improve UI/UX:**We need to improve the UI/UX of the app, mainly on the monthly entries screen (where we see the incomes, outcomes, summary, month picker), when we have most of the features and based on the markieting identity (task above). To accomplish this, 3 different models should be created with mocked up data and be available on the hamburger menu so that we can decide which one we want to pick.
- [ ] **Add translation and locale resourse strings**: PT-BR, PT-PT, EN-US

### Phase 3: Advanced Features & Scaling
- [ ] **Multi-Platform Sync:** Future transition to a centralized Web API.

## ⚠️ Architectural Constraint: Template vs. Monthly Edits

The current edit flow modifies the `FinancialEntry` **template** — the parent record that drives all month projections. This is safe for display fields (`Description`, `Amount`, `Category`) because the projection engine already handles per-month overrides via `FinancialMonthlyEntry`.

However, **structural fields must not be edited on the template once entries exist in the database:**

- **EntryType (Income/Outcome):** Stored only on the template. Changing it retroactively flips the type for every month, including past months where a user already recorded a "Paid" state. Historical balances silently corrupt.
- **Recurrence / TotalInstallments:** Controls which months the entry is active in. Reducing `TotalInstallments` orphans physical `FinancialMonthlyEntry` records in the now-excluded months — those paid states disappear from projections even though they exist in the DB. Changing recurrence type (e.g. `Installments` → `Recurrent`) reshuffles the entire spread.

**Current mitigation:** The edit popup disables the EntryType toggle and Recurrence picker when editing an existing entry.

**Future solution:** Implement a two-scope edit:
1. **Edit this month only** → Creates/updates a `FinancialMonthlyEntry` override for that specific period (status, partial amount, cancellation). This is the planned "custom monthly entry" system.
2. **Edit the template (all future months)** → For structural changes, soft-delete the old template (mark as cancelled from the changed month forward) and create a new template, preserving the physical records of the old one for past months.

Until scope-aware editing is implemented, structural fields on the template must be treated as immutable after creation.

## 🛠 How to Run

1. Clone the repository.
2. Ensure you have the **.NET 9 SDK** installed.
3. Open `FinanceApp.slnx` in Visual Studio 2022 or VS Code.
4. Run the project on an Android Emulator, iOS Simulator, or Windows Machine.

## ✅ Quality Assurance & Testing

The project maintains a high-quality bar through automated testing, ensuring that the financial logic (the most critical part) is always protected.

### Testing Strategy
- **Unit Tests:** All ViewModels (`Main`, `Month`, `AddEntry`) are tested in isolation using **Mocks**, ensuring UI logic and state transitions are correct.
- **Integration Tests:** The `FinanceService` and `ProjectionEngine` are tested against a real **In-Memory SQLite** instance to validate complex recurrence scenarios.

### Core Test Scenarios
- **Projection Accuracy:** Validates that Installments appear for exactly $N$ months and Recurrent entries persist indefinitely.
- **Data Integrity:** Ensures that manual overrides (Physical Records) correctly merge with virtual templates.
- **UI State:** Verifies that month navigation correctly triggers data loading for the carousel window (±24 months).

To run tests locally:
```bash
dotnet test FinanceApp.Tests/FinanceApp.Tests.csproj

---
*Developed with focus on architecture and performance.*