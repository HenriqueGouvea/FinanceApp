# FinanceApp E2E tests (Android)

Black-box UI tests driving the real app on an emulator via **Appium 2 + UiAutomator2**, written in C# / xUnit.
Each test also asserts on the app's SQLite database, pulled from the device after the UI flow.

## Prerequisites (one time)

- Android emulator running (default serial `emulator-5554`, e.g. `galaxy_nexus_-_api_33`).
- Node.js ≥ 18, then install the pinned local Appium + driver:
  ```bash
  cd FinanceApp.UITests && npm install
  ```

## Run

```bash
# 1. Build a standalone, debuggable APK (Fast Deployment APKs can't be installed by Appium)
dotnet build FinanceApp/FinanceApp.csproj -f net10.0-android -c Debug -p:EmbedAssembliesIntoApk=true -o artifacts/e2e-apk

# 2. Run the suite (the Appium server is started and stopped automatically)
dotnet test FinanceApp.UITests/FinanceApp.UITests.csproj
```

Each test runs in a fresh Appium session with app data wiped, so it starts from an empty database.
The APK is force-reinstalled once per run. That replaces any Fast Deployment install (`dotnet build -t:Run`).
You'll need to redeploy from the IDE afterwards.

| Env var | Default | Purpose |
|---|---|---|
| `E2E_APK_PATH` | `artifacts/e2e-apk/com.companyname.financeapp-Signed.apk` | APK under test |
| `E2E_DEVICE` | `emulator-5554` | adb serial of the target device |
| `E2E_APPIUM_URL` | *(local server started)* | Use an already running Appium server |
| `ANDROID_HOME` | VS / Android Studio default SDK path | Android SDK location (adb, UiAutomator2) |

When a wait times out, the page source and a screenshot are saved under `FinanceApp.UITests/TestResults/diagnostics/`.

## Writing tests

- Derive from `E2ETestBase` and add `[Collection(E2ECollection.Name)]` and `[Trait("Category", "E2E")]`. Put shared arrange steps (e.g. creating the entry under test) in the constructor.
- Locate elements by `AutomationId` (it surfaces as the Android `resource-id`). Add one in XAML for every element a test touches.
- Interact through page objects (`Pages/`); keep raw Appium calls out of test methods. pt-BR texts the tests select by live in `AppLabels`.
- Compute expected texts with `AppFormat` (`Money`, `MonthTitle`, `EntryCount`) instead of hardcoding them.
- The month `CarouselView` keeps neighbour months in the tree, so `AppSession` helpers only consider *displayed* elements.
- Popup buttons can sit below the fold on small screens; tap them with `ScrollToAndTap`.
- Assert persisted state with `GetEntryRowsAsync` / `GetMonthlyEntryRowsAsync` / `GetCategoryRowsAsync`. The row types in `Infrastructure/TableRows.cs` mirror the tables on purpose and don't reference the app's models.

## Coverage

| Class | Flows |
|---|---|
| `AddEntryTests` | One-time outcome, income, recurrent (start month onwards), installments (exactly N months, labels, total), category chip, cancel |
| `EditEntryTests` | Pre-filled popup with locked structural fields; description/amount change across months |
| `DeleteEntryTests` | One-time; recurrent *this month only*, *this and future*, *all months*; cancelled action sheet |
| `EntryActionsMenuTests` | Long-press reveals menu; tapping outside or on the entry hides it without opening the editor |
| `MonthNavigationTests` | Fresh-install state, swipe to next/previous month, entry created in a non-current month |
| `SectionToggleTests` | Collapse/expand outcomes; collapsed state shared across months |
| `CategoryTests` | Seeded defaults, add category and use it on an entry, delete a category used by an entry |
