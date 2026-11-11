# Preparing the iOS version

Back to the [README](README.md) · [ROADMAP](ROADMAP.md) · [PUBLISHING](PUBLISHING.md).

What it takes to put Mensal on the App Store, as of November 2026. It covers the code, the machines and accounts,
the build and release pipeline, and the App Store steps. It is written for someone who has only shipped the
Android version, so Apple's terms are explained the first time they appear (and in the glossary at the end).
Nothing here is implemented yet. The work became roadmap tasks, listed in section 9.

Facts about Apple's rules, prices and versions change often. They were checked in November 2026 against the
sources at the end. Check them again before acting on them.

## 1. Summary

- **The code is mostly ready.** .NET MAUI builds the same app for iOS: the services, view models, XAML pages,
  SQLite database and tests are shared. `dotnet build -f net10.0-ios` already compiles on Windows with no errors
  or warnings. What's missing is the Android-only code (biometrics, hiding the app in the app switcher, the field
  underline, the swipe fix for the month summary), a few iOS settings files, and some iOS-specific behaviour (no
  back button, a numeric keyboard without a "Done" key, a PIN that survives uninstalling). Section 3 lists each
  one. **Rough size: one roadmap task of about the same size as "Security hardening".**
- **You need a Mac with Apple silicon (M1 or newer).** Apple only lets Xcode, its build tools, run on macOS, and
  Xcode 27 (required from April 2027) only runs on Apple silicon. There is no supported way to build, sign or run
  an iOS app from Windows alone. Section 2 compares buying a Mac, renting one in the cloud, and building on GitHub's
  Macs.
- **You need a paid Apple Developer Program membership: US$ 99 a year**, charged in reais. Google charged US$ 25
  once; Apple charges every year, and the app leaves the App Store if the membership lapses.
- **"iCloud account" is not needed as such.** Your iCloud account *is* your Apple Account (formerly "Apple ID").
  The Developer Program membership is attached to an Apple Account, which needs two-factor authentication.
  The app itself doesn't need iCloud: data stays on the phone, as on Android.
- **Publishing is simpler than on Google Play in one way:** there is no 12-tester, 14-day closed test. Each version
  goes through a human review instead (usually 1–2 days).
- **Two deadlines matter:** since April 28, 2026 uploads must be built with Xcode 26 or later; from April 2027 they
  must use the iOS 27 SDK (Xcode 27), which also *requires* the "scene" app lifecycle (section 3, item 1) or the
  app won't launch.

## 2. What you need: machines, accounts and costs

### 2.1 A Mac

Everything Apple-specific (compiling the native parts, signing, the iPhone Simulator, uploading to the App Store)
goes through Xcode, which only runs on macOS. On Windows, Visual Studio 2026 can only *remote-control* a Mac on your
network ("Pair to Mac"). Its Windows-only alternative, "Hot Restart" (deploying straight to a USB-connected iPhone),
was removed in Visual Studio 2026 and still exists only in Visual Studio 2022, for debugging, never for releases.

| Option | Good for | Drawbacks |
|---|---|---|
| **Buy a Mac mini (Apple silicon)** (recommended) | Everything: Simulator, debugging, Face ID tests, screenshots, releases. The cheapest new Mac. | Upfront cost. Needs a monitor/keyboard, or use it from Windows with Pair to Mac. |
| **A used Mac with M1 or newer** | Same as above, cheaper. | It must run the macOS version Xcode 27 requires. Avoid Intel Macs: Xcode 27 doesn't run on them. |
| **Rent a cloud Mac** (MacinCloud, AWS EC2 Mac, others) | Trying it out before buying; occasional releases. | Monthly or hourly cost (AWS bills a 24-hour minimum). Slow remote screen, so the Simulator is awkward. |
| **Only GitHub Actions Macs** | Building, signing and uploading to TestFlight from CI. Free on this public repository. | No screen, so no Simulator or debugging. Every test round-trips through TestFlight to a real iPhone. |

Recommendation: a Mac mini (new, or used M1/M2) for development, plus GitHub Actions for the release builds, as is
already done for Android. You can develop on the Mac itself with VS Code (C# Dev Kit and the .NET MAUI extension)
or JetBrains Rider, or keep Visual Studio on Windows and pair it to the Mac.

Setup on the Mac, once:

1. Install Xcode from the Mac App Store, the version the .NET iOS workload asks for. Each workload version pins one
   Xcode version and won't build with another: the .NET 10 iOS workload 27.0 needs Xcode 27.0. Its release notes
   list the version, and `dotnet build` says which one it expected when they don't match.
2. Open Xcode once to accept its license and install the iOS Simulator runtime.
3. Install the .NET 10 SDK and the workloads: `dotnet workload install maui-ios`.
4. Clone the repo and run `dotnet build -t:Run -f net10.0-ios` to start the app in the Simulator.

### 2.2 An iPhone (recommended, not required)

The Simulator runs the app on the Mac and covers most testing, including Face ID (Simulator menu Features > Face
ID). Before the first release, try the app on a real iPhone at least once, through TestFlight (section 6.4): the
keyboard, performance and the app switcher behave differently on a real device. Any iPhone that runs iOS 15 or
later works, since that's the app's minimum version (`SupportedOSPlatformVersion` in the `.csproj`).

### 2.3 The Apple Developer Program

- **What it is:** the paid membership that lets you sign apps, use TestFlight and publish on the App Store. A free
  Apple Account can only run apps on your own device for 7 days and can't publish.
- **Cost:** US$ 99 a year, charged in local currency (the real price in reais shows at checkout). It renews every
  year. If it lapses, the app is removed from the App Store, though people who installed it keep it.
- **Individual or organization:** enroll as an **individual**. An organization needs a legal entity (CNPJ) and a
  D-U-N-S number. As an individual, the App Store shows your legal name as the seller ("Henrique Gouvêa"), like the
  developer name on Google Play.
- **How:** the easiest way is the **Apple Developer app** on an iPhone or iPad. You sign in, verify your identity
  with a photo of an ID document and pay through the App Store. Enrolling on the website
  (developer.apple.com/programs/enroll) also works. Approval usually takes from a few hours to two days.
- **Before you enroll:** your Apple Account needs two-factor authentication and your real legal name, since the
  name on the account must match your ID.

### 2.4 Costs at a glance

| Item | Cost | When |
|---|---|---|
| Apple Developer Program | US$ 99/year (in reais) | Before the first TestFlight build |
| A Mac | One-off (new or used), or a monthly fee for a cloud Mac | Before section 3's work starts |
| An iPhone for testing | Optional; any iPhone with iOS 15+ | Before the first release |
| GitHub Actions macOS runners | Free (public repository, standard runners) | — |
| App Store commission | None: the app is free and sells nothing yet | Matters only for "Premium tier" |

## 3. Code changes

Each item says what is wrong or missing on iOS and what to do. Items 1–9 block a release. Items 10–14 are looks or
polish, but users would notice them.

| # | Area | Today (Android) | On iOS | To do |
|---|---|---|---|---|
| 1 | **App lifecycle (UIScene)** | — | Builds made with Xcode 27 / the iOS 27 SDK (required from April 2027) don't launch unless the app uses the "scene" lifecycle. | Add `Platforms/iOS/SceneDelegate.cs` (`[Register("SceneDelegate")] public class SceneDelegate : MauiUISceneDelegate { }`) and a `UIApplicationSceneManifest` to `Info.plist`, as in Microsoft's migration guide. Upgrade `Microsoft.Maui.Controls` from 10.0.41 to 10.0.110 or later, which the guide requires. Mac Catalyst would need the same change and a minimum version of 17.0; it isn't shipped, so drop the `net10.0-maccatalyst` target unless it's wanted. |
| 2 | **Biometrics** | `BiometricService` (AndroidX `BiometricPrompt`) | iOS gets `UnavailableBiometricService`, so "Desbloquear com biometria" never shows. | Add an iOS `BiometricService` using `LocalAuthentication` (`LAContext.EvaluatePolicy`, `DeviceOwnerAuthenticationWithBiometrics`), registered under `#if IOS` in `MauiProgram.cs`. Add `NSFaceIDUsageDescription` to `Info.plist` ("O Mensal usa o Face ID para desbloquear o app."): **without it, the app crashes** the first time it asks for Face ID. |
| 3 | **PIN survives uninstalling** | Uninstalling the app deletes its data, SecureStorage included. | SecureStorage uses the iOS **Keychain**, which is *not* deleted when the app is uninstalled. After a reinstall the database and preferences are empty but the old PIN is still there, so the app asks for a PIN the user may no longer know. Today, uninstalling is the only way out of a forgotten PIN. | On the first start after an install, when a "first run" preference is missing (preferences *are* deleted on uninstall), call `SecureStorage.Default.RemoveAll()`. Cover it with a service test (`AppLockService`). |
| 4 | **"Esqueci o PIN" text** | Tells the user to clear the app's data in Android's settings. | iOS has no "clear data" option. | Per platform text: "Apague o app e instale de novo" on iOS. It only works once item 3 is in. |
| 5 | **Back button** | `PinPage` and `SetupPage` override `OnBackButtonPressed` and send the app to the background (`MoveTaskToBack`). | iPhones have no back button, and an app must never close or hide itself (App Review rejects it). The modal pages are full screen, so they can't be swiped away either. | Nothing to port: the `#if ANDROID` blocks already skip it. Check in the Simulator that no edge swipe dismisses the PIN or the wizard. |
| 6 | **Hide in the app switcher** | `RecentsPrivacyService` hides the app's thumbnail in "Recentes" ("Ocultar conteúdo nos apps recentes"). | iOS gets `UnavailableRecentsPrivacyService`, so the app switcher shows the balances. | Add an iOS `RecentsPrivacyService`: when the app goes inactive, cover the window with a plum view and the logo, and remove it when the app becomes active (scene `WillResignActive` / `DidBecomeActive`). iOS has no `FLAG_SECURE`; screenshots can't be blocked and don't need to be. |
| 7 | **Info.plist** | `AndroidManifest.xml` | The template `Info.plist` lacks what the App Store and the app need. | Add `NSFaceIDUsageDescription` (item 2) and `NSPhotoLibraryUsageDescription` for the profile photo; Apple flags a missing usage text even though the photo picker doesn't strictly need it. Add `ITSAppUsesNonExemptEncryption` = `false`: the app has no encryption beyond hashing and HTTPS, so the export compliance question is skipped on every upload. Add `LSApplicationQueriesSchemes` with `mailto`, which `Launcher.TryOpenAsync` needs for "Fale com o desenvolvedor". Set `CFBundleDevelopmentRegion` = `pt-BR` and `CFBundleLocalizations` = `pt-BR`: iOS translates the system texts ("Cancel", "Done", the photo picker) from these, as `MainActivity.AttachBaseContext` does on Android. Allow **portrait only**, since the screens were designed for it, and decide on iPad (item 8). |
| 8 | **iPad** | — | The template lists both iPhone and iPad (`UIDeviceFamily` 1 and 2). An iPad app needs its own 13" screenshots and is reviewed on iPad, where the phone layout would look stretched. | Ship **iPhone only** (`UIDeviceFamily` = 1). iPads still run it, in a phone-sized window, with no extra screenshots. |
| 9 | **Privacy manifest** | — | `Platforms/iOS/Resources/PrivacyInfo.xcprivacy` must declare every "required reason" API the app uses, or the upload gets a warning and later a rejection. The app uses `Preferences` (`UserDefaults` on iOS), whose entry is commented out. | Uncomment the `NSPrivacyAccessedAPICategoryUserDefaults` entry (reason `CA92.1`). Also declare `NSPrivacyTracking` = false and an empty `NSPrivacyCollectedDataTypes`, matching "Data Not Collected" (section 6.2). |
| 10 | **Numeric keyboard without "Done"** | The numeric keyboard has a "Done/OK" key (the E2E tests use it). | Every `Keyboard="Numeric"` field (10 of them: amounts, the PIN setup, due days, installments) opens a keypad **with no "Done" key**, so the user can't close it, and fields below it stay hidden. | Add an `Entry` handler on iOS that gives these fields an input accessory bar with a "Concluído" button (`UIToolbar` on `InputAccessoryView`), next to `InputUnderline` in `MauiProgram.cs`. |
| 11 | **Field underline** | `InputUnderline` draws the lilac/plum underline under fields and pickers. | iOS shows the default rounded gray border, so the fields don't match the "Calendário" look. | iOS version of `InputUnderline`: `UITextField.BorderStyle = None` plus a bottom line that turns plum on focus. |
| 12 | **Summary swipe vs. month carousel** | `SwipePagerHandler` keeps the month carousel from taking the summary card's horizontal swipes. | Without it, swiping the summary on iOS may change month instead of the summary page. | Try it in the Simulator first. If the carousel wins, add an iOS handler whose pan gesture blocks the carousel's (a `UIGestureRecognizerDelegate`). |
| 13 | **Safe areas and status bar** | — | The plum top bar has to reach behind the clock and the notch / Dynamic Island, with white status bar text; the "+" button must stay above the home indicator. | Check every page in the Simulator on a recent iPhone and the smallest supported one (iPhone SE). Set the status bar style to light (`UIStatusBarStyle` in `Info.plist`, or the Toolkit's `StatusBarBehavior`). |
| 14 | **Look and feel** | Shadows, popups, pickers and the date picker were tuned on Android. | iOS draws native pickers (wheels), alerts and shadows its own way. `Shadow.Opacity` *does* work on iOS, but it's already 1 everywhere, so the brush's alpha gives the same result. | One pass over every screen in the Simulator. Compare with the Android screenshots in `fastlane/` and fix what looks broken; native iOS look is fine where it doesn't break the theme. |

Things that already work on iOS and need no change:

- **Database:** `DatabaseContext` puts SQLite in `FileSystem.AppDataDirectory`, which is the app's `Library` folder on
  iOS. sqlite-net-pcl ships the iOS native library.
- **Export, import, photo:** `Share` opens the iOS share sheet, where "Salvar em Arquivos" saves to iCloud Drive or
  the phone. `FilePicker` already maps the backup type for iOS (`public.json` in `DataManagementViewModel`).
  `MediaPicker.PickPhotosAsync` opens the iOS photo picker.
- **Links:** `Browser` opens the policy and terms in Safari's in-app view. `Launcher` opens Mail (item 7 adds the
  query scheme).
- **App icon and splash:** `MauiIcon` and `MauiSplashScreen` generate the iOS sizes. The icon has a solid
  background (`Color="#5B2A86"`), which the App Store requires (no transparency).
- **Phone backups:** iOS backs up the app's `Library` folder to iCloud or the computer by itself, and restores it on a
  new iPhone, like Android's backup. Keychain items saved by MAUI (accessible "after first unlock", not "this device
  only") move to a new iPhone with an encrypted backup or iCloud Keychain. On Android the PIN had to be left out of
  backups (`backup_rules.xml`). On iOS it restores together with the data, so no exclusion is needed. Check it once
  on a real device.

## 4. Tests

- **Unit tests** (`FinanceApp.Tests`) run on the `net10.0` target, so they cover iOS unchanged. New iOS logic
  (item 3's first-run cleanup) gets service tests as usual. Native code (Face ID, the app switcher cover, the
  keyboard bar) can't run in unit tests, same as the Android services.
- **E2E tests** (`FinanceApp.UITests`) are Android-only today. They use Appium's UiAutomator2 driver, Android
  selectors (`UiSelector`, `UiScrollable`) in `AppSession` and several page objects, `adb` to read the on-device
  database (`DeviceDatabase`) and to put files in Downloads (`DeviceFiles`), and Android's system file picker
  (`SystemFilePickerObject`). Appium's **XCUITest** driver runs on a Mac against the Simulator, and `AutomationId`
  becomes the iOS `accessibilityIdentifier`, so the tests' locators carry over. The rest would need an iOS version:
  scrolling, the database and file access (`xcrun simctl`), and the system dialogs.
- **Recommendation:** for the first iOS release, test manually from a short checklist: setup wizard, add, edit
  and delete an entry, partial payment, credit card bill, PIN and Face ID, export, import, profile photo, app
  switcher. Then port only the ~13 **smoke** tests, run on a `xcode-27` GitHub runner on pull requests. The Android
  suite keeps covering the shared logic and screens, which are the same code on both platforms.

## 5. Build and release pipeline

This mirrors `release.yml`, on a macOS runner:

1. **CI build check:** add a job to `main.yml` on the `xcode-27` runner label (Apple silicon, Xcode 27, generally
   available since October 2026; free on public repositories) that runs `dotnet workload install maui-ios` and
   `dotnet build FinanceApp/FinanceApp.csproj -f net10.0-ios`. It catches iOS-only build breaks on every PR.
2. **Signing.** Apple's equivalent of the Android upload key has three parts, all created at developer.apple.com
   (Certificates, Identifiers & Profiles):
   - an **App ID** (the bundle identifier, `io.github.henriquegouvea.mensal`, the same as on Android). Register it
     early: like Play's package id, it is final once the app is published;
   - an **Apple Distribution certificate**, exported as a `.p12` file with a password;
   - an **App Store provisioning profile** (`.mobileprovision`), which ties the App ID to the certificate.
   They go in GitHub secrets (base64, like `ANDROID_KEYSTORE_BASE64`). The workflow imports them into a temporary
   keychain on the runner. The certificate expires after a year and must be renewed. Renewing doesn't affect the
   published app.
3. **Build the IPA** (the iOS package, like the AAB):
   `dotnet publish FinanceApp/FinanceApp.csproj -f net10.0-ios -c Release -p:RuntimeIdentifier=ios-arm64 -p:ArchiveOnBuild=true -p:CodesignKey="Apple Distribution: …" -p:CodesignProvision="…"`.
   `ApplicationDisplayVersion` becomes the version users see (`CFBundleShortVersionString`), as on Android, and
   `ApplicationVersion` the build number (`CFBundleVersion`). The build number must grow with every upload of a
   version, so the run number works, as it does for `versionCode`.
4. **Upload to TestFlight** with an **App Store Connect API key** (Users and Access > Integrations: an issuer id, a
   key id and a `.p8` file, as GitHub secrets), using `xcrun altool --upload-app` or fastlane `pilot`. Like the Play
   service account in `release.yml`, it uploads only. The release to the App Store stays a manual click.
5. Suggested trigger: the same `v*` tags as Android, so one tag ships both. The steps would go in a new
   `RELEASING-IOS.md`, next to `RELEASING.md`.

## 6. App Store steps (the owner's)

### 6.1 Create the app in App Store Connect

App Store Connect (appstoreconnect.apple.com) is Apple's Play Console. Under "Apps" > "+": platform iOS, name
"Mensal" (names are unique across the store, so check early that it's free), primary language Portuguese
(Brazil), the bundle ID from section 5, and a SKU (any internal code, e.g. `mensal-ios`).

### 6.2 App information and privacy

- **Category:** Finance. **Price:** free. **Availability:** Brazil only, or every country, as on Play.
- **Privacy policy URL:** required. Use the existing page after section 7 updates it.
- **Support URL:** also required, and it must be a page with a way to reach you. The site has no such page yet.
  Add a `/suporte/` page to `docs/` with the contact e-mail.
- **App Privacy ("privacy nutrition label"):** Apple's Data safety form. Answer **"Data Not Collected"**: the app
  sends nothing anywhere. It has to match the privacy manifest (section 3, item 9) and the policy.
- **Age rating:** a questionnaire, like Play's IARC (Apple's ratings are 4+, 9+, 13+, 16+ and 18+). A finance app
  with no user content comes out 4+. Play's "18+ target audience" was a choice made for the Play form.
- **Export compliance:** answered by `ITSAppUsesNonExemptEncryption` (section 3, item 7).

### 6.3 Store listing

Apple's fields map onto the pt-BR texts in `fastlane/metadata/android/pt-BR/`:

| App Store | Limit | From Play |
|---|---|---|
| Name | 30 | `title.txt` |
| Subtitle | 30 | Shorter than Play's 80-character short description: "Todas as contas, um só rumo." fits |
| Promotional text (editable anytime) | 170 | — |
| Description | 4000 | `full_description.txt`, minus mentions of Android/Google |
| Keywords (comma-separated, not shown) | 100 | New |
| Screenshots | 3–10 | New: taken on a **6.9" iPhone** in the Simulator (e.g. 1320 × 2868), from the same HTML sources in `fastlane/` |

There is no feature graphic. fastlane `deliver` reads the same layout (`fastlane/metadata/pt-BR/…`), so the iOS
texts can live next to the Android ones.

### 6.4 TestFlight and App Review

- **TestFlight** is Apple's testing track. **Internal testers**: up to 100 people added in App Store Connect, no
  review, builds ready minutes after upload. **External testers**: up to 10,000, by e-mail or public link, after a
  light review of the first build. Testers install the free TestFlight app. There is no minimum number of testers or
  days, unlike Play's closed test for new accounts.
- **App Review:** every version, including updates, is reviewed by a person, usually within 24–48 hours. Common
  rejection causes for an app like this:
  - missing or broken links (privacy, support);
  - a usage description missing or too vague (Face ID, photos);
  - texts or screenshots that mention Android;
  - a crash on the reviewer's device.
  In "App Review Information", explain that the PIN is optional and that everything stays on the device. Leave the
  sign-in demo account empty: there is no login.
- After approval you release by hand, or have it go live automatically.

## 7. Policy, terms and texts to update

- `docs/privacidade.md` says "para Android, distribuído pelo Google Play", "seletor de fotos do Android",
  "Android Keystore", and has a "Backup do Android" section. Make it cover both platforms: the iOS Keychain, Face ID,
  iCloud / computer backups and the iOS share sheet. Update its date.
- `docs/termos.md` says "para Android" and "pelo Google Play". Add the App Store; any future purchase goes through
  either store.
- In the app: the "Esqueci o PIN" text (section 3, item 4). The code comments that mention Android are about
  workarounds and can stay.
- The policy's "Data Not Collected" stance makes the App Privacy answers simple. Keep the two in step when ads
  arrive.

## 8. Effects on later roadmap tasks

iOS changes four of the planned tasks. A short note was added to each of them in the roadmap:

- **Ads (AdMob):** on iOS, personalized ads need Apple's **App Tracking Transparency** prompt
  (`NSUserTrackingUsageDescription`) on top of Google's UMP consent form. `Info.plist` also needs the AdMob app id
  and the list of ad networks (`SKAdNetworkItems`), and the App Privacy answers stop being "Data Not Collected".
- **Premium tier and billing:** Apple requires its own in-app purchase system (**StoreKit**) for digital goods like
  "Remover anúncios", so it's a second integration next to Play Billing. It also needs the Paid Apps agreement and
  the bank and tax forms in App Store Connect. Apple's commission is 15% under the Small Business Program, and
  Apple's June 2026 changes in Brazil moved some of these terms, so check them at that point.
- **Sign in / login:** an app offering Google sign-in must also offer **Sign in with Apple** or a similar
  privacy-focused option (App Review Guideline 4.8). Apps with accounts must also let users **delete the account
  from inside the app** (Guideline 5.1.1(v)).
- **Due date notifications:** push notifications on iOS go through Apple's **APNs** (an APNs key in the developer
  account and the "Push Notifications" capability). Local reminders scheduled on the phone don't need it.

## 9. Recommended order and tasks created

1. **Owner:** enroll in the Apple Developer Program (section 2.3) and get a Mac (section 2.1). Register the bundle
   ID and check that the name "Mensal" is free in App Store Connect.
2. **iOS platform adaptation** (task): section 3, items 1–14, checked in the Simulator, plus the unit tests for
   item 3.
3. **iOS build and release pipeline** (task): section 5, plus `RELEASING-IOS.md` with the owner's signing and API
   key steps.
4. **iOS store presence** (task): section 6.2–6.3 (support page, App Store texts and screenshots in `fastlane/`)
   and section 7 (policy and terms for both platforms). Then the first TestFlight build, internal testing, and
   App Review.
5. **iOS E2E smoke tests** (task, after the first release): section 4.

Decisions for the owner:

- Which Mac option (section 2.1).
- iPhone only (recommended) or also iPad (section 3, item 8).
- Brazil only or every country (section 6.2).
- Whether to drop the unused Mac Catalyst and Windows targets from the project (section 3, item 1).

## 10. Glossary

- **Apple Account** (formerly Apple ID): your Apple login, the same one as iCloud.
- **Apple Developer Program:** the paid yearly membership needed to sign and publish apps.
- **App Store Connect:** the website where you create the app, fill in the listing, manage TestFlight and submit for
  review (Play Console).
- **Xcode:** Apple's development tools; .NET uses them behind the scenes to build and sign. Mac only.
- **Simulator:** an iPhone running inside the Mac, for testing (the Android emulator).
- **Bundle ID:** the app's unique id, `io.github.henriquegouvea.mensal` (Android's package id). Final once
  published.
- **Certificate:** proves that a build comes from you. **Provisioning profile:** says which app and certificate may
  be installed, and how (development or App Store). Together they replace Android's upload key.
- **Info.plist:** the app's settings file for iOS (`AndroidManifest.xml`). **Entitlements:** extra capabilities
  (Push, iCloud), needed only for later features.
- **Privacy manifest** (`PrivacyInfo.xcprivacy`): declares what data the app collects and why it uses certain
  system APIs.
- **IPA:** the iOS app package uploaded to Apple (the AAB).
- **TestFlight:** Apple's beta testing service and app (Play's testing tracks).
- **App Review:** Apple's human review of every version before it reaches the store.
- **Keychain:** iOS's secure storage, where MAUI's `SecureStorage` keeps the PIN (Android Keystore).

## Sources

- Microsoft: [Migrate .NET MAUI apps to the UIKit scene lifecycle](https://devblogs.microsoft.com/dotnet/migrate-dotnet-maui-apps-uikit-scene-lifecycle/)
  (Oct 2026), [Xcode requirement per workload](https://learn.microsoft.com/en-us/dotnet/ios/troubleshooting/xcode-requirement),
  [.NET for iOS release notes](https://github.com/dotnet/macios/releases),
  [Hot Restart (not in Visual Studio 2026)](https://learn.microsoft.com/en-us/dotnet/maui/ios/hot-restart),
  [Pair to Mac](https://learn.microsoft.com/en-us/dotnet/maui/ios/pair-to-mac)
- Apple: [SDK minimum requirements](https://developer.apple.com/news/upcoming-requirements/),
  [UIScene lifecycle required in iOS 27](https://developer.apple.com/forums/thread/813300),
  [Transitioning to the scene-based life cycle](https://developer.apple.com/documentation/uikit/transitioning-to-the-uikit-scene-based-life-cycle),
  [Changes to iOS in Brazil](https://developer.apple.com/news/?id=dhwadr2x),
  [Fee waivers (pt-BR)](https://developer.apple.com/br/help/account/membership/fee-waivers/),
  [Keychain items survive uninstalling (Apple forums)](https://developer.apple.com/forums/thread/36442)
- .NET MAUI docs: [SecureStorage keeps values after uninstalling on iOS](https://github.com/dotnet/docs-maui/issues/3257)
- GitHub: [Xcode 27 runner image generally available](https://github.blog/changelog/2026-10-15-xcode-27-runner-image-generally-available)
- Enrollment walkthrough (pt-BR): [Inscrição no Apple Developer Program](https://www.tabnews.com.br/adevelop/inscricao-no-apple-developer-program-o-que-a-apple-realmente-exige-e-onde-as-pessoas-travam)
- Screenshot sizes: [App Store screenshot sizes 2026](https://appscreenshotstudio.com/blog/iphone-app-store-screenshot-sizes-2026-required-vs-optional)
  (third-party; check Apple's "Screenshot specifications" in App Store Connect Help)
