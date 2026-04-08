# T-21 — Publish NuGet beta.2 to nuget.org

**Status:** Pending
**Phase:** All (cross-cutting)
**Priority:** 🔴 High
**Estimated:** 1 hour
**Constitution:** §10.2 (NuGet distribution is part of the project identity), spec Success Metrics
**Spec reference:** Success Metrics (NuGet package: 1.0.0-beta.1 (local) → published beta.2 on nuget.org)

---

## Objective

Build the `CSLibrary2026.nupkg` and publish version `1.0.0-beta.2` to `https://nuget.org/packages/CSLibrary2026/`.

---

## Prerequisites

Before publishing, confirm:
- [ ] T-09 (warnings fix) is merged — no compiler warnings
- [ ] T-11 (CHANGELOG.md) is created and up to date
- [ ] All Phase 0 and Phase 1 tasks that affect the build are merged
- [ ] `dotnet build -c Release` passes cleanly on both TFMs
- [ ] Version number in `.csproj` is updated to `1.0.0-beta.2`

---

## Step 1 — Update Version in .csproj

**File:** `CSLibrary2026.csproj`

```xml
<!-- Change from: -->
<Version>0.0.1</Version>

<!-- To: -->
<Version>1.0.0-beta.2</Version>
```

Also update the CHANGELOG.md `[Unreleased]` section:
- Move all items from `[Unreleased]` to `[1.0.0-beta.2] — YYYY-MM-DD`
- Replace `YYYY-MM-DD` with today's date

---

## Step 2 — Build and Pack

```bash
cd CSLibrary2026
export PATH="/home/node/.dotnet:$PATH"

# Clean previous build artifacts
rm -rf bin/ obj/ ../nupkg/
mkdir -p ../nupkg

# Restore and build
dotnet restore
dotnet build -c Release

# Pack
dotnet pack -c Release -o ../nupkg --no-build

# Verify the package
ls -lh ../nupkg/
# Expected: CSLibrary2026.1.0.0-beta.2.nupkg
```

---

## Step 3 — Verify Package Contents (Optional but Recommended)

```bash
# Install nuget package explorer CLI (or use NuGet Package Explorer GUI)
dotnet tool install -g npe --version 2.11.1 2>/dev/null || true

# Or inspect the nupkg as a zip file
unzip -l ../nupkg/CSLibrary2026.1.0.0-beta.2.nupkg | grep -E "dll|nupkg"
```

Confirm:
- `lib/netstandard2.0/CSLibrary2026.dll` exists
- `lib/net10.0/CSLibrary2026.dll` exists
- `README.md` is included

---

## Step 4 — Push to nuget.org

### Option A: Via `dotnet nuget push`

```bash
# Get the API key from GitHub secrets (stored in auth-profiles.json or GitHub Actions secrets)
# The key should be stored in a secret named NUGET_API_KEY

dotnet nuget push ../nupkg/CSLibrary2026.1.0.0-beta.2.nupkg \
  --api-key "$NUGET_API_KEY" \
  --source https://api.nuget.org/v3/index.json \
  --skip-duplicate
```

### Option B: Via GitHub Actions (Recommended — no local API key needed)

Add to `.github/workflows/ci.yml` or create a separate `publish.yml`:

```yaml
name: Publish to NuGet

on:
  push:
    tags:
      - 'v*'  # Only when a v* tag is pushed

jobs:
  publish:
    name: Publish
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        run: |
          wget -q https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
          chmod +x dotnet-install.sh
          ./dotnet-install.sh --channel 10.0 --install-dir $HOME/.dotnet
          echo "$HOME/.dotnet" >> $GITHUB_PATH

      - name: Pack
        run: |
          dotnet pack CSLibrary2026/CSLibrary2026.csproj -c Release -o ./nupkg

      - name: Push to NuGet
        run: |
          dotnet nuget push ./nupkg/*.nupkg \
            --api-key ${{ secrets.NUGET_API_KEY }} \
            --source https://api.nuget.org/v3/index.json
```

Then to publish:
```bash
git tag v1.0.0-beta.2
git push origin v1.0.0-beta.2
```

---

## Step 5 — Update README.md Badge

```markdown
[![NuGet](https://img.shields.io/nuget/v/CSLibrary2026.svg)](https://www.nuget.org/packages/CSLibrary2026/)
```

Should reflect the new version. The badge URL is `https://img.shields.io/nuget/v/CSLibrary2026.svg` — no change needed; shields.io always points to the latest.

---

## Step 6 — Commit and Tag

```bash
git add CSLibrary2026.csproj CHANGELOG.md README.md
git commit -m "release: bump version to 1.0.0-beta.2

- Updated <Version> to 1.0.0-beta.2 in .csproj
- Moved [Unreleased] entries to [1.0.0-beta.2] in CHANGELOG.md
- Minor README badge verification

This tag triggers the NuGet publish GitHub Action."

git tag v1.0.0-beta.2
git push origin develop
git push origin v1.0.0-beta.2
```

---

## Step 7 — Verify on nuget.org

After push, wait ~5 minutes then check:
- https://www.nuget.org/packages/CSLibrary2026/
- Verify version `1.0.0-beta.2` appears
- Verify download count increases

---

## Definition of Done

- [ ] `CSLibrary2026.nupkg` is built with version `1.0.0-beta.2`
- [ ] Package is published to `https://www.nuget.org/packages/CSLibrary2026/`
- [ ] `lib/netstandard2.0/` and `lib/net10.0/` assemblies are in the package
- [ ] `README.md` is included in the package
- [ ] CHANGELOG.md has the version header updated
- [ ] `git tag v1.0.0-beta.2` pushed to origin
