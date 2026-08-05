# Індекс ключових файлів

---

## Root

| File | Description |
|------|-------------|
| `package.json` | UPM manifest: name, version, Unity min, dependencies |
| `README.md` | Public SDK overview |
| `INTEGRATION.md` | Public integration guide + Git URL pins |
| `CHANGELOG.md` | Version history |
| `RELEASE_CHECKLIST.md` | Pre-release verification |
| `LICENSE.md` | License |
| `.github/workflows/release.yml` | Tag → GitHub Release CI |

---

## Core API

| File | Description |
|------|-------------|
| `Runtime/BidscubeSDK/BidscubeSDK.cs` | Static public API entry point |
| `Runtime/BidscubeSDK/BidscubeSDK.asmdef` | Main runtime assembly definition |
| `Runtime/BidscubeSDK/SdkIntegrationContext.cs` | Sample scene LevelPlay mode switch |

---

## Controllers

| File | Description |
|------|-------------|
| `Controllers/AdViewController.cs` | Central ad orchestrator: canvas, layout, type dispatch |
| `Controllers/WebViewController.cs` | HTML WebView + margin sync |
| `Controllers/NewWebViewController.cs` | Alternate WebView host for samples |
| `Controllers/SDKTestScene.cs` | Primary QA scene + local VAST cases |
| `Controllers/BidscubeExampleScene.cs` | Full example + integration bar |
| `Controllers/WindowedAdTestScene.cs` | Windowed layout tests |
| `Controllers/ConsentTestScene.cs` | Consent stub tests |
| `Controllers/SceneManager.cs` | Inter-scene navigation |

---

## Views

| File | Description |
|------|-------------|
| `Views/BannerAdView.cs` | Image/banner WebView + adm unwrapping |
| `Views/VideoAdView.cs` | VAST/video, skip, end card, reward logic |
| `Views/NativeAdView.cs` | OpenRTB native parse + WebView/UI |

---

## Core

| File | Description |
|------|-------------|
| `Core/Callbacks.cs` | IAdCallback, IRewardedAdCallback, IAdRenderOverride, consent |
| `Core/SDKConfig.cs` | SDK configuration builder |
| `Core/Constants.cs` | URLs, timeouts, error codes |
| `Core/AdType.cs` | Image, Video, Native |
| `Core/AdPosition.cs` | Position enum |
| `Core/VideoAdFormat.cs` | Interstitial, Rewarded |
| `Core/VASTParser.cs` | VAST 2/3/4 XML parser + Companion (HTML/IFrame/Static) + tracking |
| `Core/VideoSessionEndPolicy.cs` | `AutoClose` / Companion post-video action policy |
| `Core/IMAVideoPlayer.cs` | IMA JNI wrapper (disabled path) |
| `Core/IVideoPlayerEventListener.cs` | Unified video events for IMA bridge |
| `Core/AdResponse.cs` | JSON response DTOs |
| `Core/Logger.cs` | Conditional SDK logging |

---

## Networking

| File | Description |
|------|-------------|
| `Networking/URLBuilder.cs` | Ad request URL (iOS-aligned) |
| `Networking/DeviceInfo.cs` | Device/app metadata for requests |
| `Networking/AdMarkupExtractor.cs` | OpenRTB/flat JSON adm extraction |
| `Networking/WebViewObject.cs` | Native WebView C# binding |
| `Networking/NetworkManager.cs` | HTTP helpers |

---

## Settings

| File | Description |
|------|-------------|
| `Settings/AdSizeSettings.cs` | ScriptableObject default ad sizes |
| `Settings/DefaultAdSizeSettings.asset` | Bundled defaults |

---

## Android runtime

| File | Description |
|------|-------------|
| `Android/BidscubeAndroidFeatureSet.cs` | Feature set + dependency mode enums |
| `Android/BidscubeAndroidExportSettings.cs` | ScriptableObject export pinning |
| `Android/AndroidBuildDefines.cs` | LiteNoVideo symbol constant |
| `Android/BidscubeLiteVideoGuard.cs` | Runtime Lite video block helper |
| `Android/BidscubeSDK.Android.asmdef` | Android types assembly |

---

## Android Editor

| File | Description |
|------|-------------|
| `Editor/Android/BidscubeAndroidGradleProjectPatcher.cs` | Gradle/AAR injection on export |
| `Editor/BidscubeDefineApplicator.cs` | Sync BIDSCUBE_ANDROID_LITE_NO_VIDEO |
| `Editor/BidscubeAndroidExportSettingsResolver.cs` | Resolve effective export settings |
| `Editor/BidscubeAndroidFeatureSetStore.cs` | EditorPrefs feature set fallback |
| `Editor/BidscubeAndroidScriptingDefinesPreprocessor.cs` | Preprocessor define hook |
| `Editor/BidscubeScriptingDefineSync.cs` | Define sync utility |
| `Editor/BidscubeVideoDefineBootstrap.cs` | Video define bootstrap |
| `Editor/BidscubeSDK.Android.Editor.asmdef` | Editor assembly |

---

## Samples

| File | Description |
|------|-------------|
| `BasicIntegration/AdExample.cs` | Minimal integration sample |
| `Scenes/*.unity` | Five test/example scenes |

---

## Debug

| File | Description |
|------|-------------|
| `Debug/AgentNdjsonDebugLog.cs` | Internal NDJSON debug telemetry |

---

## OpenRTB module

| Path | Description |
|------|-------------|
| `OpenRTB/VideoAdPayloadResolver.cs` | Resolve HTTP response → `VideoPlaybackPlan` |
| `OpenRTB/OpenRtbJson.cs` | Lightweight JSON parser |
| `OpenRTB/OpenRtbPodModels.cs` | Pod/slot DTOs |
| `OpenRTB/OpenRtbVideoObjectParser.cs` | Parse `openrtb.video` object |
| `OpenRTB/OpenRtbPoddedResponseNormalizer.cs` | OpenRTB pod response normalizer |
| `OpenRTB/PoddedPlaybackPlanBuilder.cs` | Build sequential playback plan |
| `OpenRTB/VastAdSequenceParser.cs` | Split multi-`<Ad>` VAST |
| `OpenRTB/VastAdTagJsonPlanLoader.cs` | Nested JSON plan load mode for VAST ad tag URLs |
| `OpenRTB/OpenRtbVideoUrlHelper.cs` | Direct video vs VAST ad tag URL classification |
| `OpenRTB/OpenRtbBidRequestBuilder.cs` | Placeholder (POST bid request not implemented) |
| `Tests/EditMode/` | OpenRTB EditMode unit tests |

---

## Native plugins

| Path | Description |
|------|-------------|
| `Runtime/Plugins/iOS/WebView.mm` | iOS WKWebView |
| `Runtime/Plugins/iOS/WebViewWithUIWebView.mm` | Legacy UIWebView |
| `Runtime/Plugins/Android/*.aar.tmpl` | Android WebView AAR templates |
| `Runtime/Plugins/WebView.bundle` | macOS Editor WebView |
| `Runtime/Plugins/WebGL/*.jslib` | WebGL WebView |
| `Runtime/Plugins/Editor/UnityWebViewPostprocessBuild.cs` | iOS/macOS post-process |

---

## Scripts

| File | Description |
|------|-------------|
| `scripts/copy-to-runtime.ps1` | Package layout sync for unitypackage |

---

## Internal doc (this folder)

| File | Description |
|------|-------------|
| `doc/README.md` | **Головний індекс** — повний зміст, навігація за ролями |
| `doc/overview.md` | Product overview, UPM, repo structure |
| `doc/architecture.md` | Architecture flows, OpenRTB pod diagram |
| `doc/public-api.md` | Full BidscubeSDK API |
| `doc/openrtb.md` | **OpenRTB 2.6 module** — повний reference |
| `doc/video-ads.md` | Video/VAST/end card/pods |
| `doc/banner-and-webview.md` | Banner + WebView |
| `doc/native-ads.md` | Native ads |
| `doc/networking-vast.md` | URLBuilder + VASTParser |
| `doc/configuration.md` | SDKConfig, AdSizeSettings, OpenRTB options |
| `doc/callbacks-and-errors.md` | Callbacks, pod lifecycle, error codes |
| `doc/android.md` | Android feature sets, Gradle |
| `doc/ios-and-plugins.md` | iOS/plugins |
| `doc/test-scenes-qa.md` | QA scenes, VAST + OpenRTB pod checklists |
| `doc/editmode-tests.md` | Unit tests, запуск, матриця |
| `doc/packaging.md` | UPM, git archive, unitypackage |
| `doc/troubleshooting.md` | FAQ, типові проблеми |
| `doc/release-process.md` | Release workflow |
| `doc/integration-modes.md` | SdkIntegrationContext |
| `doc/known-issues.md` | Limitations + tech debt |
| `doc/file-index.md` | Цей файл — індекс коду |
