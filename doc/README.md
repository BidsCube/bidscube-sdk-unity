# Bidscube Unity SDK — внутрішня документація

> **Аудиторія:** команда BidsCube (розробка, QA, інтеграція).  
> **Пакет:** `com.bidscube.sdk`  
> **Поточна версія:** див. [`package.json`](../package.json) (на момент останнього оновлення doc — **1.2.14**).  
> **Публічна документація:** [`README.md`](../README.md), [`INTEGRATION.md`](../INTEGRATION.md).

Ця папка описує **весь** core SDK (`bidscube-sdk-unity`). Mediation-адаптери (`com.bidscube.applovin.max`, `com.bidscube.levelplay`) — окремі репозиторії; тут лише посилання там, де потрібно.

---

## Зміст

| Документ | Що всередині |
|----------|--------------|
| [overview.md](overview.md) | Огляд продукту, UPM-структура, залежності, швидкий старт |
| [architecture.md](architecture.md) | Архітектура, потоки даних, діаграми, `AdViewController` |
| [public-api.md](public-api.md) | Повний публічний API `BidscubeSDK`, enums, constants |
| [video-ads.md](video-ads.md) | Interstitial/rewarded, VAST, skip, end card, IMA, cache |
| [banner-and-webview.md](banner-and-webview.md) | Banner/image, WebView, HTML adm, layout |
| [native-ads.md](native-ads.md) | OpenRTB native, WebView vs Unity UI |
| [networking-vast.md](networking-vast.md) | URLBuilder, DeviceInfo, AdMarkupExtractor, VASTParser |
| [configuration.md](configuration.md) | SDKConfig, AdSizeSettings, позиціонування, consent |
| [callbacks-and-errors.md](callbacks-and-errors.md) | IAdCallback, IRewardedAdCallback, коди помилок, lifecycle |
| [android.md](android.md) | Feature sets, Gradle patcher, asmdef, LiteNoVideo |
| [ios-and-plugins.md](ios-and-plugins.md) | Native plugins, WebView, VideoPlayer на iOS |
| [test-scenes-qa.md](test-scenes-qa.md) | Тестові сцени, local VAST QA, чеклісти |
| [integration-modes.md](integration-modes.md) | SdkIntegrationContext (sample / internal) |
| [release-process.md](release-process.md) | Версіонування, теги, CI, RELEASE_CHECKLIST |
| [file-index.md](file-index.md) | Індекс ключових файлів репозиторію |
| [known-issues.md](known-issues.md) | Відомі обмеження, технічний борг, нюанси |

---

## Швидка навігація за задачами

| Задача | Куди дивитись |
|--------|---------------|
| Підключити SDK у новий проєкт | [overview.md](overview.md) + [INTEGRATION.md](../INTEGRATION.md) |
| Показати interstitial / rewarded video | [video-ads.md](video-ads.md) + [public-api.md](public-api.md) |
| End card + VAST companion preview | [video-ads.md](video-ads.md) § End card |
| QA без бекенду (local VAST) | [test-scenes-qa.md](test-scenes-qa.md) |
| Android Lite vs Full video | [android.md](android.md) |
| Підготовити реліз | [release-process.md](release-process.md) |
| Знайти файл у коді | [file-index.md](file-index.md) |

---

## Правила оновлення doc

1. При зміні публічного API — оновлювати `public-api.md` і відповідний тематичний документ.
2. При релізі — перевірити версію в `overview.md` / цьому README.
3. Не дублювати повністю `INTEGRATION.md` — там інструкції для зовнішніх інтеграторів; тут — внутрішня глибина.
4. Consent, IMA, `Constants.SdkVersion` — див. [known-issues.md](known-issues.md).
