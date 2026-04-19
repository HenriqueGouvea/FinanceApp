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
- [ ] **Current month button:** A quick navigation button to jump back to the current month.
- [ ] **Full CRUD:** Implement Update and Delete functionality for all entries.
- [ ] **Incomes separation:** Separate Incomes from Outcomes in the UI.
- [ ] **Category Management:** Move from hardcoded categories to a dynamic CRUD with custom icons/colors.

### Phase 2: Advanced Features & Scaling
- [ ] **Credit Card Engine:** Logic for billing cycles and closing dates.
- [ ] **Advanced Reporting:** Charts and insights by category and period.
- [ ] **Multi-Platform Sync:** Future transition to a centralized Web API.
- [ ] **And much more...**

## 🛠 How to Run

1. Clone the repository.
2. Ensure you have the **.NET 9 SDK** installed.
3. Open `FinanceApp.slnx` in Visual Studio 2022 or VS Code.
4. Run the project on an Android Emulator, iOS Simulator, or Windows Machine.

---
*Developed with focus on architecture and performance.*