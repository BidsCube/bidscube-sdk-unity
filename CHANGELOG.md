## [Unreleased]

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
