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
