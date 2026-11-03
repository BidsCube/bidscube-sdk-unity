# Unity: інструкція — `location` / `referrer` для Banner / Video / Native

## Мета

Перевірити в **поточних SDK-шляхах** Unity (`com.bidscube.sdk`), без хардкоду WebView `baseUrl`:

- `window.location.href`
- `window.location.hostname`
- `document.referrer`

Формат звіту — як на Android / iOS / Flutter:

```
Баннер:
window.location.href = …
hostname = …
document.referrer = …

Video companion:
…

Native:
…
```

---

## Важливо: Unity ≠ нативний Android SDK для рендеру

| Що | Де |
|----|-----|
| **Ad API** | `SDKConfig.BaseURL` / `Constants.BaseURL` (`https://ssp-bcc-ads.com/sdk`) |
| **WebView рендер** | C# → `WebViewObject` (gree unity-webview plugin) |
| **Android Java SDK** | Синхронізується при `BidscubeSDK.Initialize()` для MAX/mediation, **але banner/video/native у Unity-app рендеряться через C# WebView** |

> **`SDKConfig.BaseURL`** — це URL **ad API**, не WebView base.  
> У всіх поточних SDK-шляхах передається **`baseUrl: ""`** (порожній рядок).

---

## Поточні Unity SDK-шляхи

| Формат | Шлях | WebView | `LoadHTML` baseUrl |
|--------|------|---------|-------------------|
| **Banner / Image** | `BidscubeSDK.GetBannerAdView()` → `BannerAdView` → `WebViewController` | так | `""` |
| **Video companion (HTML)** | `VideoAdView.ShowCompanionWebView()` → `WebViewObject` | так | `""` |
| **Native** | `NativeAdView` → `BuildNativeHtml()` → `WebViewController` | **так** (за замовч.) | `""` |
| **Playable** | Окремого playable WebView у Unity SDK **немає** | N/A | — |

### Файли SDK

| Формат | Файл | Виклик |
|--------|------|--------|
| Banner / Image | `Runtime/BidscubeSDK/Views/BannerAdView.cs` | `_webViewController.LoadHTML(fullHTML, "");` |
| Video companion HTML | `Runtime/BidscubeSDK/Views/VideoAdView.cs` | `_companionWebView.LoadHTML(html, "");` |
| Native | `Runtime/BidscubeSDK/Views/NativeAdView.cs` | `_webViewController.LoadHTML(html, "");` (`_useWebViewRendering = true`) |
| WebView host | `Runtime/BidscubeSDK/Controllers/WebViewController.cs` | `_webViewObject.LoadHTML(wrappedHtml, baseUrl)` |

Під капотом (unity-webview):

- **Android:** `WebViewPlugin.LoadHTML(html, baseUrl)` → при `""` — аналог `loadDataWithBaseURL(null, …)`
- **iOS:** `loadHTMLString:baseURL:` з `NSURL` з порожнього рядка → **`nil`**

---

## Відмінність від Flutter

| Формат | Flutter-only | Unity |
|--------|--------------|-------|
| Native | `FlutterNativeAdView` — **без WebView** | `NativeAdView` — **WebView з HTML-шаблону** |
| Очікування Native | «немає WebView — N/A» | **той самий probe**, очікування `about:blank` / `""` / `""` |

---

## Що виміряти (JS)

Після завантаження сторінки (`CallOnLoaded` / `ld` callback у `WebViewObject.Init`, не одразу після `LoadHTML`):

```js
(function(){
  return JSON.stringify({
    href: String(location.href),
    hostname: String(location.hostname),
    protocol: String(location.protocol),
    referrer: String(document.referrer),
    checkPixel: 'https://da-dsp.com/?t=check&u=' + encodeURIComponent(location.hostname)
  });
})();
```

### Як отримати результат у Unity

`WebViewObject.EvaluateJS()` **не повертає** значення в C# напряму. Варіанти:

**A. Через `unity:` protocol (рекомендовано)** — SDK вже використовує `unity:click:`:

```js
(function(){
  var payload = JSON.stringify({
    href: String(location.href),
    hostname: String(location.hostname),
    protocol: String(location.protocol),
    referrer: String(document.referrer),
    checkPixel: 'https://da-dsp.com/?t=check&u=' + encodeURIComponent(location.hostname)
  });
  window.location.href = 'unity:probe:' + encodeURIComponent(payload);
})();
```

У `WebViewObject.Init(cb: ...)` або `WebViewController` обробити повідомлення з префіксом `probe:` і залогувати `PROBE: …`.

**B. Offline probe runner** — окремий `GameObject` + `WebViewObject`, fixtures з iOS `WebViewJsProbeFixtures.swift` / Flutter `webview_js_probe_fixtures.dart`, probe в `ld` callback (аналог iOS `SdkLocationProbeRunner`).

**C. Логи платформи** — `console.log` у fixture + `adb logcat` / Xcode console (менш зручно).

Затримка після `CallOnLoaded`: **~50–100 ms** (як у iOS runner).

---

## Тестові HTML (offline)

Скопіювати fixtures з:

- iOS: `ios/bidscube-sdk-ios/testApp-ios/WebViewJsProbeFixtures.swift` (`simple_320x50`, `dsp_wrapper_location_check`)
- Flutter: `flutter/bidscube-testapp-flutter/lib/core/webview_js_probe_fixtures.dart`

Використовуйте **data URI / SVG**, щоб DNS не блокував завантаження.

### Video companion HTML

VAST з `HTMLResource` (offline SVG у companion) — див. Flutter `SdkLocationProbeFixtures.vastWithHtmlCompanion()` або додайте локальний VAST у `SDKTestScene` (там уже є `LocalVastWithPreviewXml` — замініть `StaticResource` на `HTMLResource` для QA).

Після завершення відео → `ShowCompanionWebView()` → probe на `_companionWebView`.

### Native

OpenRTB JSON → `NativeAdView` будує HTML через `BuildNativeHtml()` і вантажить у WebView.  
Probe на тому ж `WebViewController`, що й banner.

---

## Кейси

### 1) Banner (обов’язково)

- `BidscubeSDK.Initialize(config)`
- `BidscubeSDK.GetBannerAdView("20212", callback)` або offline HTML через probe `WebViewObject`
- Fixture: DSP wrapper з `hostname` check pixel
- Probe після `CallOnLoaded` (URL зазвичай `about:blank`)

### 2) Video companion HTML (обов’язково)

- Потрібен **`BidscubeAndroidFeatureSet.FullWithVideo`** (за замовч. `LiteNoVideo` **вимикає** video API)
- Editor: `Bidscube/Android Export Settings` → `FullWithVideo`
- VAST з `HTMLResource` companion
- Дочекатися end card → probe на companion WebView

### 3) Native (обов’язково)

- `BidscubeSDK.GetNativeAdView("20214", callback)` або offline OpenRTB JSON
- **Очікується WebView** (`NativeWebViewHost` у hierarchy)
- Probe `location` / `referrer` — **не** «N/A», як у Flutter

### 4) Порівняння baseUrl (опційно)

| Режим | `LoadHTML(html, baseUrl)` |
|--------|---------------------------|
| SDK (поточний) | `""` |
| Старий Android | `"https://adcontent.local/"` |
| about:blank | `"about:blank"` |
| publisher | `"https://publisher.example/"` |

---

## Запуск

### Test scene

1. Відкрити `Samples/SDK Demo/Scenes/SDK Test Scene.unity`  
   або test app: `unity/test/bidscube-testapp-unity-master/Assets/Scenes/SDK Test Scene.unity`
2. **File → Build Settings** → Android або iOS
3. Зібрати і запустити на **емуляторі/девайсі** (не Unity Editor на Windows — WebView там не підтримується)

### Android

```bash
# Після build & run з Unity, або:
adb logcat -s Unity | grep -E 'PROBE|SdkLocationProbe|WebViewController'
```

### Placements (live)

| Формат | Placement |
|--------|-----------|
| Banner | `20212` |
| Native | `20214` |
| Video | live placement або локальний VAST у SDK Test Scene |

### Ініціалізація (приклад)

```csharp
var config = new SDKConfig.Builder()
    .BaseURL(Constants.BaseURL)
    .EnableLogging(true)
    .EnableDebugMode(true)
    .DefaultAdTimeout(30000)
    .Build();

BidscubeSDK.Initialize(config);
```

---

## Формат звіту

```
Баннер:
window.location.href = …
window.location.origin = …
hostname = …
document.referrer = …
localStorage = …
sessionStorage = …
cookieWritable = …

Video companion:
…

Native:
…
```

---

## Орієнтир (паритет з Android/iOS/Flutter після `baseURL=null` / `""`)

```
Баннер:
window.location.href = about:blank
window.location.origin = null   # (або "null" / "about:blank" залежно від WebView)
hostname = ""
document.referrer = ""
localStorage = blocked:SecurityError   # opaque origin
sessionStorage = blocked:SecurityError
cookieWritable = false

Video companion:
(те саме)

Native (Unity WebView):
(те саме що Banner)

Native (iOS UIKit):
немає WebView — N/A
```

Якщо значення інші — зафіксувати платформу (Android/iOS), фактичний `baseUrl` у `LoadHTML` і URL з `CallOnLoaded`.

---

## Чеклист перед здачею звіту

- [ ] Збірка на **реальному Android/iOS**, не Editor Windows
- [ ] Для video: `FullWithVideo` у Android export settings
- [ ] Banner probe після `CallOnLoaded` / `about:blank`
- [ ] Video companion — саме **HTMLResource**, не StaticResource
- [ ] Native: підтверджено наявність `WebViewObject` / `WebViewController` у hierarchy
- [ ] Не плутати `SDKConfig.BaseURL` (API) з WebView `baseUrl`
- [ ] Зафіксовано SDK version (`Constants.SdkVersion`, зараз **1.2.17**)

---

## Референси

| Платформа | Probe runner / fixtures |
|-----------|-------------------------|
| iOS testApp | `SdkLocationProbeRunner.swift`, `WebViewJsProbeFixtures.swift` |
| Flutter testApp | `SdkLocationProbeRunner`, `WebViewLocationProbeView` |
| Unity | поки **немає** готового runner — рекомендовано порт iOS runner у `Samples/SDK Demo/QA/` |

---

**Фактичний результат на Flutter Android (емулятор, offline probe):** `about:blank`, `""`, `""` для banner і video companion — Unity має дати той самий результат при `LoadHTML(..., "")`.

Якщо потрібно — можна додати `SdkLocationProbeRunner.cs` + кнопку в SDK Test Scene окремим завданням.
