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

The roadmap (done and planned features, by phase) lives in [ROADMAP.md](ROADMAP.md).

## ⚠️ Architectural Constraint: Template vs. Monthly Edits

The current edit flow modifies the `FinancialEntry` **template** — the parent record that drives all month projections. This is safe for display fields (`Description`, `Amount`, `Category`) because the projection engine already handles per-month overrides via `FinancialMonthlyEntry`.

However, **structural fields must not be edited on the template once entries exist in the database:**

- **EntryType (Income/Outcome):** Stored only on the template. Changing it retroactively flips the type for every month, including past months where a user already recorded a "Paid" state. Historical balances silently corrupt.
- **Recurrence / TotalInstallments:** Controls which months the entry is active in. Reducing `TotalInstallments` orphans physical `FinancialMonthlyEntry` records in the now-excluded months — those paid states disappear from projections even though they exist in the DB. Changing recurrence type (e.g. `Installments` → `Recurrent`) reshuffles the entire spread.

**Current mitigation:** The edit page disables the EntryType toggle and Recurrence picker when editing an existing entry.

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