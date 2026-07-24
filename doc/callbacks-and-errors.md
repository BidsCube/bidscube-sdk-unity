# Callbacks та коди помилок

Файл: `Runtime/BidscubeSDK/Core/Callbacks.cs`

---

## IAdCallback (обов'язкові методи)

Implement на MonoBehaviour test scenes або app callback class.

| Method | When |
|--------|------|
| `OnAdLoading(placementId)` | Load started |
| `OnAdLoaded(placementId)` | Creative ready (video: prepared) |
| `OnAdDisplayed(placementId)` | Visible to user |
| `OnAdClicked(placementId)` | User click (video click-through, end card, native link) |
| `OnAdClosed(placementId)` | Dismissed / destroyed |
| `OnAdFailed(placementId, errorCode, errorMessage)` | Load/play failure |
| `OnVideoAdStarted(placementId)` | Video playback started |
| `OnVideoAdCompleted(placementId)` | Video played to end |
| `OnVideoAdSkipped(placementId)` | Skip or close before complete |
| `OnVideoAdSkippable(placementId)` | Skip button became active |
| `OnInstallButtonClicked(placementId, buttonText)` | Native install CTA |

### AdCallback base class

Empty virtual implementations for all `IAdCallback` + `IRewardedAdCallback` methods — extend for samples.

---

## IRewardedAdCallback (optional, окремий interface)

```csharp
void OnUserRewarded(string placementId);
```

- **Not** on `IAdCallback` — check `callback is IRewardedAdCallback`
- Fires only for `VideoAdFormat.Rewarded` after **natural complete**
- Does **not** fire on: skip, close before complete, failure

### Example

```csharp
public class MyAds : MonoBehaviour, IAdCallback, IRewardedAdCallback
{
    public void OnUserRewarded(string placementId) {
        GrantCoins();
    }
    // ... implement other IAdCallback methods
}
```

---

## IAdRenderOverride (optional)

```csharp
bool OnAdRenderOverride(string placementId, string adm, AdType adType, int position);
```

- Return **`true`** → host app rendered adm; SDK skips default WebView/VideoPlayer/UI
- Return **`false`** → SDK continues normal path
- Invoked before render in Banner, Video, Native views

---

## IConsentCallback

| Method | Stub behavior |
|--------|---------------|
| `OnConsentInfoUpdated()` | After 0.1s delay |
| `OnConsentInfoUpdateFailed(Exception)` | Rarely called |
| `OnConsentFormShown()` | On ShowConsentForm |
| `OnConsentFormError(Exception)` | On error path |
| `OnConsentGranted()` | Auto-grant in stub |
| `OnConsentDenied()` | Manual test only |
| `OnConsentNotRequired()` | Optional |
| `OnConsentStatusChanged(bool)` | Flag changes |

Base: `ConsentCallback` — empty virtuals.

---

## Error codes

Defined in `Constants.ErrorCodes` (+ top-level `ErrorCodes` alias):

| Code | Constant | Typical cause |
|------|----------|---------------|
| 1001 | `InvalidURL` | URLBuilder failure |
| 1002 | `InvalidResponse` | Bad server body, VAST parse fail, empty adm |
| 1003 | `NetworkError` | HTTP error |
| 1004 | `TimeoutError` / `Timeout` | Load timeout (banner/native controller) |
| 1005 | `UnknownError` | Unclassified |
| 1006 | `LiteNoVideoVideoNotSupported` | Android LiteNoVideo + direct video API |

### Error messages

`Constants.ErrorMessages.*` — human-readable strings paired with codes.

Example LiteNoVideo:

> Bidscube video is disabled in LiteNoVideo Android builds. Use FullWithVideo or AppLovin MAX for video.

---

### OpenRTB pod — complete path (2 slots)

```
OnAdLoading
OnAdLoaded                    (slot 1 prepared)
OnAdDisplayed
OnVideoAdStarted              (pod start)
OnVideoAdSkippable            (per slot, if applicable)
OnVideoAdCompleted            (slot 1) — internal advance, no app callback
... slot 2 plays ...
OnVideoAdCompleted            (slot 2 = last)
OnUserRewarded                (rewarded only, after last slot)
OnAdClosed
```

### OpenRTB pod — skip on slot 1

```
OnAdLoading → OnAdLoaded → OnAdDisplayed → OnVideoAdStarted
→ OnVideoAdSkipped            (user skip/close — pod stops)
→ OnAdClosed
(slot 2 does NOT play)
```

### OpenRTB pod — slot failure with continue

При `VideoPodSkipPolicy.SkipCurrentAndContinue` + `VideoPodContinueOnSlotError(true)`:

```
slot 1 fails → advance to slot 2 (no OnAdFailed if recovered)
```

При `FailEntirePod` або continue disabled:

```
slot 1 fails → OnAdFailed → pod stops
```

---

## Video callback order reference (single ad)

### Interstitial — complete path

```
OnAdLoading
OnAdLoaded
OnAdDisplayed
OnVideoAdStarted
OnVideoAdSkippable          (after skip countdown)
OnVideoAdCompleted
OnAdClosed                  (user closes end card)
```

### Interstitial — skip path

```
OnAdLoading
OnAdLoaded
OnAdDisplayed
OnVideoAdStarted
OnVideoAdSkippable
OnVideoAdSkipped
OnAdClosed
```

### Rewarded — complete (with reward)

```
OnAdLoading
OnAdLoaded
OnAdDisplayed
OnVideoAdStarted
OnVideoAdSkippable
OnVideoAdCompleted
OnUserRewarded              ← only here
OnAdClosed
```

### Rewarded — skip (no reward)

```
...
OnVideoAdSkipped
OnAdClosed
(no OnUserRewarded)
```

### Close during video (before complete)

```
OnVideoAdSkipped            (if not already skipped/completed)
OnAdClosed
```

---

## Guarded notifications (VideoAdView)

Internal `Notify*` methods prevent duplicate callback fires:

- `_hasLoading`, `_hasLoaded`, `_hasDisplayed`, `_hasStarted`
- `_hasCompleted`, `_hasSkipped`, `_hasClosed`, `_hasRewarded`

Reset on each new load via `ResetCallbackState()`.

---

## Handling failures in app

```csharp
public void OnAdFailed(string placementId, int errorCode, string errorMessage)
{
    switch (errorCode)
    {
        case ErrorCodes.TimeoutError:
            // retry or skip
            break;
        case ErrorCodes.LiteNoVideoVideoNotSupported:
            // use mediation / FullWithVideo build
            break;
        default:
            Logger.Log($"Ad failed: {errorCode} {errorMessage}");
            break;
    }
}
```
