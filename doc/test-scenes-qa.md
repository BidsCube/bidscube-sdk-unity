# Test scenes та QA

Scenes: `Runtime/BidscubeSDK/Scenes/`

---

## Список сцен

| Scene | Script | Призначення |
|-------|--------|-------------|
| **SDK Test Scene.unity** | `SDKTestScene.cs` | **Primary QA hub** — всі ad types, positions, local VAST |
| **Bidscube Example Scene.unity** | `BidscubeExampleScene.cs` | Full demo, consent, integration mode bar |
| **Windowed Ad Scene.unity** | `WindowedAdTestScene.cs` | Layout/position у windowed area |
| **Consent Test Scene.unity** | `ConsentTestScene.cs` | Consent API stubs |
| **Custom Ad Render Scene.unity** | custom render demo | `IAdRenderOverride` |

Navigation: `SceneManager.cs` — `SceneType` enum + serialized scene names.

---

## SDK Test Scene — основний QA

### Startup flow

1. Open **SDK Test Scene** → Play
2. **Initialize SDK**
3. Test ad buttons або VAST QA buttons

### Default placement IDs (empty input)

| Ad type | Placement ID |
|---------|--------------|
| Image | `20212` |
| Video | `20213` |
| Native | `20214` |

### Manual position override

- Toggle **Use Manual Position**
- Dropdown: UNKNOWN, ABOVE_THE_FOLD, … FULL_SCREEN
- Re-show ad to apply

### Local VAST QA (v1.2.13+)

**Без backend** — hardcoded XML у `SDKTestScene.cs`.

| Case | Placement ID | Button |
|------|--------------|--------|
| No preview | `local_vast_no_preview` | **VAST (No Preview)** |
| With preview | `local_vast_with_preview` | **VAST (With Preview)** |

Buttons створюються runtime під **Video Ads** якщо не assigned у Inspector.

Alternative: ввести placement ID в input → **Video Ads**.

#### Case 1 — no preview

- Doordash burger MP4 (~13s)
- No Companion / StaticResource
- End card: last video frame fallback
- Skip after default 5s countdown
- No crash

#### Case 2 — with preview

- Big Buck Bunny MP4 (30s)
- `skipoffset="00:00:05"` → Skip in 5 → Skip
- Companion JPEG preview on end card
- Click → google.com, `OnAdClicked`
- Close → `OnAdClosed`

### Log panel

Scroll log shows callback order + integration mode status.

### Other controls

- **Clear All Ads** — destroy active creatives
- **Cleanup SDK** — full cleanup
- **Test Logging** — logger smoke test

---

## Bidscube Example Scene

- Banners (header/footer/custom)
- Consent buttons (stub flow)
- Integration mode selector bar
- Link to SDK Test Scene via SceneManager

Implements `IAdCallback` + `IRewardedAdCallback`.

---

## Windowed Ad Test Scene

Tests `SetAdViewsParentTransform` / embedded slot behavior and position refresh.

---

## Consent Test Scene

- `RequestConsentInfoUpdate`, `ShowConsentForm`, `ResetConsent`
- Ad smoke tests after consent
- **Not** production CMP validation

---

## Custom Ad Render Scene

Demonstrates `IAdRenderOverride` — app takes over adm rendering.

---

## BasicIntegration sample

`Runtime/BidscubeSDK/BasicIntegration/AdExample.cs`:

- Minimal UI wiring
- Direct `AdViewController.Initialize` (lower-level than static API)

---

## QA checklists

### Regression — all ad types

- [ ] Initialize / Cleanup without leaks
- [ ] Image loads and displays
- [ ] Video interstitial complete + skip
- [ ] Video rewarded → `OnUserRewarded` only on complete
- [ ] Native loads (WebView path)
- [ ] Manual position override works
- [ ] Clear All Ads destroys hierarchies

### Video end card (1.2.13+)

- [ ] VAST no preview: end card after complete/skip, fallback frame
- [ ] VAST with preview: companion image on end card
- [ ] End card click opens URL, `OnAdClicked`
- [ ] Close fires `OnAdClosed` (not on skip alone)

### Android builds

- [ ] LiteNoVideo: video API → 1006
- [ ] FullWithVideo: video plays on device
- [ ] Banner WebView on device

### iOS builds

- [ ] Banner + video on device
- [ ] End card + skip

---

## Running scenes in Unity

1. Add package to test project (local path or Git URL)
2. Open scene from `Runtime/BidscubeSDK/Scenes/`
3. Ensure TMP + uGUI packages resolved
4. Play Mode → Initialize → test

For device builds: use same scenes or integrate API into host app test harness.
