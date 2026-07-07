# Android

---

## Feature sets

Файл: `Runtime/BidscubeSDK/Android/BidscubeAndroidFeatureSet.cs`

```csharp
public enum BidscubeAndroidFeatureSet
{
    LiteNoVideo = 0,              // default
    WebViewVideoNoDesugar = 1,
    LegacyMediaVideoNoDesugar = 2,
    FullWithVideo = 3
}
```

| Feature set | Native AAR | Unity direct video | Media3/IMA | Desugaring |
|-------------|------------|-------------------|------------|------------|
| **LiteNoVideo** | `sdk-lite-no-video` | **Blocked** (`1006`) | No | Stripped |
| **WebViewVideoNoDesugar** | `sdk-webview-video` | HTML5 via WebView | No | Stripped |
| **LegacyMediaVideoNoDesugar** | `sdk-legacy-media-video` | VideoView/MediaPlayer | No | Stripped |
| **FullWithVideo** | `sdk-full-video` or Maven | **Yes** | Yes | `desugar_jdk_libs:2.0.4` in launcher |

---

## LiteNoVideo compile symbol

| | |
|---|---|
| Symbol | `BIDSCUBE_ANDROID_LITE_NO_VIDEO` |
| Constant | `AndroidBuildDefines.LiteNoVideoSymbol` |
| Applied by | `BidscubeDefineApplicator` when feature set = LiteNoVideo |

### Guard locations

1. `BidscubeSDK.TryRejectDirectVideoInLiteNoVideo` — all video public API
2. `AdViewController.Initialize` — video init blocked
3. `BidscubeLiteVideoGuard` — runtime helper / messaging

When symbol active:

```csharp
callback.OnAdFailed(placementId, 1006, ErrorMessages.LiteNoVideoVideoNotSupported);
```

**Mediation video** (MAX adapter) — separate path, not blocked by this guard.

---

## Export settings asset

**Assets → Create → Bidscube → Android Export Settings**

File: `BidscubeAndroidExportSettings.cs`

| Field | Purpose |
|-------|---------|
| `featureSet` | Which AAR graph to inject |
| `coreDependencyMode` | Bundled AAR vs Maven vs custom Gradle |
| `customCoreImplementationGradleLines` | Custom `implementation` lines |
| `forceCompileSdk` / `forceMinSdk` | Optional SDK version override |
| `enableDesugaring` | Legacy field; behavior driven by feature set |

Commit asset to repo for CI/team parity.

---

## Core dependency modes

```csharp
public enum BidscubeAndroidCoreDependencyMode
{
    BundledUnityLibraryLibsAar = 0,
    MavenBidscubeSdkAar = 1,
    CustomGradleLines = 2,
    SkipInjectionIntegratorOwnsCore = 3
}
```

---

## Gradle patcher

File: `Editor/Android/BidscubeAndroidGradleProjectPatcher.cs`

Runs on **`IPostGenerateGradleAndroidProject`**:

1. Resolve effective settings (`BidscubeAndroidExportSettingsResolver`)
2. Copy selected AAR → `unityLibrary/libs/`
3. Inject managed Gradle block into `unityLibrary/build.gradle`
4. FullWithVideo: ensure launcher desugaring deps

### Resolution order

1. `BidscubeAndroidExportSettings` asset in project
2. `BidscubeAndroidFeatureSetStore` (EditorPrefs fallback)

---

## Editor tooling

| File | Role |
|------|------|
| `BidscubeDefineApplicator.cs` | Sync `BIDSCUBE_ANDROID_LITE_NO_VIDEO` scripting define |
| `BidscubeAndroidScriptingDefinesPreprocessor.cs` | Preprocessor hook |
| `BidscubeScriptingDefineSync.cs` | Define sync utility |
| `BidscubeVideoDefineBootstrap.cs` | Video-related define bootstrap |
| `BidscubeAndroidExportSettingsResolver.cs` | Resolve export config |
| `BidscubeAndroidFeatureSetStore.cs` | EditorPrefs feature set storage |

---

## Assembly split

| Assembly | Referenced by |
|----------|---------------|
| `BidscubeSDK.Android` | Main runtime, **mediation adapters** |
| `BidscubeSDK.Android.Editor` | Editor only — Gradle patcher |

Adapters (`com.bidscube.applovin.max`) reference `BidscubeSDK.Android` for shared Gradle rules.

---

## Plugins (Android)

`Runtime/Plugins/Android/`:

- `WebViewPlugin-release.aar.tmpl` / variant templates
- `core-1.6.0.aar.tmpl` — AndroidX core
- Gradle templates for unity-webview

**No** mediation adapter AARs in core repo (RELEASE_CHECKLIST).

---

## Build verification checklist

- [ ] Selected feature set matches product requirements (Lite vs Full)
- [ ] Gradle export completes without duplicate-class errors
- [ ] LiteNoVideo: direct `ShowVideoAd` returns 1006
- [ ] FullWithVideo: interstitial/rewarded video plays
- [ ] WebView banners work on device

---

## Companion packages after core release

Bump in separate repos:

- `com.bidscube.applovin.max`
- `com.bidscube.levelplay`

Set `dependencies.com.bidscube.sdk` to new semver + re-tag adapter packages.
