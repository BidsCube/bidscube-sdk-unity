# Release checklist — `com.bidscube.sdk`

Use this list before tagging and publishing a core SDK release. Current production pin referenced by apps: **`v1.2.5`**.

## Version and tag

- [ ] `package.json` field `"name"` is `com.bidscube.sdk`
- [ ] `package.json` field `"version"` matches the release (e.g. `1.2.5` for tag `v1.2.5`)
- [ ] Git tag name follows `v` + semver (e.g. `v1.2.5`) and matches `package.json` version
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

- [ ] If `.github/workflows/` includes a release workflow, tag `v*` matches `package.json` so automation can run (on **`v1.2.5`**, workflow exists on the tag; default branch may not include it until merged)

## Consumer verification

- [ ] Unity Package Manager resolves the package from  
  `https://github.com/Bidscube/bidscube-sdk-unity.git#v1.2.5`  
  (or the equivalent `BidsCube` org URL your manifest uses)
- [ ] **Android:** Gradle build completes without duplicate-class errors from overlapping SDK/adapter AARs
- [ ] **iOS:** Xcode build completes without duplicated frameworks from overlapping SDK/adapter pods or embedded binaries

## Branch vs tag (do not assume `master` is the release)

- **Release tag `v1.2.5`:** Validated — `package.json` has `"name": "com.bidscube.sdk"` and `"version": "1.2.5"`; dependencies are `com.unity.ugui` and `com.unity.textmeshpro` only; no AppLovin/LevelPlay adapter assets in-tree.
- **Default branch (`master`):** May lag the tag (e.g. older `package.json` version or missing changelog entries). Prefer **documenting** drift and merging forward rather than rewriting tags.

### Optional follow-up after a release

- Sync `master` with the released commit or bump `package.json` on `master` to the next **development** version (e.g. `1.2.6` or `1.3.0`) so the default branch is unambiguous for contributors.

## Doc erratum on tag `v1.2.5` (optional hotfix)

- The Quick Start snippet in `README.md` on tag `v1.2.5` used `.BaseURL(Constants.)` (invalid). Use `.BaseURL(Constants.BaseURL)` or omit `BaseURL` to use the default. **Master** documentation has been corrected; a future doc-only tag can align the default branch and release docs if desired.
