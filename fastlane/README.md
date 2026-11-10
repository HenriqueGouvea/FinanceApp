# Store listing (Google Play, pt-BR)

`metadata/android/pt-BR/` holds Mensal's Play Store listing in the layout `fastlane supply` reads. For now it is
uploaded by hand in Play Console (see [RELEASING.md](../RELEASING.md), "Store listing and App content").

| File | Play field | Limit |
|---|---|---|
| `title.txt` | App name | 30 characters |
| `short_description.txt` | Short description | 80 characters |
| `full_description.txt` | Full description | 4,000 characters |
| `images/icon.png` | App icon | 512×512 PNG |
| `images/featureGraphic.png` | Feature graphic | 1024×500 PNG |
| `images/phoneScreenshots/*.png` | Phone screenshots, in file name order | 2–8; the long side at most twice the short one |

Keep the texts true to the app: a feature named there must exist. Update them, and the screenshots, when a release
changes what the app looks like.

## Graphics

`graphics/icon.html` and `graphics/feature-graphic.html` are the sources of the icon and the feature graphic: the
brand mark from `FinanceApp/Resources/AppIcon/appiconfg.svg` and the palette from `Colors.xaml`. Render them with
Edge (or Chrome) headless, from this folder:

```sh
EDGE="/c/Program Files (x86)/Microsoft/Edge/Application/msedge.exe"
OUT="$(cygpath -w "$PWD/metadata/android/pt-BR/images")"
"$EDGE" --headless=new --hide-scrollbars --allow-file-access-from-files --window-size=512,512 \
  --screenshot="$OUT\\icon.png" "file:///$(cygpath -w "$PWD/graphics/icon.html")"
"$EDGE" --headless=new --hide-scrollbars --allow-file-access-from-files --window-size=1024,500 \
  --screenshot="$OUT\\featureGraphic.png" "file:///$(cygpath -w "$PWD/graphics/feature-graphic.html")"
```

## Screenshots

Taken on the Galaxy A16 emulator with sample data, at **1080×2160** (`adb shell wm size 1080x2160`; the native
1080×2340 is longer than Play's 2:1 limit) and with the status bar in demo mode
(`adb shell settings put global sysui_demo_allowed 1`, then
`adb shell am broadcast -a com.android.systemui.demo -e command enter`, `clock -e hhmm 0941`,
`network -e mobile hide -e wifi show -e level 4 -e fully true`, `notifications -e visible false`). Restore with
`adb shell wm size reset` and `-e command exit`.

1. `1_inicio`: "Início" with paid, estimated (±) and partially paid expenses.
2. `2_fatura_cartao`: the credit card bill with its purchases, and the incomes.
3. `3_contas_e_cartoes`: accounts, the credit card's available limit and a meal voucher.
4. `4_guia_inicial`: the setup wizard's house expenses step.
5. `5_pin`: the PIN lock.
