# Troubleshooting та FAQ

Внутрішній довідник для типових проблем інтеграції та QA.

---

## Ініціалізація

### SDK не ініціалізується

| Симптом | Причина | Рішення |
|---------|---------|---------|
| API no-op | `SetInitializationEnabled(false)` | `SetInitializationEnabled(true)` |
| API no-op | `config.DisableInitialization` | Builder `.DisableInitialization(false)` |
| API no-op | Env `BIDSCUBE_DISABLE_INIT` | Unset env var |

### `IsInitialized()` false після `Initialize()`

Перевірити gates вище. `Cleanup()` скидає config.

---

## Banner / Image

### WebView порожній / білий екран

- Перевірити network / placement ID
- `OnAdFailed` code + message
- Android: WebView plugin AAR injected (Gradle patcher)
- iOS: `WebView.mm` у build

### Banner не в slot parent

```csharp
BidscubeSDK.SetAdViewsParentTransform(slotRect, useLayoutSlotSizing: true);
BidscubeSDK.ReapplyLayoutForAllActiveAds();
```

Video **ігнорує** parent override.

### Розмір banner неправильний

- `AdSizeSettings` asset + `preferDefaultsOverAdm`
- Server adm width/height у JSON response

---

## Video

### Video не грає (Android device)

| Причина | Діагностика |
|---------|-------------|
| LiteNoVideo build | `OnAdFailed(1006)` |
| HLS/DASH URL | HLS/DASH URLs (`.m3u8`, `.mpd`) may be classified as direct video URLs, but Unity `VideoPlayer` support can be platform-dependent and unreliable. `VASTParser` prefers progressive MP4. |
| HTTPS cert | Check `OnAdFailed` network message |
| Empty VAST | Parse fail → 1002 |

**Fix для streaming:** cache fallback (`DownloadToCacheThenReplay`) на Android.

### VAST ad tag URL грає як direct video

Має бути fixed у 1.2.14: non-`.mp4` URLs → fetch first. Перевірити `OpenRtbVideoUrlHelper.IsLikelyDirectVideoUrl`.

### Pod грає тільки 1 slot

Перевірити nested JSON path — має викликати `LoadPlaybackPlanCoroutine` для 2+ slots (1.2.14+).

### Reward не видається

- Тільки `ShowRewardedVideoAd` + `IRewardedAdCallback`
- Тільки після **natural complete** (не skip, не close)
- Pod: reward після **останнього** slot

### Skip не з'являється

- Default 5s або VAST `skipoffset`
- `OnVideoAdSkippable` після countdown

### End card без картинки

By design якщо немає companion — last frame fallback. Close має працювати.

---

## OpenRTB pod

### `bids[]` ignored

`OpenRtbPodMetadataEnabled(false)` — увімкнути в config.

### Strict mode — no plan

- Duration mismatch з `rqddurs`
- Hybrid fixed slots > `poddur`
- Check logs: `[PoddedPlaybackPlanBuilder] Strict:`

### Slot failure продовжує pod

`VideoPodSkipPolicy.SkipCurrentAndContinue` + `VideoPodContinueOnSlotError(true)`.

### Slot failure зупиняє pod

`FailEntirePod` або `VideoPodContinueOnSlotError(false)`.

---

## Native

### Native не парситься

- Response має бути OpenRTB native JSON або HTML adm
- `c=n`, `res=json` у URL

### WebView vs Unity UI

`NativeAdView._useWebViewRendering = true` за замовчуванням.

---

## Android

### Error 1006 на video

`BIDSCUBE_ANDROID_LITE_NO_VIDEO` — expected для LiteNoVideo. Use `FullWithVideo` або mediation.

### Gradle duplicate class

Mediation AAR + core AAR overlap — перевірити adapter repo, `SkipInjectionIntegratorOwnsCore`.

### Desugaring errors

`FullWithVideo` потребує `desugar_jdk_libs` у launcher — Gradle patcher injects.

---

## iOS

### WebView не показується

- `WebView.mm` compiled
- Post-process: `UnityWebViewPostprocessBuild.cs`

### Video black screen

- Codec support (H.264 MP4 preferred)
- Check URL reachable

---

## Тести

### EditMode tests не видно

- Host project з `ProjectSettings/`
- Package resolved
- Test Runner → EditMode filter

### Package-only repo — CLI fails

Очікувано. Див. [editmode-tests.md](editmode-tests.md).

---

## Git / release

### Push rejected (non-fast-forward)

```bash
git fetch max
git rebase max/master
git push max master
```

### `origin` HTTPS auth fail

Використовувати `max` SSH remote.

---

## Де шукати логи

- `SDKConfig.EnableLogging(true)`
- Unity Console: `[VideoAdView]`, `[PoddedPlaybackPlanBuilder]`, `[BidscubeSDK]`
- Internal: `AgentNdjsonDebugLog` (NDJSON, не public)

---

## Швидкі посилання

| Тема | Документ |
|------|----------|
| API | [public-api.md](public-api.md) |
| OpenRTB | [openrtb.md](openrtb.md) |
| Android feature sets | [android.md](android.md) |
| QA scenes | [test-scenes-qa.md](test-scenes-qa.md) |
| Known issues | [known-issues.md](known-issues.md) |
