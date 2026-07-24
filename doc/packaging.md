# Packaging та distribution

Як збирати, експортувати та розповсюджувати `com.bidscube.sdk`.

---

## UPM (рекомендовано)

### Git URL pin

```json
{
  "dependencies": {
    "com.bidscube.sdk": "https://github.com/BidsCube/bidscube-sdk-unity.git#v1.2.15"
  }
}
```

### Local path (розробка)

```json
{
  "dependencies": {
    "com.bidscube.sdk": "file:../bidscube-sdk-unity"
  }
}
```

Unity Package Manager → **+** → Add package from disk → `package.json`.

---

## Clean source archive

Do not create release/source archives by zipping the local working directory. Use `git archive`.

```bash
cd bidscube-sdk-unity
git archive --format=zip --output ../bidscube-sdk-unity-clean.zip HEAD
```

Do not include:

- `.git/`
- `Temp/`
- `Library/`
- `Logs/`
- `UserSettings/`
- `Build/`
- `build/`
- `build.app/`
- `__MACOSX/`
- `.DS_Store`
- `*.unitypackage` unless it is intentionally attached as a release artifact, not part of source archive

### Що **включається**

- `Runtime/`, `Editor/`, `Tests/`, `doc/`
- `package.json`, `README.md`, `INTEGRATION.md`, `CHANGELOG.md`
- `.meta` files для Unity assets

---

## Legacy `.unitypackage`

Скрипт: `scripts/copy-to-runtime.ps1`

- Дзеркалить `Assets/` layout → `Runtime/` для legacy export workflow
- PowerShell only; companion `.sh` може бути відсутній

GitHub Release може прикріплювати `.unitypackage` вручну після export з Unity Editor:

**Assets → Export Package** → select `Runtime/BidscubeSDK` + plugins.

---

## GitHub Release CI

`.github/workflows/release.yml`:

- Trigger: push tag `v*`
- Створює GitHub Release з notes з tag message
- Не збирає Unity binary автоматично

Після tag:

```bash
git tag v1.2.15
git push max master
git push max v1.2.15
```

Див. [release-process.md](release-process.md).

---

## Version sync checklist

При кожному релізі синхронізувати:

| Файл | Поле |
|------|------|
| `package.json` | `"version"` |
| `Runtime/BidscubeSDK/Core/Constants.cs` | `SdkVersion` |
| `CHANGELOG.md` | новий section |
| `INTEGRATION.md` | Git URL pin |
| `doc/overview.md` | pin version |
| `doc/release-process.md` | приклади tag/push |

---

## Mediation companion packages

This package is the core `com.bidscube.sdk` Unity SDK. AppLovin MAX and LevelPlay adapters are separate packages/repositories. This core package should not include AppLovin/LevelPlay AARs or adapter code.

Після core release оновити залежності в окремих репозиторіях:

- `com.bidscube.applovin.max`
- `com.bidscube.levelplay`

Core package **не містить** mediation binaries.

---

## Consumer verification

- [ ] UPM resolve без помилок
- [ ] Android Gradle build без duplicate classes
- [ ] iOS Xcode build без duplicate frameworks
- [ ] EditMode tests pass у harness project
- [ ] SDK Test Scene smoke на device

Повний список: [RELEASE_CHECKLIST.md](../RELEASE_CHECKLIST.md).
