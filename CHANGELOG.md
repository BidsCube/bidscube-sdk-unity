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
- Integration examples and quick-start guide in `README.md`.
