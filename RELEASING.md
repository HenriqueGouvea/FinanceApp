# Releasing Mensal on Google Play

The package id is **`io.github.henriquegouvea.mensal`**. It is final: Google Play never lets an app change it
after the first upload. It is set in `FinanceApp/FinanceApp.csproj` (`ApplicationId`), and repeated in the two
backup rule files (`Platforms/Android/Resources/xml/`, the SecureStorage file name carries it),
`FinanceApp.UITests/Infrastructure/E2ESettings.cs` and the workflows (`e2e.yml`, `release.yml`).

The pipeline is `.github/workflows/release.yml`:

| Trigger | What it does |
|---|---|
| Push a tag `v1.2.3` | Builds a signed AAB (`versionName` 1.2.3) and uploads it to the **internal testing** track. |
| "Run workflow" by hand (Actions tab) | Builds the signed AAB only, kept as the `mensal-aab-<code>` artifact for 30 days. |

The `versionCode` is the workflow's run number (plus the optional `VERSION_CODE_OFFSET` variable), so it grows
with every run, as Play requires. Release builds use profiled AOT (the default; see PUBLISHING.md P5).

## One-time setup

### 1. Create the upload key

On your own computer, outside the repository (Java's `keytool` comes with the JDK):

```sh
keytool -genkeypair -v -keystore mensal-upload.keystore -alias upload -keyalg RSA -keysize 2048 -validity 10000
```

Pick a strong password and store the keystore file and the password in a password manager. **Never commit
the keystore.** If it is lost or leaked, Google support can reset the upload key (Play App Signing, step 3), but
it takes days.

### 2. Add the GitHub secrets

Repository → Settings → Secrets and variables → Actions → "New repository secret", or with `gh`:

```sh
base64 -w0 mensal-upload.keystore | gh secret set ANDROID_KEYSTORE_BASE64   # PowerShell: [Convert]::ToBase64String([IO.File]::ReadAllBytes("mensal-upload.keystore")) | gh secret set ANDROID_KEYSTORE_BASE64
gh secret set ANDROID_KEYSTORE_PASSWORD
gh secret set ANDROID_KEY_ALIAS          # upload
gh secret set ANDROID_KEY_PASSWORD       # same as the keystore password unless you chose another
```

`gh secret set NAME` without a value prompts for it, so the password doesn't land in your shell history.

### 3. Create the app in Play Console and enrol in Play App Signing

1. Create the Play developer account (US$ 25, identity verification) and the app "Mensal" (pt-BR, app, free).
2. Run the workflow by hand, download the `mensal-aab-<code>` artifact and upload the AAB to **Internal testing**
   in Play Console. Play's API can't upload a new app's first build, so this one is manual.
3. Accept **Play App Signing** when asked (mandatory for new apps): Google keeps the app signing key, and your
   key from step 1 becomes the upload key.

### 4. Let the workflow upload to Play

1. Google Cloud Console → create a project (or reuse one) → enable the **Google Play Android Developer API**.
2. Create a **service account**, then a JSON key for it.
3. Play Console → Users and permissions → invite the service account's email with "Release to testing tracks"
   for Mensal.
4. Save the JSON key as the `PLAY_SERVICE_ACCOUNT_JSON` secret: `gh secret set PLAY_SERVICE_ACCOUNT_JSON < key.json`,
   then delete the local file.

## Releasing a version

1. Bump `ApplicationDisplayVersion` in the csproj if you want local builds to show it (the tag wins in CI).
2. On an up-to-date `master`: `git tag v1.0.0 && git push origin v1.0.0`.
3. Watch "Release (Android)" in the Actions tab.
4. Until the app passes its first review, Play only accepts **draft** releases, so the upload lands as a draft
   in Internal testing: open it in Play Console and roll it out. After the first review, set the repository
   variable `PLAY_RELEASE_STATUS` to `completed` (Settings → Secrets and variables → Actions → Variables) and
   uploads go straight to the internal testers.

## Variables and secrets

| Name | Kind | Purpose |
|---|---|---|
| `ANDROID_KEYSTORE_BASE64` | secret | The upload keystore, base64 |
| `ANDROID_KEYSTORE_PASSWORD` | secret | Keystore password |
| `ANDROID_KEY_ALIAS` | secret | Key alias (`upload`) |
| `ANDROID_KEY_PASSWORD` | secret | Key password |
| `PLAY_SERVICE_ACCOUNT_JSON` | secret | Service account JSON key for the Play Developer API (tags only) |
| `PLAY_RELEASE_STATUS` | variable | `draft` (default) until the first review, then `completed` |
| `VERSION_CODE_OFFSET` | variable | Added to the run number, if the workflow is ever renamed and its count restarts |
