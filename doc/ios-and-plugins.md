# iOS та native plugins

---

## Bidscube-specific iOS ad SDK

**Немає.** Video на iOS — Unity `VideoPlayer`. Banners/native — **unity-webview** plugins.

---

## WebView plugins (iOS)

| File | Role |
|------|------|
| `Runtime/Plugins/iOS/WebView.mm` | WKWebView (iOS 9+) — **primary** |
| `Runtime/Plugins/iOS/WebViewWithUIWebView.mm` | Legacy UIWebView fallback |
| `Runtime/Plugins/Editor/UnityWebViewPostprocessBuild.cs` | Xcode post-process |

### Requirements

- iOS **12.0+** (README)
- WKWebView for in-app HTML ads

---

## Other platforms

| Platform | Plugin |
|----------|--------|
| **Android** | `WebViewPlugin-*.aar.tmpl`, AndroidX core AAR templates |
| **macOS Editor** | `Runtime/Plugins/WebView.bundle` — Editor WebView preview |
| **WebGL** | `unity-webview-webgl-plugin.jslib` |

---

## Video on iOS

- Unity `VideoPlayer` + RenderTexture → RawImage
- VAST parsed in C# (`VASTParser`)
- Progressive MP4 preferred (avoid HLS in Unity player)
- IMA iOS stubs exist in `IMAVideoPlayer.cs` but **production path disabled**

---

## Build post-processing

`UnityWebViewPostprocessBuild.cs`:

- Modifies generated Xcode project
- Links WebView native code
- Run automatically on iOS/macOS build

---

## iOS integration notes

1. Ensure WebView `.meta` files restrict plugins to **iPhone** where required
2. No duplicate frameworks from mediation adapters in **core** package
3. Test banner + video on physical device (simulator WebView limitations)

---

## DeviceInfo on iOS

- `AdvertisingIdentifier` — IDFA when available / ATT context
- User-Agent includes Bidscube prefix for ad requests

Privacy keys (NSUserTrackingUsageDescription, etc.) — **host app responsibility**, not in core SDK.

---

## Verification checklist

- [ ] Banner HTML renders in WKWebView
- [ ] Native ad WebView template renders
- [ ] Interstitial video plays (MP4 URL from VAST)
- [ ] End card + skip work after complete/skip
- [ ] No Xcode duplicate symbol errors from overlapping adapter SDKs
