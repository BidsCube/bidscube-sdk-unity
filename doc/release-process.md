# Release process

---

## Source of truth

| What | Where |
|------|-------|
| Semver | `package.json` → `"version"` |
| Git tag | `v` + semver (e.g. `v1.2.14`) |
| Changelog | `CHANGELOG.md` |
| Public pin | `INTEGRATION.md` Git URL `#vX.Y.Z` |

**Tag version MUST match `package.json`.** CI validates this.

---

## Pre-release checklist

Full list: [`RELEASE_CHECKLIST.md`](../RELEASE_CHECKLIST.md)

Summary:

- [ ] `package.json` name = `com.bidscube.sdk`
- [ ] Version bumped, CHANGELOG entry added
- [ ] Only ugui + TMP dependencies
- [ ] No mediation AARs in core tree
- [ ] No `Library/`, build artifacts committed
- [ ] Consumer can resolve `git#vX.Y.Z`

---

## CI workflow

File: `.github/workflows/release.yml`

**Trigger:** push tag matching `v*`

**Steps:**

1. Checkout
2. Verify tag version == `package.json` version (`jq`)
3. Create GitHub Release (`softprops/action-gh-release@v2`, auto notes)

---

## Manual release commands

```bash
cd /path/to/bidscube-sdk-unity

git status
git diff --stat

git add -A
git commit -m "$(cat <<'EOF'
Release com.bidscube.sdk 1.2.14

<short summary from CHANGELOG>
EOF
)"

git tag v1.2.14

# SSH remote (recommended internally)
git push max master
git push max v1.2.14

# or HTTPS
git push origin master
git push origin v1.2.14
```

### Non-fast-forward

```bash
git fetch max
git rebase max/master
git push max master
git push max v1.2.14
```

---

## After release

1. Verify GitHub Release created for tag
2. Update consumer `manifest.json` pins
3. Bump companion packages:
   - `com.bidscube.applovin.max`
   - `com.bidscube.levelplay`
4. Notify QA with test scene checklist ([test-scenes-qa.md](test-scenes-qa.md))

---

## Unity package distribution

### Git URL (UPM)

```json
"com.bidscube.sdk": "https://github.com/BidsCube/bidscube-sdk-unity.git#v1.2.14"
```

### .unitypackage (optional)

Script: `scripts/copy-to-runtime.ps1` — mirrors Assets → Runtime layout for legacy `.unitypackage` builds.

GitHub Release may attach unitypackage if published manually.

---

## Versioning policy (internal)

- **Patch** (1.2.x): bug fixes, small behavior fixes, doc
- **Minor** (1.x.0): new API, significant features (backward compatible)
- Document breaking changes explicitly in CHANGELOG

---

## Branch / remote notes

| Remote | URL | Notes |
|--------|-----|-------|
| `max` | `git@github.com:BidsCube/bidscube-sdk-unity.git` | SSH — preferred for push |
| `origin` | HTTPS GitHub | May need credentials |

Default branch: **`master`**

---

## Recent release themes (reference)

| Version | Highlights |
|---------|------------|
| 1.2.14 | OpenRTB 2.6 podded video, VAST ad tag URL fetch, EditMode tests |
| 1.2.13 | VAST end card preview, `LoadVideoAdFromVastXml`, local VAST QA |
| 1.2.12 | Interstitial/rewarded split, real video lifecycle, LiteNoVideo guards |
| 1.2.11 | Four Android feature sets |
| 1.2.8–1.2.10 | Gradle patcher, asmdef split, UPM meta fixes |
| 1.2.5 | Banner layout, native height, video backdrop |
| 1.2.4 | Release workflow, comment cleanup |

Full history: [`CHANGELOG.md`](../CHANGELOG.md)
