# Bidscube Unity SDK — внутрішня документація

> **Аудиторія:** команда BidsCube (розробка, QA, інтеграція, support).  
> **Пакет:** `com.bidscube.sdk`  
> **Поточна версія:** **1.2.15** — див. [`package.json`](../package.json), [`CHANGELOG.md`](../CHANGELOG.md).  
> **Публічна документація:** [`README.md`](../README.md), [`INTEGRATION.md`](../INTEGRATION.md).

Ця папка — **повний внутрішній довідник** по core SDK (`bidscube-sdk-unity`).

This package is the core `com.bidscube.sdk` Unity SDK. AppLovin MAX and LevelPlay adapters are separate packages/repositories. This core package should not include AppLovin/LevelPlay AARs or adapter code.

---

## Зміст (повний)

### Огляд і архітектура

| Документ | Що всередині |
|----------|--------------|
| [overview.md](overview.md) | Продукт, UPM, структура repo, швидкий старт, assemblies |
| [architecture.md](architecture.md) | Шари, AdViewController, data flows, діаграми |
| [integration-modes.md](integration-modes.md) | SdkIntegrationContext (sample / internal) |
| [file-index.md](file-index.md) | Індекс усіх ключових файлів |

### Публічний API і конфігурація

| Документ | Що всередині |
|----------|--------------|
| [public-api.md](public-api.md) | Повний API `BidscubeSDK`, enums, constants |
| [configuration.md](configuration.md) | SDKConfig, AdSizeSettings, OpenRTB options, consent |
| [callbacks-and-errors.md](callbacks-and-errors.md) | IAdCallback, IRewardedAdCallback, error codes, pod lifecycle |

### Типи реклами

| Документ | Що всередині |
|----------|--------------|
| [video-ads.md](video-ads.md) | Interstitial/rewarded, VAST, skip, end card, pods |
| [openrtb.md](openrtb.md) | **OpenRTB 2.6 podded video** — повний module reference |
| [banner-and-webview.md](banner-and-webview.md) | Banner/image, WebView, HTML adm, layout |
| [native-ads.md](native-ads.md) | OpenRTB native render, WebView vs Unity UI |

### Мережа і платформи

| Документ | Що всередині |
|----------|--------------|
| [networking-vast.md](networking-vast.md) | URLBuilder, DeviceInfo, VASTParser, HTTP |
| [android.md](android.md) | Feature sets, Gradle patcher, LiteNoVideo |
| [ios-and-plugins.md](ios-and-plugins.md) | Native plugins, WebView, VideoPlayer |

### QA, тести, реліз

| Документ | Що всередині |
|----------|--------------|
| [test-scenes-qa.md](test-scenes-qa.md) | Unity test scenes, VAST QA, OpenRTB pod QA |
| [editmode-tests.md](editmode-tests.md) | Unit tests, запуск, матриця покриття |
| [release-process.md](release-process.md) | Версіонування, теги, push, CI |
| [packaging.md](packaging.md) | UPM, git archive, unitypackage |
| [troubleshooting.md](troubleshooting.md) | FAQ, типові проблеми, діагностика |

### Мета

| Документ | Що всередині |
|----------|--------------|
| [known-issues.md](known-issues.md) | Обмеження, технічний борг, backlog |

---

## Швидка навігація за роллю

### Розробник SDK

1. [architecture.md](architecture.md) → [openrtb.md](openrtb.md)
2. [file-index.md](file-index.md)
3. [editmode-tests.md](editmode-tests.md)
4. [known-issues.md](known-issues.md)

### QA

1. [test-scenes-qa.md](test-scenes-qa.md)
2. [troubleshooting.md](troubleshooting.md)
3. [callbacks-and-errors.md](callbacks-and-errors.md) — expected callback order

### Інтегратор (внутрішній)

1. [overview.md](overview.md) + [INTEGRATION.md](../INTEGRATION.md)
2. [public-api.md](public-api.md)
3. [configuration.md](configuration.md)
4. [android.md](android.md) / [ios-and-plugins.md](ios-and-plugins.md)

### Release manager

1. [release-process.md](release-process.md)
2. [packaging.md](packaging.md)
3. [RELEASE_CHECKLIST.md](../RELEASE_CHECKLIST.md)

---

## Швидка навігація за задачами

| Задача | Документ |
|--------|----------|
| Підключити SDK | [overview.md](overview.md), [INTEGRATION.md](../INTEGRATION.md) |
| Interstitial / rewarded video | [video-ads.md](video-ads.md), [public-api.md](public-api.md) |
| OpenRTB pod (2+ slots) | [openrtb.md](openrtb.md), [configuration.md](configuration.md) |
| VAST end card + companion | [video-ads.md](video-ads.md) § End card |
| Local VAST без backend | [test-scenes-qa.md](test-scenes-qa.md) |
| VAST ad tag URL vs MP4 | [openrtb.md](openrtb.md), [troubleshooting.md](troubleshooting.md) |
| Android Lite vs Full | [android.md](android.md) |
| Error 1006 | [android.md](android.md), [callbacks-and-errors.md](callbacks-and-errors.md) |
| Запустити unit tests | [editmode-tests.md](editmode-tests.md) |
| Зібрати clean zip | [packaging.md](packaging.md) |
| Підготувати реліз | [release-process.md](release-process.md) |
| Знайти файл у коді | [file-index.md](file-index.md) |

---

## Версійні highlights (1.2.15)

- Integrator `user_id` on ad requests (`SDKConfig.UserId` / `SetUserId`) for SSP postbacks
- OpenRTB 2.6 **response-side** podded video
- Sequential `VideoPlayer` playback (structured / dynamic / hybrid pods)
- VAST ad tag URL fetch, nested JSON multi-slot plans
- Redirect depth cap (5), strict hybrid budget, JSON trailing garbage guard
- EditMode tests у `Tests/EditMode/`
- `SdkVersion` synced → `1.2.15`

Повна історія: [`CHANGELOG.md`](../CHANGELOG.md).

---

## Правила оновлення doc

1. **Публічний API змінився** → `public-api.md` + тематичний doc + `CHANGELOG.md`
2. **OpenRTB змінився** → `openrtb.md`, `video-ads.md`, `editmode-tests.md`
3. **Реліз** → `overview.md`, цей README, `release-process.md`, `INTEGRATION.md` pin
4. **Новий тест** → `editmode-tests.md` матриця
5. Не дублювати `INTEGRATION.md` повністю — там зовнішній onboarding; тут — внутрішня глибина
6. Мова: **українська** для внутрішніх doc; code identifiers — English

---

## Зовнішні посилання

| Ресурс | URL |
|--------|-----|
| GitHub repo | `https://github.com/BidsCube/bidscube-sdk-unity` |
| SSH remote | `max` → `git@github.com:BidsCube/bidscube-sdk-unity.git` |
| SSP default | `https://ssp-bcc-ads.com/sdk` |
