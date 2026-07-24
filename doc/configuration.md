# Configuration

---

## SDKConfig

Файл: `Runtime/BidscubeSDK/Core/SDKConfig.cs`

Builder pattern:

```csharp
var config = new SDKConfig.Builder()
    .EnableLogging(true)
    .EnableDebugMode(false)
    .DefaultAdTimeout(30000)
    .DefaultAdPosition(AdPosition.Unknown)
    .BaseURL(Constants.BaseURL)
    .AdSizeSettings(myAdSizeSettings)
    .DisableInitialization(false)
    .UserId("your-app-user-id")
    .Build();

BidscubeSDK.Initialize(config);
```

### Properties

| Property | Builder | Default |
|----------|---------|---------|
| `EnableLogging` | `EnableLogging(bool)` | `true` |
| `EnableDebugMode` | `EnableDebugMode(bool)` | `false` |
| `DefaultAdTimeoutMs` | `DefaultAdTimeout(int)` | 30000 |
| `DefaultAdPosition` | `DefaultAdPosition(AdPosition)` | `Unknown` |
| `BaseURL` | `BaseURL(string)` | `Constants.BaseURL` |
| `AdSizeSettings` | `AdSizeSettings(asset)` | `null` |
| `DisableInitialization` | `DisableInitialization(bool)` | `false` |
| `UserId` | `UserId(string)` | `null` — integrator user id; sent as query `user_id` on ad requests for SSP postbacks |

Also after init:

```csharp
BidscubeSDK.SetUserId("player-123"); // update after login
BidscubeSDK.GetUserId();             // current value (or null)
```

Empty/null `user_id` is omitted from the request URL.

### OpenRTB pod video settings

| Property | Builder | Default |
|----------|---------|---------|
| `OpenRtbPodMetadataEnabled` | `OpenRtbPodMetadataEnabled(bool)` | `true` |
| `VideoPodDurationValidationMode` | `VideoPodDurationValidationMode(...)` | `Lenient` |
| `VideoPodSkipPolicy` | `VideoPodSkipPolicy(...)` | `SkipCurrentAndContinue` |
| `VideoPodContinueOnSlotError` | `VideoPodContinueOnSlotError(bool)` | `true` |
| `VideoPodShowCounter` | `VideoPodShowCounter(bool)` | `true` — shows a lightweight pod slot counter overlay during sequential pod playback when supported by `VideoAdView` |

Enums: `OpenRtbPodDurationValidationMode` (`Lenient`, `Strict`), `OpenRtbPodSkipPolicy` (`SkipCurrentAndContinue`, `FailEntirePod`).

OpenRTB 2.6 support is response-side podded video parsing only. The SDK still uses the legacy GET ad request flow through `URLBuilder`. `OpenRtbBidRequestBuilder` is a placeholder. Full OpenRTB POST bid requests are not implemented.

### Static detected metadata

| Property | Source |
|----------|--------|
| `DetectedAppId` | `Application.identifier` |
| `DetectedAppName` | `Application.productName` |
| `DetectedAppVersion` | `Application.version` |
| `DetectedLanguage` | `Application.systemLanguage` |
| `DetectedUserAgent` | `BidscubeSDK-Unity/1.0 (Unity ...; OS ...)` |

---

## AdSizeSettings

Файл: `Runtime/BidscubeSDK/Settings/AdSizeSettings.cs`

ScriptableObject: **Assets → Create → BidscubeSDK → Ad Size Settings**

Bundled default: `Runtime/BidscubeSDK/Settings/DefaultAdSizeSettings.asset`

| Field | Default | Usage |
|-------|---------|-------|
| `defaultBannerSize` | 1080×150 | Banner/image logical size |
| `defaultNativeSize` | 728×400 | Native logical size |
| `defaultVideoSize` | 0×0 | 0 = fullscreen |
| `preferDefaultsOverAdm` | `false` | Override server sizes when `true` |

Injected at `CreateAdViewController` → `SetAdSizeSettings` on child views.

---

## Ad position configuration

### Priority

1. `BidscubeSDK.SetAdPosition(manual)` — highest
2. Server response position (`SetResponseAdPosition`)
3. `SDKConfig.DefaultAdPosition`

### Test scenes

SDK Test Scene: toggle **Use Manual Position** + dropdown (UNKNOWN … FULL_SCREEN).

---

## Initialization gates

Init skipped when:

- `BidscubeSDK.SetInitializationEnabled(false)`
- `SDKConfig.DisableInitialization == true`
- Environment variable `BIDSCUBE_DISABLE_INIT` (where checked)

---

## Consent configuration

⚠️ **Stub implementation** — not a production CMP.

- `Initialize()` sets ads consent flag **true** by default
- `RequestConsentInfoUpdate` / `ShowConsentForm` — simulated delays
- Use **Consent Test Scene** for API smoke tests only

Див. [known-issues.md](known-issues.md).

---

## Logging

`Logger.cs` — conditional logs when `SDKConfig.EnableLogging`.

`EnableDebugMode` — additional debug verbosity in select paths.

---

## Android export settings (separate from SDKConfig)

ScriptableObject: **Assets → Create → Bidscube → Android Export Settings**

Див. [android.md](android.md) — `BidscubeAndroidExportSettings`.

---

## Runtime ad parent override

```csharp
// Embedded ad slot in launcher UI
BidscubeSDK.SetAdViewsParentTransform(launcherSlot, useLayoutSlotSizing: true);

// Reset
BidscubeSDK.ClearAdViewsParentTransform();
```

Video ads **ignore** this override.
