# Publish preparation

Back to the [README](README.md) · [ROADMAP](ROADMAP.md).

What stands between Mensal and its first Google Play release, as of November 2026: a review of the code
(security, data protection, performance), the store and release requirements, the Brazilian laws that apply,
and a plan for ads and a paid tier. Items fixed in the same pull request are marked **Fixed**. The rest became
roadmap tasks (listed at the end) or are decisions for the owner.

This is not legal or tax advice. The legal and tax sections list what to check, and with whom.

## 1. Verdict

The app is in good shape for a first release: everything stays on the device, there is no account and no
network traffic, the PIN is stored properly, and the code has good test coverage. Five things block publishing:

1. **A debug "Nuke DB" button was on the home screen of every build.** One tap deleted every entry with no
   confirmation. **Fixed** (removed).
2. **The package id is still `com.companyname.financeapp`.** It can never change after the first upload.
3. **There is no release pipeline.** No signing key, the version is hard-coded, and nothing builds an AAB.
4. **There is no privacy policy, store listing or "Sobre" screen.** Play requires the policy for every app.
5. **A new personal Play developer account must run a closed test** with at least 12 testers for 14 days in a
   row before it can publish to production. This takes the longest, so start it as soon as an AAB exists.

Ads and a premium tier don't need to be in the first release (see section 8).

## 2. Fixed in this pull request

| Area | Change |
|---|---|
| Data loss | Removed the "Nuke DB" button from "Início" (and `MainViewModel.ClearDatabaseCommand`). A real "Apagar todos os dados", with confirmation, is a new task. |
| Backup | Android backup and device-to-device transfer now skip the SecureStorage file (`Platforms/Android/Resources/xml/backup_rules.xml`, `data_extraction_rules.xml`). Its Keystore key never leaves the phone, so a restored copy couldn't be decrypted. The database, profile and settings are still backed up, so a new phone gets the user's data back (without the PIN, which they set again). |
| Performance | Loading a month used to add its entries one at a time, and every add rebuilt the outcome list, the income list and the credit card bills: 30 entries meant ~30 rebuilds of each list. `MonthViewModel.ReplaceEntries` swaps them in and notifies once. |
| Cleanup | Removed the unused `Microsoft.Maui.Controls.Compatibility` package, the template's `dotnet_bot.png` and a stale `AddEntryPopup.xaml` entry in the project file. |
| CI | The E2E failure after #26 (see section 10). |

## 3. Security

| # | Severity | Finding | Recommendation |
|---|---|---|---|
| S1 | Critical | "Nuke DB" button wiped all entries in one tap. | **Fixed.** |
| S2 | High | **Vulnerable native SQLite.** `sqlite-net-pcl` 1.9.172 pulls `SQLitePCLRaw.lib.e_sqlite3` 2.1.2, flagged by `dotnet list package --vulnerable` ([GHSA-2m69-gcr7-jv3q](https://github.com/advisories/GHSA-2m69-gcr7-jv3q), CVE-2025-6965, SQLite memory corruption). No 2.1.x fix exists. | **Fixed in "Security hardening".** Upgrade to `sqlite-net-pcl` 1.11.285 (SQLitePCLRaw 3.0.3 + SQLite 3.53.3) in the app, unit test and E2E projects. Exploiting it needs crafted SQL, and the app only runs its own queries, so the real risk is low. Still, it's a red flag in any audit and in Play's SDK reports. |
| S3 | High | **The PIN can be guessed without limit.** 4 digits = 10,000 combinations, and wrong attempts aren't counted or delayed. Someone holding the unlocked phone can try every PIN in an afternoon. | **Fixed in "Security hardening".** Count failed attempts in preferences (so killing the app doesn't reset them). After 5 failures, wait 30 s, and double the wait after each further failure. Show "Muitas tentativas. Tente de novo em 30 s". Optionally allow a 6-digit PIN. |
| S4 | Medium | **Balances show up in the recent apps screen.** The app locks only after 1 minute in the background, and the recent apps thumbnail shows the last screen, balances included, to anyone who opens it. | **Fixed in "Security hardening".** When a PIN is on, set `FLAG_SECURE` on the activity, or show a plain plum cover while the app is in the background. `FLAG_SECURE` also blocks screenshots, which some users want, so make it a switch in "Segurança" ("Ocultar conteúdo nos apps recentes"). It must stay off in E2E runs, which take screenshots (`AlternatingRowsTests`, timeout diagnostics). |
| S5 | Medium | **Failed writes are silent.** `FinanceService` catches every exception when saving, deleting or paying, and only writes it to `Console`. If a write fails, the user thinks it worked. | **Fixed in "Security hardening".** Let the services throw. Catch at one place (a base command or a global handler) that shows "Não foi possível salvar. Tente de novo." and logs it. |
| S6 | Medium | **Multi-row writes have no transaction.** `DeleteEntryAsync` (template + monthly rows + payments), `SetCreditCardBillStatusAsync` (every purchase on the bill) and the setup wizard write row by row. If the app is killed halfway, half the change stays. | **Fixed in "Security hardening".** Wrap each one in `RunInTransactionAsync`, as `CategoryService` and `PaymentSourceService` already do. |
| S7 | Low | **The database isn't encrypted.** Android's file-based encryption protects it while the phone is locked, and other apps can't read it. A rooted phone or a forensic copy of an unlocked phone can. | Keep it as is for now. Encrypting with SQLCipher needs a key in SecureStorage, and that key isn't backed up, so a backup restored on a new phone couldn't be opened. Users would lose their data when they change phones, and without an account there is no other way back. Revisit once cloud sync (Phase 3) exists. |
| S8 | Low | **The `INTERNET` permission is unused.** The app makes no network call. | If v1.0 ships without ads, remove it from Release builds. The Play listing then shows no network access, and "Funciona 100% offline: seus dados nunca saem do seu celular" becomes a selling point. Debug builds need it for deployment. |
| S9 | Low | Biometrics use `BIOMETRIC_WEAK` and only open the UI, without unlocking a key. | Fine for an app lock (most phones' face unlock is "weak"). Document it, and don't reuse it to protect a cloud token later: that needs `BIOMETRIC_STRONG` with a `CryptoObject`. |
| S10 | Low | Error messages go to logcat in Release (`Console.WriteLine`). | **Fixed in "Security hardening".** Covered by S5: use `ILogger` and add no provider in Release. |

Already in good shape: the PIN is hashed with PBKDF2-SHA256 (100,000 iterations, random salt) and compared in
constant time. It lives in SecureStorage, so it's encrypted with a Keystore key. "PIN on" depends on the hash
alone, so a restore can't lock the user out. `Microsoft.Extensions.Logging.Debug` is only added in Debug.
Release builds aren't debuggable. No secret is kept in the repository.

## 4. Data protection

Today the developer never receives any user data. Entries, accounts, the name and the photo stay in the app's
private storage, and the only copy that leaves the phone is the user's own Android backup, which goes to their
Google account (end-to-end encrypted with the screen lock on Android 9+). That changes with each item below. The
privacy policy and the Data safety form must be updated every time.

| Feature | Data the developer starts processing | Needed |
|---|---|---|
| v1.0, offline | None | Privacy policy saying exactly that. Data safety: "No data collected", "No data shared". |
| Ads (AdMob) | Advertising ID, IP, device data (processed by Google) | Policy update, Data safety update, ads declaration, consent (section 7), `AD_ID` permission (added by the SDK). |
| Crash reporting (Sentry, Firebase) | Crash traces, device model | Not needed for v1.0: Play Console's Android vitals already shows crashes and ANRs, without an SDK. |
| Account and sync (Phase 3) | Everything: email, all financial data | Full LGPD program (section 6), account deletion in the app and on the web (a Play requirement for apps with accounts), access logs (Marco Civil). |

Two features were missing for users' control over their own data. They are good practice, and the second
is a real need, because there is no account to recover data from. Both are done ("Data management" in the
roadmap, under "Configurações › Seus dados"). The exported file stays on the user's side: the share sheet sends
it wherever they choose, and the developer still receives nothing.

- **"Apagar todos os dados"** in "Configurações", with a confirmation that names what will be deleted (entries,
  accounts and cards, categories, profile, PIN). It replaces the removed debug button. Uninstalling works too,
  but users expect a button.
- **"Exportar dados" / "Importar backup"**: a file (JSON, plus CSV for spreadsheets) shared with the Android
  share sheet. It covers switching phones without a Google backup, and makes data portability (LGPD art. 18, V)
  easy.

## 5. Performance

The app is small and works with a few hundred rows, so there's no urgent problem left after the fix in section 2.
What to do before release, and what to watch:

| # | Finding | Recommendation |
|---|---|---|
| P1 | Rows were rebuilt once per entry when a month loaded. | **Fixed.** |
| P2 | Each month load reads every template, every category and every payment source again, and 5 months load at start (20 queries). | **Fixed.** `CategoryService` and `PaymentSourceService` cache their lists, and `FinanceService` reads them from there. Their own writes clear the cache, and the setup wizard, backup import and wipe call `ClearCache()`. The change messages aren't used for this: their recipients' order isn't guaranteed, so a view could reload before the cache is cleared. |
| P3 | `FinancialMonthlyEntry` has no index on `(Year, Month)`: every month load scans the table. Each recurrent entry adds 12 rows a year once acted on. | **Fixed.** `DatabaseContext.CreateIndexesAsync` adds `(Year, Month)` and `FinancialEntryId` on `FinancialMonthlyEntry`, and `FinancialMonthlyEntryId` on `FinancialPayment`. |
| P4 | `GetUsedCreditLimitsAsync` (run each time the menu opens) reads every monthly row ever saved. | **Fixed.** It reads only the monthly rows of outcomes paid with a credit card (one join), and nothing when there are no cards. Past months are still read: a paid past month frees the limit. |
| P5 | Cold start time has never been measured in a Release build. | **Measured** on the Galaxy A16 emulator (x86_64), `adb shell am start -W`, 5 cold starts each, median: **3.9 s with profiled AOT** (the Release default) and **3.2 s with full AOT** (`AndroidEnableProfiledAot=false`). Both miss the 2 s target. The emulator is slower than a phone, so measure on a real low-end arm64 phone once a signed build reaches internal testing, before choosing: full AOT saves ~0.7 s but adds ~11 MB. If the phone is also slow, look at what the first screen builds (the month carousel's ±24 months). |
| P6 | AAB size unknown. | **Measured:** the Release AAB is 31.9 MB (profiled AOT, all ABIs) and the universal APK 32.6 MB; with full AOT the APK is 43.5 MB. Play serves each phone only its ABI, so the download is smaller: check the per-device size in Play Console after the first internal upload. |
| P7 | Each section's `CollectionView` sits inside the month's `ScrollView`, so it never recycles rows: every row of the month is created at once. | Fine for typical months (under 100 rows). Revisit if users report slow months. |

## 6. Google Play requirements and release engineering

**Before the first upload:**

- **Package id.** Choose the final one (e.g. `br.com.<your-domain>.mensal`, or `io.github.henriquegouvea.mensal`
  if you have no domain). It can't change afterwards. Update `ApplicationId`, `E2ESettings.AppPackage`, the two
  backup rule files (the SecureStorage file name contains the id) and the CI scripts. Changing it installs a
  separate app, so existing test installs lose their data (no store users yet, so no harm).
- **Signing.** Create an upload key (`keytool`), keep it outside the repo (password manager + GitHub secrets),
  and enrol in Play App Signing (mandatory for new apps). Losing the upload key can be fixed with Google's
  support; losing an app signing key can't, which is why Google holds it.
- **Versioning.** `ApplicationVersion` (the Android `versionCode`) must grow with every upload: set it from the
  CI run number. `ApplicationDisplayVersion` follows `1.0.0`.
- **Release workflow.** A GitHub Actions workflow, run on a `v*` tag, that builds a signed AAB
  (`dotnet publish -f net10.0-android -c Release`) and uploads it to the internal testing track
  (Play Developer API, e.g. with `fastlane supply`).
- **Target API.** Currently 36 (the .NET 10 default), which meets Play's requirement. Each August, Play requires
  the previous year's Android version. Min SDK 21 is fine.

**Play Console, "App content":** privacy policy URL, Data safety form, ads declaration, content rating (IARC
questionnaire), target audience (18+, see ECA Digital below), and the financial features declaration (Mensal is
"personal finance / budgeting": it doesn't lend, pay or invest, so no license is needed).

**Store listing (pt-BR):** name "Mensal: controle de contas" (max 30 characters), short description (80),
full description (4,000), 512×512 icon, 1024×500 feature graphic, at least 2 phone screenshots (4–8 is better:
home, credit card bill, setup wizard, PIN), category "Finanças", contact email.

**Testing track:** personal developer accounts created after November 2023 must run a **closed test with at least
12 testers opted in for 14 days in a row** before applying for production. Recruit friends and family early,
and ask for real use (Play asks how the test went when you apply).

**In the app:** a "Sobre o Mensal" page in "Configurações" with the version, links to the privacy policy and
terms of use, a contact email, and open source notices (the Open Sans font is under the SIL Open Font License,
which asks for its notice; MAUI, CommunityToolkit and sqlite-net are MIT).

**Privacy policy hosting:** a page on GitHub Pages is enough (free, stable URL). It must be reachable without
login, in Portuguese, and name the developer and a contact.

## 7. Brazilian laws

| Law | Applies when | What it asks |
|---|---|---|
| **LGPD** (Lei 13.709/2018) | Personal data processed by the developer: from ads on, fully with accounts. | Legal basis for each use (consent for personalized ads), a privacy policy in plain Portuguese, data subject rights (access, correction, deletion, portability, art. 18), security, and breach reporting to the ANPD. A single developer or small company counts as a *small-scale processing agent* (Resolução CD/ANPD nº 2/2022): simpler records, no DPO required (a contact channel is still required). Financial data isn't "sensitive" under art. 5, II, but treat it as if it were. |
| **Marco Civil da Internet** (Lei 12.965/2014) | Once there's a backend run by a company. | Keep access logs (IP, date and time) for 6 months, secured (art. 15). Users' privacy and consent for data use (art. 7). |
| **CDC** (Lei 8.078/1990) and **Decreto 7.962/2013** | Once anything is sold (premium). | Clear price, what's included and renewal terms before purchase; easy cancellation; 7-day right of withdrawal for online purchases (art. 49). Google Play handles refunds, but the app's own terms shouldn't contradict this. |
| **ECA Digital** (Lei 15.211/2025, in force since March 2026) | Digital products aimed at, or likely to be used by, children and teenagers. | Mensal is for adults. Declare the target audience as 18+ in Play, say so in the terms, and never serve profiled ads that could reach minors. Ask a lawyer whether that's enough. |
| **Taxes** | Once money comes in (AdMob or Play pays from abroad). | As an individual, income from abroad goes through the monthly *carnê-leão*. With volume, a company (ME under Simples Nacional) is usually cheaper. Software publishing activities aren't allowed for MEI. Ask an accountant before the first payout. A Play organization account also needs a D-U-N-S number. |
| **Trademark** (INPI) | Before investing in the brand. | "Mensal" is a common word that describes a monthly finance app, so INPI may refuse it as a word-only mark. A mixed mark (logo + name) is more likely to be registered. Search [INPI's database](https://busca.inpi.gov.br) in classes 9 (software) and 36 (financial affairs) first. Fees are lower for individuals and MEI. |

Open Finance (Phase 4, bank integrations): reading bank data requires being a participant authorized by the
Banco Central, or going through a regulated aggregator (Pluggy, Belvo and others). Banco Inter's API for its own
account holders is a narrower option. Plan this before the Phase 4 tasks.

## 8. Ads (free version)

**AdSense is for websites. Mobile apps use AdMob.** Same Google account, different product.

**Integration:** there is no official AdMob SDK for MAUI. The options are a community plugin (such as
`Plugin.MauiMTAdmob`) or a binding of the Google Mobile Ads SDK for Android. Check that the chosen one supports
.NET 10 and is still maintained before starting.

**Where:**

- Never on the PIN screen, the setup wizard, the entry page or popups. Ads next to buttons that change money
  cause accidental taps, which AdMob penalizes, and ruin trust.
- **Best spot:** an adaptive banner at the end of "Início", after the lists (where the 96 dp spacer for the "+" is
  now), so it never covers an entry. A native ad styled like a card is the next step.
- Lower traffic, less intrusive: a banner at the bottom of "Categorias", "Contas e cartões" and "Configurações".
- **No interstitials.** People open a finance app several times a day for one tap ("Pagar"). Full-screen ads
  there lead to 1-star reviews.

**Consent:** Google requires a certified consent tool (its UMP SDK) only in Europe, the UK and Switzerland. In
Brazil, LGPD needs a legal basis for personalized ads, usually consent. The simplest safe setup: show the UMP
form, and serve non-personalized ads to whoever declines. Add "Privacidade e anúncios" to "Configurações" so the
choice can be changed.

**What to expect:** revenue = daily active users × ads seen per day × eCPM ÷ 1,000. Brazilian eCPMs are much lower
than US ones. With 1,000 daily users, 3 banner views each and an illustrative US$0.30 eCPM, that's about
US$0.90 a day, ~US$27 a month (AdMob pays from US$100). **Ads won't pay the bills at this scale.** Their role is
to fund the free tier a little and make "remover anúncios" worth buying.

## 9. Free and premium tiers

Rule of thumb: **keep the free tier fully useful for monthly control**, and charge for what costs money to run
(servers, bank APIs) or what power users want. Limiting the core (e.g. "only 20 entries") makes people leave
before they get attached, and gets the app bad reviews in a crowded category.

| Feature | Free | Premium ("Mensal+") |
|---|---|---|
| Entries, every recurrence (incl. yearly), partial payments, estimated amounts | ✔ | ✔ |
| Categories, accounts, cards, vouchers, credit card bill | ✔ | ✔ |
| PIN and biometrics, setup wizard | ✔ | ✔ |
| Export / import a local backup file | ✔ | ✔ |
| No ads | | ✔ |
| Advanced reports (charts by category and period, Phase 3) | Current month only | ✔ every period |
| Attachments (proof of payment, Phase 3) | | ✔ |
| Due date reminders (Phase 3) | Push | Push + email |
| Cloud backup, sync between devices, web app (Phase 3) | | ✔ |
| Bank integration (Phase 4) | | ✔ |

**Price (hypothesis to validate):** R$ 9,90/month or R$ 79,90/year (about 2 months free), with a 7-day trial.
Check what Mobills, Organizze and similar apps charge in Play before launching. Play's fee on subscriptions
is 15%.

**Billing:** digital features sold in a Play app must use Google Play Billing. For MAUI, look at
`Plugin.InAppBilling` (check .NET 10 support) or a binding of the Play Billing Library. Purchases must be checked
(ideally by the backend, once it exists) and restored on a new phone.

**When:**

1. **v1.0:** free, no ads, offline (and without `INTERNET` if S8 is done). Fastest Play review, simplest privacy
   policy, and the "your data never leaves your phone" message. Use Play Console's retention numbers and the
   reviews to decide what comes next.
2. **v1.x:** ads (section 8) plus a one-time "Remover anúncios" purchase. This is the first billing integration,
   without subscriptions yet.
3. **Phase 3:** a subscription once there's something with a running cost (sync, web app) or clear extra value
   (reports, attachments, reminders).

## 10. E2E failure after merging #26

`E2E (Android)` failed on master after #26 (run 42398838136, shard 3). One test failed, and failed again on the
rerun, so it wasn't flaky: `CategoryTests.OpenCategories_FreshInstall_ShowsSeededDefaultsGroupedByType`.

**Cause:** #26 added the "Saúde" and "Transporte" default categories, so "Categorias" now lists 8 categories. On
CI's 320×640 emulator the last one ("Salário", the incomes are sorted by name) sits below the fold, and the test
read only the names on screen. The PR's smoke set doesn't include this test, and the local emulator's screen is
big enough to show all 8, so it only failed in the full post-merge run.

**Fix:** `CategoriesPageObject.AllCategoryNames()` scrolls through the list and collects every name
(`AppSession.TextsWhileScrollingDown`). The test checks the "Despesas" group at the top before scrolling and the
"Receitas" group at the bottom after.

## 11. Decisions for the owner

1. **Package id** (section 6). Blocks the release pipeline task.
2. **v1.0 with or without ads** (recommended: without, section 9). Decides whether `INTERNET` goes (S8).
3. **Individual or company** for the Play account and the income (section 7, ask an accountant).
4. **Public repository?** It's public with no license, which means "all rights reserved", but anyone can read
   and copy the code, including how premium is unlocked. Making it private costs GitHub Actions minutes (free
   on public repos; 2,000 a month on a free private plan, Windows minutes count double). Alternatively, keep it
   public and add a license that allows reading but not reuse.
5. **Trademark:** file "Mensal" (mixed mark), or look for a more distinctive name before the store listing
   makes it public.

## 12. Tasks created

In [ROADMAP.md](ROADMAP.md), Phase 2, after "Publish preparation":

1. **Security hardening:** S2 (SQLite upgrade), S3 (PIN attempt limit), S4 (hide in recent apps), S5 (error
   handling), S6 (transactions).
2. **Data management:** "Apagar todos os dados", "Exportar dados" / "Importar backup".
3. **Performance pass:** P2–P6.
4. **Release pipeline:** package id, signing, versioning, release workflow, AAB to internal testing.
5. **Store presence:** privacy policy and terms (GitHub Pages), "Sobre o Mensal", store listing, Play Console
   forms, closed test with 12 testers.
6. **Ads (AdMob):** after decision 2.
7. **Premium tier and billing:** after v1.0 feedback.

Not code, so kept here as a checklist: accountant (taxes, company or not), lawyer review of the privacy policy
and terms, INPI search and filing, Play developer account (US$25, identity verification).
