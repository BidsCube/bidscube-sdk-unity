# Banner / Image ads та WebView

## BannerAdView

Файл: `Runtime/BidscubeSDK/Views/BannerAdView.cs`

### Flow

1. `URLBuilder.BuildAdRequestURL` — `c=b`, `res=js`
2. HTTP GET → response body
3. `IAdRenderOverride` check
4. Unwrap nested JSON `{"adm":"..."}` layers (може бути кілька рівнів)
5. `WebViewController` loads HTML
6. Margin sync: WebView native rect ↔ Unity RectTransform
7. Callbacks: `OnAdLoading` → `OnAdLoaded` → `OnAdDisplayed`

### Position-specific behavior

| Position | Layout |
|----------|--------|
| `Header` / `Footer` | Clamped height, full width |
| `Sidebar` | Fixed width sidebar |
| `FullScreen` | Full canvas |
| Embedded slot (`useLayoutSlotSizing`) | HTML `flex-start` alignment |

### Banner tracking

`BidscubeSDK` tracks active banners in `_activeBanners` for `RemoveAllBanners()` / `GetActiveBannerCount()`.

---

## WebViewController

Файл: `Runtime/BidscubeSDK/Controllers/WebViewController.cs`

Primary WebView host для banner і native.

### Responsibilities

- Wraps `WebViewObject` (native plugin binding)
- Margin calculation для Screen Space Overlay canvas
- Fullscreen native mode toggle
- `FindBestCanvasFallback()` — якщо primary canvas invalid після layout change
- `ReapplyLayout()` — sync після `BidscubeSDK.ReapplyLayoutForAllActiveAds()`

### NewWebViewController

Файл: `Runtime/BidscubeSDK/Controllers/NewWebViewController.cs`

Alternate controller для custom render samples і SDK Test Scene image path (`ShowImageAdWithWebView`).

---

## WebViewObject (native bridge)

Файл: `Runtime/BidscubeSDK/Networking/WebViewObject.cs`

Low-level C# ↔ native WebView:

- `Init(...)`, `LoadURL`, `LoadHTML`, margin setters
- Platform-specific implementations через plugins

---

## HTML adm handling

Server може повернути:

- Full HTML document
- HTML fragment (SDK wraps у template)
- JSON envelope з `adm` field

`BannerAdView` unwraps JSON recursively до чистого HTML/VAST.

---

## Custom banner size

```csharp
BidscubeSDK.ShowCustomBanner(placementId, AdPosition.Unknown, width, height, callback);
```

Explicit `RectTransform.sizeDelta` на controller root.

---

## Corner radius

Test scenes expose `_bannerCornerRadius` — passed to WebView HTML template styling where supported.

---

## IAdRenderOverride для banner

```csharp
public bool OnAdRenderOverride(string placementId, string adm, AdType adType, int position)
{
    if (adType == AdType.Image) {
        // custom render adm yourself
        return true; // SDK skips WebView
    }
    return false;
}
```

Див. Custom Ad Render Scene.

---

## Native WebView plugins

Див. [ios-and-plugins.md](ios-and-plugins.md):

- Android: `WebViewPlugin-*.aar.tmpl`, `core-1.6.0.aar.tmpl`
- iOS: `WebView.mm` (WKWebView)
- macOS Editor: `WebView.bundle`
- WebGL: `unity-webview-webgl-plugin.jslib`

Post-process: `Runtime/Plugins/Editor/UnityWebViewPostprocessBuild.cs`
