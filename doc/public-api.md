# Публічний API — `BidscubeSDK`

Файл: `Runtime/BidscubeSDK/BidscubeSDK.cs`

Усі методи **static**. Singleton `MonoBehaviour` створюється автоматично.

---

## Lifecycle

| Метод | Опис |
|-------|------|
| `Initialize(SDKConfig config)` | Основна ініціалізація. No-op якщо `SetInitializationEnabled(false)`, `config.DisableInitialization`, або env `BIDSCUBE_DISABLE_INIT` |
| `Initialize()` | Default config: logging on, timeout 30s, `Constants.BaseURL` |
| `IsInitialized()` | `_configuration != null` |
| `SetInitializationEnabled(bool)` / `IsInitializationEnabled()` | Глобальний gate init |
| `Cleanup()` | `ClearAllAds()` + reset config, consent, positions |
| `GetConfiguredAdTimeoutMs()` | З config або `Constants.DefaultTimeoutMs` (30000) |
| `ApplyConfiguredTimeoutTo(UnityWebRequest)` | Timeout у секундах на request |

---

## Layout / parenting

| Метод | Опис |
|-------|------|
| `SetAdViewsParentTransform(Transform parent, bool useLayoutSlotSizing)` | Parent для non-video ads (embedded launcher slot) |
| `ClearAdViewsParentTransform()` | Повернення до default `SDKContent` root |
| `ReapplyLayoutForAllActiveAds()` | Re-layout + WebView margin sync після Unity layout rebuild |

---

## Position

| Метод | Опис |
|-------|------|
| `SetAdPosition(AdPosition)` | Manual override (найвищий пріоритет) |
| `GetAdPosition()` | Manual position |
| `GetResponseAdPosition()` | Position з server adm |
| `SetResponseAdPosition(AdPosition)` | Internal — встановлюють views |
| `GetEffectiveAdPosition()` | Manual > server > default |

### `AdPosition`

```csharp
public enum AdPosition {
    Unknown = 0,
    AboveTheFold = 1,
    DependOnScreenSize = 2,  // internal
    BelowTheFold = 3,
    Header = 4,
    Footer = 5,
    Sidebar = 6,
    FullScreen = 7
}
```

---

## Consent (stub)

| Метод | Опис |
|-------|------|
| `RequestConsentInfoUpdate(IConsentCallback)` | Delay 0.1s → `OnConsentInfoUpdated` |
| `ShowConsentForm(IConsentCallback)` | Auto-grant після delay |
| `EnableConsentDebugMode(string testDeviceId)` | Debug device id |
| `ResetConsent()` | Clear flags (Consent Test Scene) |
| `IsConsentRequired()` / `HasAdsConsent()` / `HasAnalyticsConsent()` | Flag accessors |
| `GetConsentStatusSummary()` | Text summary |

⚠️ `Initialize()` ставить `_hasAdsConsentFlag = true` за замовчуванням. Не production CMP — див. [known-issues.md](known-issues.md).

---

## URL

| Метод | Опис |
|-------|------|
| `BuildRequestURL(placementId, AdType, AdPosition)` | Delegates to `URLBuilder` з configured base URL |

---

## Banner / Image

| Метод | Опис |
|-------|------|
| `ShowImageAd(placementId, callback)` | Image ad через AdViewController |
| `ShowHeaderBanner(placementId, callback)` | Position Header + banner |
| `ShowFooterBanner(placementId, callback)` | Position Footer |
| `ShowSidebarBanner(placementId, callback)` | Position Sidebar |
| `ShowCustomBanner(placementId, position, width, height, callback)` | Explicit RectTransform size |
| `GetBannerAdView(placementId, callback)` → `GameObject` | Creative root |
| `GetBannerAdView(placementId, AdPosition, callback)` → `BannerAdView` | Typed overload |
| `GetImageAdView(...)` | Alias для `GetBannerAdView` |
| `RemoveAllBanners()` | Destroy tracked banners |
| `UntrackBanner(BannerAdView)` | Untrack без destroy |
| `GetActiveBannerCount()` | Count active banners |

---

## Video

| Метод | Опис |
|-------|------|
| `ShowInterstitialVideoAd(placementId, callback)` | `VideoAdFormat.Interstitial` |
| `ShowRewardedVideoAd(placementId, callback)` | `VideoAdFormat.Rewarded` |
| `ShowVideoAd(placementId, callback)` | **Alias** → interstitial |
| `ShowSkippableVideoAd(placementId, skipButtonText, callback)` | **Alias** → interstitial (`skipButtonText` ignored) |
| `GetInterstitialVideoAdView(placementId, callback)` → `GameObject` | Manual control |
| `GetRewardedVideoAdView(placementId, callback)` → `GameObject` | Manual control |
| `GetVideoAdView(placementId, callback)` → `GameObject` | **Alias** → interstitial view |

### `VideoAdFormat`

```csharp
public enum VideoAdFormat { Interstitial, Rewarded }
```

### LiteNoVideo guard

Якщо compiled з `BIDSCUBE_ANDROID_LITE_NO_VIDEO`:

- Video entry points → `OnAdFailed(1006)` immediately
- Player не створюється

Див. [android.md](android.md).

---

## Native

| Метод | Опис |
|-------|------|
| `ShowNativeAd(placementId, callback)` | Native через AdViewController |
| `GetNativeAdView(placementId, callback)` → `GameObject` | Legacy standalone path |

---

## Cleanup

| Метод | Опис |
|-------|------|
| `ClearAllAds()` | Destroy all controllers + banners |

---

## Enums

### `AdType`

```csharp
public enum AdType { Image, Video, Native }
```

---

## Constants (`Constants.cs`)

| Constant | Value |
|----------|-------|
| `DefaultTimeoutMs` | 30000 |
| `DefaultAdPosition` | `AdPosition.Unknown` |
| `BaseURL` | `https://ssp-bcc-ads.com/sdk` |
| `UserAgentPrefix` | `"BidscubeSDK"` |
| `SdkVersion` | `"1.2.14"` (sync with `package.json`) |

### Error codes

| Code | Name |
|------|------|
| 1001 | `InvalidURL` |
| 1002 | `InvalidResponse` |
| 1003 | `NetworkError` |
| 1004 | `TimeoutError` / `Timeout` |
| 1005 | `UnknownError` |
| 1006 | `LiteNoVideoVideoNotSupported` |

Public aliases: top-level `ErrorCodes`, `ErrorMessages` (same values).

---

## Lower-level API (samples)

`AdViewController.Initialize(...)` — напряму в `AdExample.cs`. Для production рекомендується static `BidscubeSDK` API.

### VideoAdView direct

```csharp
var go = BidscubeSDK.GetVideoAdView(placementId, callback);
var view = go.GetComponent<VideoAdView>();
view.LoadVideoAdFromVastXml(vastXmlString);  // QA / custom VAST
view.LoadVideoAdFromURL(url);                 // SSP або direct MP4
```

---

## Aliases summary

| Alias | Actual |
|-------|--------|
| `ShowVideoAd` | `ShowInterstitialVideoAd` |
| `GetVideoAdView` | `GetInterstitialVideoAdView` |
| `ShowSkippableVideoAd` | `ShowInterstitialVideoAd` |
| `GetImageAdView` | `GetBannerAdView` |
