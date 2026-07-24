# Integration modes (internal / sample)

Файл: `Runtime/BidscubeSDK/SdkIntegrationContext.cs`

`SdkIntegrationContext` is for sample/test scenes only. Production apps and external integrators should not use it to select mediation mode. Mediation adapters such as AppLovin MAX and LevelPlay are distributed separately.

⚠️ **Не публічний integrator API.** Використовується в sample/test scenes для перемикання між direct SDK і LevelPlay mediation testing.

---

## SdkIntegrationMode

```csharp
public enum SdkIntegrationMode
{
    BidscubeDirect = 0,
    BidscubeWithLevelPlayAdapter = 1,
    LevelPlayMediation = 2
}
```

| Mode | Description |
|------|-------------|
| **BidscubeDirect** | Тільки Bidscube SDK; LevelPlay bridge **не** повинен працювати |
| **BidscubeWithLevelPlayAdapter** | Bidscube + custom ironSource adapter |
| **LevelPlayMediation** | Mediation path через LevelPlay UI/placements |

---

## SdkIntegrationContext API

| Member | Behavior |
|--------|----------|
| `Mode` | Current mode (default `BidscubeDirect`) |
| `SuppressLevelPlayBridge` | `true` when `BidscubeDirect` |
| `SetMode(mode)` | Persist to PlayerPrefs `bcc_sdk_integration_mode` |
| `LoadPersistedMode()` | Restore on boot |
| `GetCurrentDescription()` | Long UI string for test scenes |
| `GetCurrentShortStatus()` | Short status label |

---

## Bootstrap

`SdkIntegrationContextBootstrap` — `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` loads persisted mode automatically.

---

## Usage in test scenes

**SDKTestScene** and **BidscubeExampleScene** at Start:

```csharp
SdkIntegrationContext.LoadPersistedMode();
if (SdkIntegrationContext.SuppressLevelPlayBridge)
{
    // destroy BidscubeLevelPlayBridge if present
}
LogMessage(SdkIntegrationContext.GetCurrentShortStatus());
```

Bidscube Example Scene exposes UI to switch modes.

---

## When to use

| Scenario | Mode |
|----------|------|
| QA core SDK only | `BidscubeDirect` |
| Test ironSource custom adapter | `BidscubeWithLevelPlayAdapter` |
| Test full mediation waterfall | `LevelPlayMediation` |

Production host apps typically **do not** use `SdkIntegrationContext` — they choose either direct SDK or mediation adapter package explicitly.

---

## Related packages (out of repo)

- `com.bidscube.levelplay` — LevelPlay / ironSource adapter
- `com.bidscube.applovin.max` — AppLovin MAX adapter

Both depend on `com.bidscube.sdk` semver pin.
