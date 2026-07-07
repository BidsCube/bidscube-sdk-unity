# Native ads

Файл: `Runtime/BidscubeSDK/Views/NativeAdView.cs`

---

## Overview

Native ads завантажують OpenRTB-style native markup з SSP:

- Request: `c=n`, `res=json` (див. `URLBuilder`)
- Response: JSON з `adm` containing native assets або HTML fallback

---

## Rendering modes

### Default: WebView (`_useWebViewRendering = true`)

1. Parse native JSON (`assets`, `link`, `imptrackers`)
2. Generate HTML template з title, description, image, CTA
3. Load через `WebViewController`
4. Click → `link.url` + tracking

### Fallback: Unity UI

Якщо WebView path fails або disabled:

- `Image` для icon/main image
- `Text` для title/body
- `Button` install CTA
- `OnInstallButtonClicked(placementId, buttonText)`

---

## Size defaults

`AdSizeSettings.defaultNativeSize` — default **728×400** logical pixels (v1.2.5+).

`ApplyAdSizeSettings()` at load:

- `preferDefaultsOverAdm = false` — server-reported sizes win
- `preferDefaultsOverAdm = true` — force defaults from asset

Create asset: **Assets → Create → BidscubeSDK → Ad Size Settings**

---

## OpenRTB parsing

Uses `AdMarkupExtractor` + inline JSON parsing in `NativeAdView`:

- `assets[]` — title, img, data
- `link.url` — click-through
- `imptrackers[]` — impression pixels

HTML adm fallback — treated like banner HTML path.

---

## Position

Same priority as other ad types: manual > server > default.

Native typically renders in center or slot per `AdPosition`.

---

## Callbacks

Standard `IAdCallback` lifecycle.

Install button: `OnInstallButtonClicked` (native CTA).

---

## API

```csharp
BidscubeSDK.ShowNativeAd("20214", callback);
BidscubeSDK.GetNativeAdView("20214", callback); // legacy standalone path
```

Default test placement in SDK Test Scene: **`20214`**.

---

## IAdRenderOverride

Return `true` from `OnAdRenderOverride` with `AdType.Native` to render adm yourself (custom native UI kit).
