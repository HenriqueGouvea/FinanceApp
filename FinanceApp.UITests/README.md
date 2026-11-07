# FinanceApp E2E tests (Android)

Black-box UI tests driving the real app on an emulator via **Appium 2 + UiAutomator2**, written in C# / xUnit.
Each test also asserts on the app's SQLite database, pulled from the device after the UI flow.

## Prerequisites (one time)

- Android emulator running (default serial `emulator-5554`, e.g. `galaxy_a16_-_api_35`, Android 15).
- 3-button navigation on the emulator: `adb shell cmd overlay enable-exclusive --category com.android.internal.systemui.navbar.threebutton` (once per AVD; it survives a normal shutdown, but recheck it with `adb shell cmd overlay list | grep navbar` after the emulator was killed or cold booted). Android 15 images default to gesture navigation, where a scroll swipe that starts near the bottom edge is taken as "go home" and the app leaves the screen mid-test, mostly at CI's 320×640 size. A real Galaxy A16 ships with the 3-button bar too.
- Node.js ≥ 18, then install the pinned local Appium + driver:
  ```bash
  cd FinanceApp.UITests && npm install
  ```

## Run

```bash
# 1. Build a standalone, debuggable APK (Fast Deployment APKs can't be installed by Appium)
dotnet build FinanceApp/FinanceApp.csproj -f net10.0-android -c Debug -p:EmbedAssembliesIntoApk=true -o artifacts/e2e-apk

# 2. Run the relevant classes (the Appium server is started and stopped automatically)
dotnet test FinanceApp.UITests/FinanceApp.UITests.csproj --logger "console;verbosity=normal" --filter "FullyQualifiedName~EditEntryTests"
```

The run shares one Appium session. Before each test the app data is wiped (`mobile: clearApp`) and the app relaunched, so every test starts from an empty database.
The APK is force-reinstalled once per run. That replaces any Fast Deployment install (`dotnet build -t:Run`).
You'll need to redeploy from the IDE afterwards.

| Env var | Default | Purpose |
|---|---|---|
| `E2E_APK_PATH` | `artifacts/e2e-apk/com.companyname.financeapp-Signed.apk` | APK under test |
| `E2E_DEVICE` | `emulator-5554` | adb serial of the target device |
| `E2E_APPIUM_URL` | *(local server started)* | Use an already running Appium server |
| `ANDROID_HOME` | VS / Android Studio default SDK path | Android SDK location (adb, UiAutomator2) |

When a wait times out, the page source and a screenshot are saved under `FinanceApp.UITests/TestResults/diagnostics/`.

### In CI

`.github/workflows/e2e.yml` builds the APK once, then runs [`run-e2e.sh`](run-e2e.sh) on emulators:

- **After each merge to `master`:** the whole suite, split into 3 parallel shards (`--shard 1 3`, …). Classes are dealt to shards by test count, so new classes are picked up without editing the workflow.
- **On pull requests:** only the smoke set (`--filter Smoke=true`), the critical journeys tagged `[Trait("Smoke", "true")]`.
- Failed tests are rerun once. A test that passes on the rerun is reported as a "Flaky E2E test" warning instead of failing the run; one that fails again fails it.

## What belongs here

The suite follows the test pyramid. E2E tests cover the critical user journeys and the wiring: the UI writes to the
database and the screen reacts. Logic (labels, formatting, totals, status and date rules, what a list shows) is tested
in the ViewModel and service tests, where it is fast and precise. In practice:

- 1–2 tests per feature, for its main flows, plus one per option that the UI maps to a different code path (e.g. each delete scope).
- Small checks that share a setup and look at different aspects of the same result go in one test, grouped with FluentAssertions' `AssertionScope` so one failed assert doesn't hide the others. Don't merge broadly: a long journey fails at its first flaky step and is harder to diagnose.
- A test costs ~13 s of reset before it does anything, and every entry created through the UI ~10 s more, so seed the starting data instead (below) and keep the UI for the behaviour under test.

## Writing tests

- Derive from `E2ETestBase` and add `[Collection(E2ECollection.Name)]` and `[Trait("Category", "E2E")]`. Add `[Trait("Smoke", "true")]` to a test only if it is one of the few journeys worth running on every PR.
- Seed the data the test starts from by passing it to the base constructor: `base(server, data => data.AddEntry("Aluguel E2E", 1500m))`. `SeedData` writes entries, payment sources and partial payments straight into the database before the app starts, which skips the setup wizard too. A seeded test starts faster than an empty one.
- Locate elements by `AutomationId` (it surfaces as the Android `resource-id`). Add one in XAML for every element a test touches.
- Interact through page objects (`Pages/`); keep raw Appium calls out of test methods. pt-BR texts the tests select by live in `AppLabels`.
- Compute expected texts with `AppFormat` (`Money`, `MonthTitle`, `EntryCount`) instead of hardcoding them.
- The month `CarouselView` keeps neighbour months in the tree, so `AppSession` helpers only consider *displayed* elements.
- Popup buttons can sit below the fold on small screens; tap them with `ScrollToAndTap`.
- Assert persisted state with `GetEntryRowsAsync` / `GetMonthlyEntryRowsAsync` / `GetCategoryRowsAsync`. The row types in `Infrastructure/TableRows.cs` mirror the tables on purpose and don't reference the app's models; `SeedData` writes plain SQL for the same reason.

## Coverage

| Class | Flows |
|---|---|
| `AddEntryTests` | One-time outcome (description focused on open, amount shown once a recurrence is picked, currency mask), recurrent income (from its start month on), installments (exactly N months, labels, total); "Cancelar", the header back button (visible after scrolling) and system back discard the entry; saving from another month starts the entry there |
| `EditEntryTests` | Pre-filled page with locked structural fields, description/amount change in every month; back button and system back discard changes |
| `DeleteEntryTests` | One-time; cancelled action sheet then *this month only*; *this and future*; *all months* |
| `EntryActionsMenuTests` | Long press reveals the menu; tapping outside or on the entry hides it without opening the editor |
| `EntryStatusTests` | "Pagar"/"Receber" mark the row and the summary's paid totals only that month; "Pendente" reverts them |
| `MonthNavigationTests` | Swipe and arrows change month and dismiss the hint for good; "Mês atual" pill; month picker (jump to another year with the ▾ badge inside the pill, cancel, "Mês atual", entries loaded after a far jump) |
| `MonthSummaryTests` | Totals page first; swipe between pages without changing month; dots; selected page kept across months |
| `PartialPaymentTests` | "Pagar parcial" (cancel, popup texts and progress, payments add up with their records, "Pagar" settles); "Valor já pago neste mês" (only that month, prefilled, clearing reverts to pending) |
| `PartialPaymentFlagTests` | Turning the flag on offers the action and the paid field; turning it off hides the action but keeps payments removable |
| `PaymentRecordsTests` | The month's payments listed in the entry page; removing one is undone by "Cancelar" and applied by "Salvar" |
| `OverBudgetTests` | Paying beyond the planned amount warns in the popup and strikes the planned amount, only that month |
| `EstimatedAmountTests` | Estimated (and partial payment) switches on a new entry, "±" badge in every month, turned off when editing |
| `DueDateTests` | Picking a due day on a new entry; changing it to today ("Vence hoje") and clearing it |
| `SectionToggleTests` | Collapsing a section hides only its rows, in every month |
| `AlternatingRowsTests` | Every other outcome row tinted red and income row tinted green (sampled from the rendered rows) |
| `CategoryTests` | Seeded defaults grouped by type; new category used on an entry; deleting a category used by an entry |
| `PaymentSourceTests` | Adding a voucher and a card (groups, balance labels); renaming a source used by an entry; changing an entry's source; deleting a source used by an entry |
| `CreditCardBillTests` | New card with bill day and paying account; buying with a card on the entry page (falls due with the bill, no estimated/partial switches); bill row (total, due date, account, collapse); "Pagar fatura"/"Pendente"; available limit freed by paying the bill; editing the bill's day and account |
| `AppLockTests` | Turning the PIN on (mismatched confirmation first); lock on relaunch (wrong then right PIN); "Esqueci o PIN" and Back keep it locked; turning it off; changing it |
| `SetupTests` | Welcome step and Back keeping what was typed; skipping (not shown again) then rerunning and closing; filling every step (summary, then sources and entries created); rerunning adds entries |
| `SettingsTests` | Sections and the name, shown in the menu greeting and kept after a restart |
