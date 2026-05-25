## [Unreleased]

---

## [1.2.12] - 2026-04-30

### Added

- **Video contracts:** `ShowInterstitialVideoAd`, `ShowRewardedVideoAd`, `GetInterstitialVideoAdView`, `GetRewardedVideoAdView`; `VideoAdFormat` enum; optional `IRewardedAdCallback.OnUserRewarded` (backward-compatible, not on `IAdCallback`).
- `ShowVideoAd` / `GetVideoAdView` remain aliases for interstitial video.

### Changed

- **Video lifecycle:** Removed fake delayed video callbacks (`DelayedVideoAdLoaded`, `LoadVideoAd` success stubs). `VideoAdView` uses guarded `Notify*` methods — `OnAdLoaded` on prepare, `OnAdDisplayed` / `OnVideoAdStarted` on playback start, reward only after real completion for `VideoAdFormat.Rewarded`.
- **Android LiteNoVideo:** interstitial/rewarded video entry points also respect **`BIDSCUBE_ANDROID_LITE_NO_VIDEO`** via `TryRejectDirectVideoInLiteNoVideo`.
- IMA path disabled until event bridge is wired; custom VAST / `VideoPlayer` is the reliable path.

---

## [1.2.11] - 2026-05-12

### Added

- **Android:** expanded shared export/runtime configuration to the four release-ready feature sets **`LiteNoVideo`**, **`WebViewVideoNoDesugar`**, **`LegacyMediaVideoNoDesugar`**, and **`FullWithVideo`**.

### Changed

- **Android Gradle export:** bundled core artifact selection, Maven coordinates, and desugaring policy now align with the selected feature set instead of a simple Lite/Full split.

---

## [1.2.10] - 2026-05-07

### Fixed

- **UPM:** valid **UUID v4** `guid` in **`AndroidBuildDefines.cs.meta`** (YAML-safe, Unity 6 package resolution / player builds).
- **UPM:** removed orphan **`BidscubeSDK-unity.unitypackage.meta`** (no tracked `.unitypackage`; avoids immutable PackageCache warnings).

---

## [1.2.9] - 2026-05-06

### Added

- **Android (runtime):** separate assembly **`BidscubeSDK.Android`** (`Runtime/BidscubeSDK/Android/BidscubeSDK.Android.asmdef`) so host Editor assemblies (e.g. mediation adapters) can reference **`BidscubeSDK.Android`** explicitly alongside **`BidscubeSDK`** / **`BidscubeSDK.Android.Editor`**.

### Fixed

- **Editor (Android Gradle):** **`BidscubeAndroidGradleProjectPatcher`** — avoid **`Regex.Replace(..., int count)`** on some Unity / .NET profiles (CS1503); use first-match replacement via **`Match`** / **`Match.Result`**.
- **Editor:** **`BidscubeDefineApplicator`** — use the literal Android Lite define string (stays in sync with **`AndroidBuildDefines.LiteNoVideoSymbol`**) for stable cross-assembly compilation.

---

## [1.2.8] - 2026-05-06

### Added

- **Android (editor + runtime):** shared **`BidscubeAndroidExportSettings`**, **`BidscubeAndroidFeatureSet`**, scripting-define sync (**`BIDSCUBE_ANDROID_LITE_NO_VIDEO`**), and **`BidscubeAndroidGradleProjectPatcher`** so **`com.bidscube.applovin.max`** and **`com.bidscube.levelplay`** can apply the same Lite / Full Gradle rules. Lite uses bundled **`sdk-lite-no-video`** AAR naming; Full uses **`sdk-full-video`** + optional launcher desugaring injection.

### Changed

- **`BidscubeLiteVideoGuard`** message text (no AppLovin-specific wording).

---

## [1.2.7] - 2026-05-06

### Fixed

- **Build:** removed duplicate `BidscubeSDK.ReapplyLayoutForAllActiveAds` definition (merge artifact) that caused **CS0111** when compiling the package.

---

## [1.2.6] - 2026-05-06

### Fixed

- **Android LiteNoVideo:** `ShowVideoAd`, `GetVideoAdView`, and `AdViewController` video init now respect **`BIDSCUBE_ANDROID_LITE_NO_VIDEO`** (set by `com.bidscube.applovin.max` for Lite builds). Direct Unity video no longer starts when the native graph omits IMA/Media3; callbacks receive **`LiteNoVideoVideoNotSupported` (1006)**.

---

## [1.2.5] - 2026-04-30

### Changed

- **Banners / image:** `GetBannerAdView` now builds the same path as `ShowImageAd` via `AdViewController` (load inside `CreateImageAdView`); returns the creative root from `GetAdViewGameObject()` or the controller `GameObject`, and `null` if the SDK is not initialized.
- **WebView:** `UpdateWebViewMargins` uses `FindBestCanvasFallback()` (highest `sortingOrder` among active canvases) when the webview is not under a `Canvas` in the hierarchy.
- **Banner HTML:** In `LoadAdContent`, embedded layout slots (`AdViewsParentUsesLayoutSlotSizing`) use `flex-start` for vertical alignment; footer without a slot still uses `flex-end`.
- **Video:** Full-screen black `VideoBackdrop` behind the `VideoTexture`; sibling order: backdrop, video, then skip/close on top.
- **Native / defaults:** `LogicalHeight` and `AdSizeSettings.defaultNativeSize` default height increased to 400; native template CSS `min-height` for vertical/fullscreen image regions adjusted accordingly.

## [1.2.4] - 2026-04-29

### Added

- GitHub Actions workflow to create a GitHub Release when a version tag (`v*`) is pushed (tag must match `package.json`).

## [1.2.3] - 2026-04-29

### Changed

- Android: video preparation tries a local `file://` cache sooner when HTTPS streaming is unreliable (NuCachedSource2).

## [1.2.1] - 2025-12-11

### Added

- AdSizeSettings ScriptableObject (Assets/Settings) to configure default ad sizes per AdType via the editor.
- Optional IAdRenderOverride callback to allow consumers to fully handle rendering (placementId, adm JSON, AdType, position).

### Fixed

- Improved the copy-to-runtime tooling to compute repo root reliably and to support dry-run, flattening, and excluding the Editor folder by default.

## [1.1.0] - 2025-11-25

### Changed
- Refactored AdViewController 
- Improved BannerAdView and VideoAdView to support custom UI elements via inspector
- Updated sample scenes to demonstrate new customization options
### Fixed

- Various fixes to native ad parsing and banner sizing logic that previously caused oversized or clipped native/banner views on some layouts.
- Resolved several editor-only compilation warnings and cleaned up sample scene wiring.


## [0.2.2] - 2025-01-21

### Changed

- Removed hardcoded UI elements from AdViewController, BannerAdView, and VideoAdView
- Added support for custom GameObjects and prefabs via inspector
- Removed WebViewObjectBG background GameObject creation
- Improved Unicode text handling in native ads (German text support)
- Cleaned up Documentation~ folder to only include README.md

### Fixed

- Fixed Unicode encoding issues in native ads (German characters now display correctly)
- Removed unnecessary WebViewObjectBG GameObject spawning
- Removed loading label and WebViewHost from BannerAdView

### Added

- GitHub Actions workflow for automatic Assets to Runtime sync on release

## [0.2.0] - 2025-11-20

### Added

- Fixed problems with integration and compatability
- Fixed error of parsing each type of ad
- Improved [position] handling and added boundaries to the banners and native ads

## [0.1.0] - 2025-10-13

### Added

- Initial public release of the Bidscube Unity SDK.
- Support for Image, Video, Native, and Banner ads.
- Consent management helpers for GDPR/CCPA.
- Banner positioning helpers (header, footer, custom position).
- Basic error handling and logging hooks.
- Unity Package Manager distribution via Git URL:
  - `https://github.com/BidsCube/bidscube-sdk-unity.git`
- Integration examples and quick-start guide in `README.md`
