# Video ads

Файл: `Runtime/BidscubeSDK/Views/VideoAdView.cs`

---

## Interstitial vs Rewarded

| | Interstitial | Rewarded |
|---|-------------|----------|
| API | `ShowInterstitialVideoAd` / `ShowVideoAd` | `ShowRewardedVideoAd` |
| Format | `VideoAdFormat.Interstitial` | `VideoAdFormat.Rewarded` |
| Reward callback | Ні | `IRewardedAdCallback.OnUserRewarded` |
| Reward trigger | — | Тільки після **natural complete** (не skip, не close, не fail) |

Host app вирішує **коли** показувати interstitial — SDK не має frequency cap.

---

## Завантаження

### З SSP (network)

```csharp
BidscubeSDK.ShowInterstitialVideoAd("20213", callback);
// або
var go = BidscubeSDK.GetVideoAdView("20213", callback);
go.GetComponent<VideoAdView>().LoadVideoAdFromURL(url);
```

`LoadVideoAdFromURL` coroutine:

1. HTTP GET (`c=v`, `res=json`) — **legacy GET flow unchanged**
2. `VideoAdPayloadResolver.Resolve(responseText, SDKConfig)` — OpenRTB pod + legacy adm/VAST
3. If multi-slot `VideoPlaybackPlan` → sequential `LoadPlaybackSlotCoroutine` per slot
4. Else single slot: VAST parse / direct URL
5. Wrapper VAST → recursive fetch (max depth **5**)
6. `VASTParser.Parse` → video URL per slot
7. `VideoPlayer.Prepare()` → `Play()`

### OpenRTB 2.6 podded video (response-side)

**Повний module reference:** [openrtb.md](openrtb.md)

OpenRTB 2.6 support is response-side podded video parsing only. The SDK still uses the legacy GET ad request flow through `URLBuilder`. `OpenRtbBidRequestBuilder` is a placeholder. Full OpenRTB POST bid requests are not implemented.

| Component | File |
|-----------|------|
| Payload resolver | `OpenRTB/VideoAdPayloadResolver.cs` |
| Pod normalizer | `OpenRTB/OpenRtbPoddedResponseNormalizer.cs` |
| Plan builder | `OpenRTB/PoddedPlaybackPlanBuilder.cs` |
| VAST multi-ad split | `OpenRTB/VastAdSequenceParser.cs` |
| JSON parser | `OpenRTB/OpenRtbJson.cs` |

**Response shapes:** root `adm`, `bids[]`, `seatbid[].bid[]`, `openrtb.video`, `openRtb.video`, root `video`, raw VAST.

**Pod modes:** structured (`slotinpod` + `rqddurs`), dynamic (`poddur` budget), hybrid (fixed slots + dynamic fill), single.

**Callbacks for pods:**

- `OnAdLoaded` — once when first slot prepares
- `OnVideoAdStarted` — once when pod starts
- `OnVideoAdCompleted` / `OnUserRewarded` — once after **last** slot
- Skip/close — stops pod, no remaining slots

- `VastAdTagUrl` — HTTP URL returning VAST/JSON (fetch first)
- `DirectVideoUrl` — direct `.mp4`/`.webm`/etc. for `VideoPlayer`

`OpenRtbVideoUrlHelper.IsLikelyDirectVideoUrl` distinguishes ad tag URLs from direct media.

**`VideoPodSkipPolicy`:** user skip/close always stops the pod; slot failures use `VideoPodContinueOnSlotError` when policy is `SkipCurrentAndContinue`, or fail entire pod when `FailEntirePod`.

**`VideoPodShowCounter`:** shows a lightweight pod slot counter overlay during sequential pod playback when supported by `VideoAdView`.


### Inline VAST (QA, без backend)

```csharp
videoAdView.LoadVideoAdFromVastXml(vastXmlString);
```

- Parse locally через `VASTParser`
- Fire impression URLs
- Skip offset з VAST
- Без HTTP для VAST body

---

## VAST parsing

Файл: `Runtime/BidscubeSDK/Core/VASTParser.cs`

### `VASTData` fields

| Field | Source |
|-------|--------|
| `videoUrl` | `MediaFile` (MP4 preferred > WebM > first) |
| `clickThroughUrl` | `VideoClicks > ClickThrough` |
| `previewImageUrl` | `Companion > StaticResource` |
| `previewClickThroughUrl` | `Companion > CompanionClickThrough` |
| `skipOffset` | `Linear skipoffset="HH:MM:SS"` attribute |
| `duration` | `Duration` |
| `impressionUrls`, `startUrls`, quartile, `completeUrls`, `skipUrls`, `clickTrackingUrls` | Tracking |

### Media selection policy

**MP4 > WebM > перший URL** — progressive formats preferred. HLS/DASH URLs (`.m3u8`, `.mpd`) may be classified as direct video URLs, but Unity `VideoPlayer` support can be platform-dependent and unreliable.

### Wrapper VAST

`VideoAdView` detects wrapper → fetch `VASTAdTagURI` → merge impressions → parse inline.

---

## UI layers (z-order)

1. `VideoBackdrop` — чорний fullscreen фон
2. `_videoTexture` (RawImage) — video frame
3. Skip button (top-left)
4. Close button (top-right, **завжди visible**)
5. Progress slider (bottom)
6. End card (after complete/skip)
7. Close поверх end card

---

## Skip flow

1. Default `_skipTime = 5.0f` або VAST `skipOffset`
2. `EnableSkipButton` coroutine:
   - Показує skip button (disabled)
   - Countdown: `"Skip in N"` кожну секунду
   - Після countdown: `"Skip"`, `interactable = true`
   - `OnVideoAdSkippable(placementId)`
3. Skip click:
   - Fire VAST skip tracking
   - `OnVideoAdSkipped`
   - `ShowEndCard()` — **не** auto-dismiss

---

## End card

End card behavior, introduced in 1.2.13 and still current in 1.2.15.

Після **complete** або **skip**:

```
ShowEndCard()
  → pause video
  → hide skip
  → show EndCardRoot
  → if previewImageUrl → LoadEndCardPreview (HTTP texture)
  → else fallback: last video frame (RenderTexture)
  → CTA "Learn More" if click URL exists
  → Close on top (SetAsLastSibling)
```

### Preview fallback rules

| У VAST є companion image? | Поведінка |
|---------------------------|-----------|
| **Так** | Завантажити `StaticResource` URL на end card |
| **Ні** | **Існуюча fallback логіка без змін** — last frame з RenderTexture; end card **все одно показується** |
| Load fail | Fallback на last frame |

### Click on end card

Priority URL:

1. `previewClickThroughUrl` (companion)
2. `clickThroughUrl` (linear VideoClicks)

→ `Application.OpenURL` + `OnAdClicked`

### Close on end card

→ `OnAdClosed` + `DismissVideoAdHierarchy()`

---

## Callback lifecycle (guarded)

Duplicate fires blocked через `_hasLoading`, `_hasLoaded`, etc.

| Callback | When |
|----------|------|
| `OnAdLoading` | Start load |
| `OnAdLoaded` | `VideoPlayer.prepareCompleted` |
| `OnAdDisplayed` + `OnVideoAdStarted` | Playback started |
| `OnVideoAdSkippable` | Skip countdown finished |
| `OnVideoAdCompleted` | Natural end |
| `OnVideoAdSkipped` | Skip button, або close before complete |
| `OnUserRewarded` | Rewarded + completed only |
| `OnAdClosed` | Close button або destroy |
| `OnAdFailed` | Network / parse / playback error |
| `OnAdClicked` | Video click-through або end card click |

### Typical rewarded sequence (complete)

```
OnAdLoading → OnAdLoaded → OnAdDisplayed → OnVideoAdStarted
→ OnVideoAdSkippable (optional)
→ OnVideoAdCompleted → OnUserRewarded
→ [end card visible]
→ OnAdClosed (user taps Close)
```

### Typical skip sequence

```
... → OnVideoAdSkipped → [end card]
→ OnAdClosed
```

---

## IMA

- `IMAVideoPlayer.cs` — Android JNI stubs для Google IMA
- `IVideoPlayerEventListener` — unified interface для future bridge
- **`VideoAdView._useIMA = false`** — IMA disabled; production path is **custom VAST + Unity VideoPlayer**

---

## Legacy video API

`ShowSkippableVideoAd(placementId, skipButtonText, callback)` is a legacy compatibility alias for interstitial video. `skipButtonText` is currently ignored. Prefer `ShowInterstitialVideoAd(...)` for new integrations.

---

## Android video cache fallback

При HTTPS streaming failure:

1. `DownloadToCacheThenReplay`
2. Save to `Application.temporaryCachePath`
3. Replay via `file://`

(з CHANGELOG 1.2.3+)

---

## Fullscreen canvas

Video завжди під `SDKContent` fullscreen canvas (Screen Space Overlay), не під embedded slot parent.

---

## LiteNoVideo

Compile symbol `BIDSCUBE_ANDROID_LITE_NO_VIDEO` → video API blocked at `BidscubeSDK` and `AdViewController` level.

Див. [android.md](android.md).

---

## QA: local VAST test cases

Див. [test-scenes-qa.md](test-scenes-qa.md):

- `local_vast_no_preview` — video без companion
- `local_vast_with_preview` — Big Buck Bunny + companion JPEG + skip 5s
