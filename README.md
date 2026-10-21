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
- [ ] **Status control:** Add the possibility to change the status of an entry within the month (ie.: an installment paid on that month). It has a visual identification of that status too, maybe just an icon or whataver is better for the UI/UX.
- [ ] **Separate income and outcome categories:** When the user adds an income, it should show just income category options. The same should be valid for outcomes.
- [ ] **Improve month navigation:** Add a quick navigation button to jump back to the current month. Add also the possibility to jump to a specific month an year. These options should have a good UI/UX, because we can't start adding a lot of buttons on the top.

### Phase 2: Advanced Features & Scaling
- [ ] **Create a splash screen:** Check how to design a new one.
- [ ] **Add a setup page for new users:** This page should be shown for the first time a user opens the app, asking things like name, picture (optional), salary, house spends (if it's the case), and other information to configure a pre-existent data for his finance. It also should as for an optional password to open the app.
- [ ] **Partial payment:** Add the option to add the partial payment of an entry like supermarket or restaurants. This value should be shown in the list and the user can set it open the entry edit window.
- [ ] **Payment source engine:** Add a new page accessed via the hamburger menu with a CRUD for the payment sources. When adding a new entry there should have the option to link to a payment source which can be a bank account or a credit card.
- [ ] **Multi-Platform Sync:** Future transition to a centralized Web API.
- [ ] **Advanced Reporting:** Charts and insights by category and period.
- [ ] **Add translation and locale resourse strings**: PT-BR, PT-PT, EN-US
- [ ] **And much more...**

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