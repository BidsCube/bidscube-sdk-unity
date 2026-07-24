# EditMode unit tests

> **Шлях:** `Tests/EditMode/`  
> **Assembly:** `BidscubeSDK.OpenRTB.Tests.asmdef`  
> **Framework:** NUnit (Unity Test Framework)

Тести покривають **OpenRTB parsing і plan building** без PlayMode / device. Не замінюють manual QA на сценах.

---

## Структура

```
Tests/
├── Tests.meta
└── EditMode/
    ├── BidscubeSDK.OpenRTB.Tests.asmdef
    ├── OpenRtbJsonTests.cs
    ├── OpenRtbVideoObjectParserTests.cs
    ├── OpenRtbPoddedResponseNormalizerTests.cs
    ├── PoddedPlaybackPlanBuilderTests.cs
    ├── VideoAdPayloadResolverTests.cs
    ├── VastAdSequenceParserTests.cs
    ├── VastAdTagJsonPlanLoaderTests.cs
    ├── OpenRtbVideoUrlHelperTests.cs
    └── OpenRtbPodSkipPolicyTests.cs
```

**References:** `BidscubeSDK` runtime assembly (`InternalsVisibleTo` через `OpenRTB/AssemblyInfo.cs`).

**Немає PlayMode tests** — video playback, WebView, Gradle — тільки manual/device QA.

---

## Запуск

### Unity Editor

1. Відкрити host project з підключеним пакетом (local path або Git URL)
2. **Window → General → Test Runner**
3. Вкладка **EditMode**
4. **Run All** або фільтр `BidscubeSDK.OpenRTB.Tests`

### Unity CLI (host project з `ProjectSettings/`)

```bash
Unity -batchmode \
  -projectPath /path/to/host-project \
  -runTests \
  -testPlatform EditMode \
  -testResults test-results.xml \
  -quit
```

⚠️ Цей репозиторій — **UPM package only** (немає `ProjectSettings/`). CLI напряму на корені пакету не працює — потрібен wrapper project.

---

## Матриця тестів

| Test class | Що перевіряє |
|------------|--------------|
| `OpenRtbJsonTests` | Valid JSON; trailing garbage rejected; trailing whitespace OK |
| `OpenRtbVideoObjectParserTests` | `podid`, `rqddurs`/`rqdDurs`, `poddur`, `maxseq` |
| `OpenRtbPoddedResponseNormalizerTests` | `bids[]`, `seatbid`, multi-pod → first sorted podid |
| `PoddedPlaybackPlanBuilderTests` | Dynamic budget, hybrid order, strict hybrid fail, structured sort |
| `VideoAdPayloadResolverTests` | Raw VAST, root adm, bids pod, metadata disabled, mp4 vs ad tag URL, rqdDurs fallback |
| `VastAdSequenceParserTests` | Multi-`<Ad>` split, sequence, duration parse |
| `VastAdTagJsonPlanLoaderTests` | Multi-slot JSON → `FullPlan`; single → `SingleSlot` |
| `OpenRtbVideoUrlHelperTests` | `.mp4` direct; ad tag URL; redirect depth > 5 |
| `OpenRtbPodSkipPolicyTests` | Default policy; builder sets `FailEntirePod` |

---

## Патерн JSON fixtures

Для VAST у JSON використовуйте `JsonEscape` + concatenation (не `\"` у verbatim strings):

```csharp
static string JsonEscape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

const string Vast1 = "<VAST version=\"3.0\">...</VAST>";
var json = @"{ ""bids"": [ { ""adm"": """ + JsonEscape(Vast1) + @""" } ] }";
```

---

## CI integration (рекомендація)

1. Створити minimal Unity test project у CI (або використати існуючий harness repo)
2. Додати `com.bidscube.sdk` via Git URL `#v1.2.15`
3. Запускати EditMode batchmode на кожен PR / tag
4. Артефакт: `test-results.xml` (JUnit-compatible)

Поточний `.github/workflows/release.yml` — тільки GitHub Release на tag, без test runner.

---

## Що не покрито unit tests

| Область | Чому | QA |
|---------|------|-----|
| `VideoAdView` coroutines | PlayMode / device | test-scenes-qa.md |
| WebView render | Native plugin | device builds |
| Gradle patcher | Editor export | android.md |
| Network HTTP mocks | No mock server in tests | manual / integration |
| Reward callback timing | Lifecycle guards | SDK Test Scene |

---

## Додавання нового тесту

1. Створити `Tests/EditMode/MyFeatureTests.cs`
2. Namespace: `BidscubeSDK.OpenRTB.Tests`
3. Додати `.meta` (Unity згенерує GUID при відкритті, або `uuidgen`)
4. Тестувати через **public/internal** API з `BidscubeSDK.OpenRTB` — не дублювати parser logic у тестах
5. Оновити цей документ і [openrtb.md](openrtb.md)
