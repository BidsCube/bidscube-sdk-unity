# Release checklist — `com.bidscube.sdk`

Use this list before tagging and publishing a core SDK release. Current production pin referenced by apps: **`v1.2.6`**.

## Version and tag

- [ ] `package.json` field `"name"` is `com.bidscube.sdk`
- [ ] `package.json` field `"version"` matches the release (e.g. `1.2.6` for tag `v1.2.6`)
- [ ] Git tag name follows `v` + semver (e.g. `v1.2.6`) and matches `package.json` version
- [ ] `CHANGELOG.md` includes an entry for this version

## Dependencies (`package.json`)

- [ ] Only minimal Unity dependencies, e.g. `com.unity.ugui` and `com.unity.textmeshpro` (no AppLovin, LevelPlay, IronSource, or other mediation SDK entries)

## Package contents (core only)

- [ ] `Runtime/BidscubeSDK/` — C# runtime and direct SDK API only
- [ ] `Runtime/Plugins/Android/` — core Android assets only (e.g. WebView / AndroidX templates as shipped with this repo). **No** `applovin-bidscube-max-adapter-*.aar`, LevelPlay/IronSource adapter AARs, or duplicate legacy SDK binaries
- [ ] `Runtime/Plugins/iOS/` — core iOS native sources only; `.meta` files restrict plugins to **iPhone** where required. **No** AppLovin or LevelPlay adapter native code in this package
- [ ] Documentation describes **direct** SDK usage only (`README.md`, `INTEGRATION.md`)

## Repository hygiene

- [ ] No generated Unity project folders committed (`Library/`, `Temp/`, `Logs/`, etc. — see `.gitignore`)
- [ ] No binary build artifacts tracked (`*.apk`, `*.aab`, `*.ipa`, `*.app`, etc.)
- [ ] No mediation-specific or duplicate adapter binaries in the core tree

## CI / automation (when present)

- [ ] If `.github/workflows/` includes a release workflow, tag `v*` matches `package.json` so automation can run

## Consumer verification

- [ ] Unity Package Manager resolves the package from  
  `https://github.com/BidsCube/bidscube-sdk-unity.git#v1.2.6`  
  (or the equivalent `Bidscube` org URL your manifest uses)
- [ ] **Android:** Gradle build completes without duplicate-class errors from overlapping SDK/adapter AARs
- [ ] **iOS:** Xcode build completes without duplicated frameworks from overlapping SDK/adapter pods or embedded binaries

## Push / tag (example)

```bash
cd bidscube-sdk-unity
git add -A && git status
git commit -m "Release com.bidscube.sdk 1.2.6"
git tag v1.2.6
git push origin main && git push origin v1.2.6
```

Then create a **GitHub Release** from tag `v1.2.6` (title `v1.2.6`, notes from `CHANGELOG.md`).

## After release

- Bump companion packages (`com.bidscube.applovin.max`, `com.bidscube.levelplay`) so `package.json` → `dependencies` → `com.bidscube.sdk` matches **`1.2.6`** and re-tag those repos per their `RELEASE.md`.
