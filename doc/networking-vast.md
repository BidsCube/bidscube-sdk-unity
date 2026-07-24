# Networking та VAST

---

## URLBuilder

Файл: `Runtime/BidscubeSDK/Networking/URLBuilder.cs`

### `BuildAdRequestURL(baseURL, placementId, adType, position, timeoutMs, debug, ctaText?, userId?)`

Формат **вирівняний з iOS SDK**.

### Query parameters

| Param | Source | Example |
|-------|--------|---------|
| `placementId` | argument | `20212` |
| `c` | ad type | `b` banner, `v` video, `n` native |
| `m` | fixed | `api` |
| `res` | ad type | `js` (image), `json` (video, native) |
| `app` | fixed | `1` |
| `bundle` | `DeviceInfo.BundleId` | |
| `name` | `DeviceInfo.AppName` | |
| `app_store_url` | `DeviceInfo.AppStoreURL` | |
| `language` | `DeviceInfo.Language` | |
| `deviceWidth` / `deviceHeight` | screen pixels | |
| `ua` | `DeviceInfo.UserAgent` | |
| `ifa` | advertising ID | |
| `dnt` | Do Not Track | |
| `gdpr`, `gdpr_consent` | GDPR flags | |
| `us_privacy`, `ccpa`, `coppa` | privacy flags | |
| `user_id` | `SDKConfig.UserId` / `BidscubeSDK.SetUserId` | Optional; omitted when empty. Used by SSP for postbacks. |

### OpenRTB pod response (1.2.14+)

Video responses may include OpenRTB pod metadata. Parsing is **not** in URLBuilder — it happens in `VideoAdPayloadResolver` after HTTP GET returns body.

Див. повний reference: [openrtb.md](openrtb.md).

Legacy GET URL format **unchanged** — still `c=v`, `res=json`.

### Response format by type

| AdType | `res` | Expected body |
|--------|-------|---------------|
| Image | `js` | HTML or JSON wrapper with HTML adm |
| Video | `json` | JSON with VAST/XML adm, OpenRTB pod, or direct video URL |
| Native | `json` | OpenRTB native JSON adm |

---

## DeviceInfo

Файл: `Runtime/BidscubeSDK/Networking/DeviceInfo.cs`

Static accessors для ad request metadata:

- `BundleId`, `AppName`, `AppStoreURL`
- `DeviceWidth`, `DeviceHeight`
- `UserAgent` (Bidscube prefix + Unity/OS)
- `AdvertisingIdentifier`, `DoNotTrack`
- GDPR/CCPA/COPPA placeholder flags

---

## AdMarkupExtractor

Файл: `Runtime/BidscubeSDK/Networking/AdMarkupExtractor.cs`

Extract `adm` from:

- Flat JSON `{ "adm": "..." }`
- OpenRTB bid response envelopes
- Nested JSON layers

Used by Banner, Video, Native views before render.

---

## NetworkManager

Файл: `Runtime/BidscubeSDK/Networking/NetworkManager.cs`

Shared HTTP helpers (where used by views).

Timeout applied via `BidscubeSDK.ApplyConfiguredTimeoutTo(request)`.

---

## VASTParser

Файл: `Runtime/BidscubeSDK/Core/VASTParser.cs`

Див. також **OpenRTB pod module:** `Runtime/BidscubeSDK/OpenRTB/` — `VastAdSequenceParser` для multi-`<Ad>` VAST у pod slots.

### `Parse(string vastXml)` → `VASTData`

Supports VAST **2.0 / 3.0 / 4.x** InLine creatives.

### Key parsing rules

1. **Video URL** — first suitable `MediaFile`; prefer progressive MP4
2. **Skip offset** — `Linear@skipoffset` attribute (`HH:MM:SS` or seconds)
3. **Companion preview** — first `Companion > StaticResource` inner text (trimmed)
4. **Companion click** — `CompanionClickThrough`
5. **Tracking** — `Impression`, `Tracking event="start|firstQuartile|midpoint|thirdQuartile|complete|skip"`, `ClickTracking`

### Utilities

| Method | Purpose |
|--------|---------|
| `IsWrapperVAST(xml)` | Detect wrapper ad |
| `ExtractVASTAdTagURI(xml)` | Get wrapped tag URL |
| `FireTrackingUrl(url)` | HTTP ping (fire-and-forget) |
| `FireTrackingUrls(list)` | Batch ping |
| `TestDefaultVAST()` | Internal test helper |

### Logging

Success log includes: video URL, duration, skipOffset, preview URLs.

---

## Wrapper VAST (VideoAdView)

Handled in `LoadVideoAdCoroutine`:

1. Detect wrapper
2. Fetch `VASTAdTagURI`
3. Merge wrapper-level `Impression` URLs into `_vastData`
4. Recurse (max depth 5)
5. Parse final inline VAST

---

## AdResponse DTOs

Файл: `Runtime/BidscubeSDK/Core/AdResponse.cs`

JSON deserialization types for server responses (where `JsonUtility` used).

---

## Debug logging

`URLBuilder` and select VideoAdView paths write `AgentNdjsonDebugLog` entries (hypothesis tags H1, H3, etc.) — internal only.
