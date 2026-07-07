# Огляд SDK

## Що це

**Bidscube Unity SDK** (`com.bidscube.sdk`) — UPM-пакет для показу реклами в Unity-іграх на **iOS** та **Android**:

- **Banner / Image** — HTML через in-app WebView
- **Video** — VAST + Unity `VideoPlayer` (interstitial і rewarded)
- **Native** — OpenRTB native markup (WebView або Unity UI)

SDK вирівняний з **iOS SDK** за форматом ad request URL і поведінкою SSP.

## Репозиторій

| | |
|---|---|
| GitHub | `https://github.com/BidsCube/bidscube-sdk-unity` |
| SSH remote (внутрішній) | `max` → `git@github.com:BidsCube/bidscube-sdk-unity.git` |
| HTTPS remote | `origin` → потребує credentials |
| Гілка | `master` |

## UPM manifest (`package.json`)

```json
{
  "name": "com.bidscube.sdk",
  "version": "1.2.14",
  "unity": "2020.3",
  "dependencies": {
    "com.unity.ugui": "2.0.0",
    "com.unity.textmeshpro": "3.0.6"
  }
}
```

**Pin для production:**

```json
"com.bidscube.sdk": "https://github.com/BidsCube/bidscube-sdk-unity.git#v1.2.14"
```

## Структура репозиторію

```
bidscube-sdk-unity/
├── package.json              # UPM manifest, semver
├── README.md                 # Публічний огляд
├── INTEGRATION.md            # Публічна інструкція інтеграції
├── CHANGELOG.md              # Історія версій
├── RELEASE_CHECKLIST.md      # Чекліст перед релізом
├── LICENSE.md
├── doc/                      # ← ця внутрішня документація
├── Runtime/
│   ├── BidscubeSDK/          # C# runtime (основний код)
│   └── Plugins/              # Native WebView (Android/iOS/macOS/WebGL)
├── Editor/                   # Android Gradle patcher, define sync
├── scripts/                  # copy-to-runtime.ps1 (unitypackage)
└── .github/workflows/        # release.yml — GitHub Release на tag v*
```

### Runtime/BidscubeSDK/

| Підпапка | Призначення |
|----------|-------------|
| `Core/` | Enums, config, constants, callbacks, VAST, IMA wrapper |
| `Controllers/` | AdViewController, WebView, test scenes |
| `Views/` | BannerAdView, VideoAdView, NativeAdView |
| `Networking/` | URLBuilder, DeviceInfo, WebViewObject |
| `Android/` | Feature sets, export settings, LiteNoVideo guard |
| `Settings/` | AdSizeSettings ScriptableObject |
| `Scenes/` | 5 тестових Unity-сцен |
| `BasicIntegration/` | Мінімальний AdExample |
| `Debug/` | AgentNdjsonDebugLog (internal telemetry) |

## Мінімальна інтеграція (код)

```csharp
using BidscubeSDK;

var config = new SDKConfig.Builder()
    .EnableLogging(true)
    .BaseURL(Constants.BaseURL)
    .Build();

BidscubeSDK.Initialize(config);

// Banner
BidscubeSDK.ShowImageAd("20212", callback);

// Interstitial video
BidscubeSDK.ShowInterstitialVideoAd("20213", callback);

// Rewarded video (callback implements IRewardedAdCallback)
BidscubeSDK.ShowRewardedVideoAd("20213", rewardedCallback);

// Native
BidscubeSDK.ShowNativeAd("20214", callback);
```

## Залежності та межі пакету

**У пакеті є:**

- Core C# runtime + sample scenes
- WebView plugins (unity-webview fork)
- Android export tooling (Editor)

**У пакеті немає (окремі пакети):**

- AppLovin MAX adapter (`com.bidscube.applovin.max`)
- LevelPlay / ironSource adapter (`com.bidscube.levelplay`)
- Mediation SDK binaries

## Assembly definitions

| Assembly | Шлях | Хто посилається |
|----------|------|-----------------|
| `BidscubeSDK` | `Runtime/BidscubeSDK/BidscubeSDK.asmdef` | Host app, samples |
| `BidscubeSDK.Android` | `Runtime/BidscubeSDK/Android/` | Adapters, Gradle tooling |
| `BidscubeSDK.Android.Editor` | `Editor/` | Тільки Editor |

## Singleton lifecycle

`BidscubeSDK` — static facade над `MonoBehaviour` singleton:

- GameObject `"BidscubeSDK"`, `DontDestroyOnLoad`
- Створюється при першому виклику API
- `Cleanup()` — знищує всі ads, скидає config/consent/position

## SSP endpoint

Default base URL: `https://ssp-bcc-ads.com/sdk`

Query params будуються в `URLBuilder` — див. [networking-vast.md](networking-vast.md).
