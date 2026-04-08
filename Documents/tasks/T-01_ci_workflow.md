# T-01 — Create GitHub Actions CI Workflow

**Status:** Pending
**Phase:** 0 — Foundation
**Priority:** 🔴 High
**Estimated:** 1 hour
**Constitution:** §6.3, §11.3, §12 TD-4
**Spec reference:** Success Metrics (CI pipeline: not configured → runs on every PR)

---

## Objective

Add `.github/workflows/ci.yml` so every PR and push runs `dotnet build -c Release` on both `netstandard2.0` and `net10.0` and `dotnet test -c Release` on `net10.0`.

---

## Prerequisite

- GitHub repo is at `https://github.com/cslrfid/CSLibrary2026`
- GitHub token has permission to create workflows (configured in `auth-profiles.json`)

---

## Deliverable

File: `.github/workflows/ci.yml`

```yaml
name: CI

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main, develop]

env:
  DOTNET_NOLOGO: 1
  DOTNET_CLI_TELEMETRY_OPTOUT: 1

jobs:
  build:
    name: Build (${{ matrix.target }})
    runs-on: ubuntu-latest
    strategy:
      fail-fast: false
      matrix:
        target: [netstandard2.0, net10.0]

    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup .NET 10
        if: matrix.target == 'net10.0'
        run: |
          wget -q https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
          chmod +x dotnet-install.sh
          ./dotnet-install.sh --channel 10.0 --install-dir $HOME/.dotnet
          echo "$HOME/.dotnet" >> $GITHUB_PATH

      - name: Build .NET SDK path
        run: echo "$HOME/.dotnet" >> $GITHUB_PATH

      - name: Restore
        run: dotnet restore CSLibrary2026/CSLibrary2026.csproj

      - name: Build
        run: >
          dotnet build CSLibrary2026/CSLibrary2026.csproj
          -c Release
          --framework ${{ matrix.target }}

      - name: Pack
        run: >
          dotnet pack CSLibrary2026/CSLibrary2026.csproj
          -c Release
          -o ./nupkg
          --no-build

  test:
    name: Test (net10.0)
    runs-on: ubuntu-latest
    # Hardware integration tests are excluded in CI per constitution §6.2
    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup .NET 10
        run: |
          wget -q https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
          chmod +x dotnet-install.sh
          ./dotnet-install.sh --channel 10.0 --install-dir $HOME/.dotnet
          echo "$HOME/.dotnet" >> $GITHUB_PATH

      - name: Restore tests
        run: dotnet restore CSLibrary2026/Tests/CSLibrary2026.Tests/CSLibrary2026.Tests.csproj

      - name: Build tests
        run: dotnet build CSLibrary2026/Tests/CSLibrary2026.Tests/CSLibrary2026.Tests.csproj -c Release

      - name: Run tests
        run: >
          dotnet test CSLibrary2026/Tests/CSLibrary2026.Tests/CSLibrary2026.Tests.csproj
          -c Release
          --filter "Category!=integration"
          --logger "trx;LogFileName=results.trx"
```

---

## Step-by-Step Commands

```bash
# 1. Create the workflows directory
mkdir -p CSLibrary2026/.github/workflows

# 2. Write the workflow file
# (use the write tool to create .github/workflows/ci.yml)

# 3. Add .nupkg to .gitignore (packages are build artifacts)
echo "nupkg/" >> CSLibrary2026/.gitignore

# 4. Commit
git add .github/workflows/ci.yml .gitignore
git commit -m "ci: add GitHub Actions workflow for build and test"

# 5. Push and create PR to develop
git push origin develop
gh pr create --base develop --title "ci: add GitHub Actions build and test workflow" --body "Adds .github/workflows/ci.yml that runs dotnet build on both TFMs and dotnet test on net10.0."
```

---

## Verification

After the PR is merged:
1. Open GitHub Actions tab on the repo
2. Trigger a manual workflow run or push a new commit
3. Confirm all 3 jobs pass: `build (netstandard2.0)`, `build (net10.0)`, `test (net10.0)`

---

## Definition of Done

- [ ] `.github/workflows/ci.yml` exists in the repo
- [ ] All 3 jobs pass on the merged `develop` branch
- [ ] The workflow runs on every PR to `develop` and `main`
