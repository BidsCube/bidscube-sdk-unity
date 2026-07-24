# Відомі обмеження та технічний борг

Внутрішній список нюансів, які важливо знати при розробці та підтримці SDK.

---

## Constants.SdkVersion sync

`Constants.SdkVersion` is currently synced with `package.json` at `1.2.15`.

Keep this as a release checklist item:

- `package.json` version
- `Runtime/BidscubeSDK/Core/Constants.cs`
- `CHANGELOG.md`
- Git tag `vX.Y.Z`

---

## Consent API — stub, не production CMP

| | |
|---|---|
| **Проблема** | `RequestConsentInfoUpdate` / `ShowConsentForm` — simulated delays, auto-grant |
| **Impact** | Не GDPR/ATT compliant out of the box |
| **Рекомендація** | Host app інтегрує власний CMP; SDK consent flags — placeholder для test scenes |

`Initialize()` встановлює `_hasAdsConsentFlag = true` за замовчуванням.

---

## IMA disabled

| | |
|---|---|
| **Проблема** | `VideoAdView._useIMA = false` — IMA event bridge not wired |
| **Impact** | Production video path = **VAST + Unity VideoPlayer** only |
| **Код** | `IMAVideoPlayer.cs`, `IVideoPlayerEventListener.cs` — future work |

---

## HLS / DASH на VideoPlayer

| | |
|---|---|
| **Проблема** | HLS/DASH URLs (`.m3u8`, `.mpd`) may be classified as direct video URLs, but Unity `VideoPlayer` support can be platform-dependent and unreliable. Progressive MP4 is preferred. |
| **Mitigation** | `VASTParser` prefers progressive MP4 over streaming formats |
| **Fallback** | Android cache download + `file://` replay on stream failure |

---

## Mediation adapters — окремі репозиторії

This package is the core `com.bidscube.sdk` Unity SDK. AppLovin MAX and LevelPlay adapters are separate packages/repositories. This core package should not include AppLovin/LevelPlay AARs or adapter code.

Core SDK **не містить**:

- AppLovin MAX adapter AAR/native code
- LevelPlay / ironSource adapter

Документувати cross-links для integrators; Gradle conflicts — перевіряти в adapter repos.

---

## SdkIntegrationContext — тільки для samples

Не частина public integrator contract. Production apps не повинні покладатися на PlayerPrefs mode switching.

---

## ShowSkippableVideoAd — legacy compatibility only

`ShowSkippableVideoAd(placementId, skipButtonText, callback)` is a legacy compatibility alias for interstitial video. `skipButtonText` is currently ignored. Prefer `ShowInterstitialVideoAd(...)` for new integrations.

---

## AdViewController Initialize overload

4-param overload `Initialize(placementId, adType, callback, position)` removed; 5-param version has defaults — existing 3-arg calls still compile.

---

## AgentNdjsonDebugLog

Internal debug instrumentation in URLBuilder / VideoAdView — not documented publicly; may write local NDJSON. Review before external builds if privacy-sensitive.

---

## Test scene IAdRenderOverride stubs

Some test controllers may have incomplete override signatures vs canonical:

```csharp
bool OnAdRenderOverride(string placementId, string adm, AdType adType, int position);
```

Verify Custom Ad Render Scene for correct interface.

---

## Date inconsistencies in CHANGELOG

Some historical entries have dates that don't sort chronologically (e.g. 1.2.12 dated 2026-04-30 vs 1.2.11 dated 2026-05-12). Cosmetic only.

---

## Git remotes

- **`max`** (SSH) — preferred for push internally
- **`origin`** (HTTPS) — may fail without credentials

Document for team onboarding.

---

## Video timeout

`AdViewController` load timeout **not applied to video** — long prepare/stream won't auto-fail via controller timeout. Failures come from VideoPlayer error / network.

---

## End card без preview і без video frame

If RenderTexture empty and no companion — end card shows overlay with transparent preview area. **No crash** — by design (v1.2.13). QA should verify Close still works.

---

## OpenRTB pod metadata disabled

При `OpenRtbPodMetadataEnabled(false)` — `bids[]` / seatbid pod parsing skipped; root `adm` still resolves.

---

## OpenRTB POST not implemented

OpenRTB 2.6 support is response-side podded video parsing only. The SDK still uses the legacy GET ad request flow through `URLBuilder`. `OpenRtbBidRequestBuilder` is a placeholder. Full OpenRTB POST bid requests are not implemented.

---

## Recommended follow-ups (backlog)

1. Wire IMA event bridge or remove dead code path
2. Real CMP integration or remove consent stubs from public API docs
3. Optional: expose skip button text from VAST/custom API
4. Auto-generate `public-api.md` section from XML doc comments (if team wants)
