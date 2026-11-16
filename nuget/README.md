# NuGet publishing

This folder holds the NuGet publishing assets for **CSLibrary2026**.

| Path | Purpose |
|---|---|
| `scripts/Publish-NuGet.ps1` | Build the package and push it to NuGet.org (Windows) |
| `scripts/Merge-NuGetPackages.ps1` | Merge the Linux and macOS packages into one `.nupkg` |
| `scripts/publish-nuget.sh` | Build the package and push it to NuGet.org (Linux / macOS) |
| `scripts/push-cslibrary.sh` | Legacy daily sync script (kept for reference) |
| `api-key.txt.template` | Template for the local API key file |
| `packages/` | Previously published packages |

## What is in the published package

The NuGet package contains **only the TCP/IP + Plugin.BLE flavours**:

```
lib/netstandard2.0/           lib/netstandard2.1/
lib/net10.0/
lib/net10.0-android36.0/      lib/net10.0-ios26.0/
lib/net10.0-windows10.0.19041/
```

Two HALs are deliberately **not** published and are built from source instead:

| HAL | Target framework | Why it is not published |
|---|---|---|
| `Source/HAL/btframework` | `net48` | Needs the WCL assemblies (`Source/HAL/btframework/wcl*.dll`) and ships a different HAL than the package |
| `Source/HAL/WinFormsBLE` | `net10.0-windows*` | A MAUI app and a WinForms app resolve the same `net10.0-windows*` target framework, so NuGet cannot hand out a different HAL per UI framework |

This is enforced in `CSLibrary2026.csproj`: the `net48` target sets
`IncludeBuildOutput=false` and `SuppressDependenciesWhenPacking=true`, so it
contributes neither a `lib/net48` folder nor a `.NETFramework4.8` dependency
group. `Source/HAL/WinFormsBLE/**` is not compiled into the project at all.

## Publishing from the command line

### Windows

```powershell
# Build only
pwsh -File nuget/scripts/Publish-NuGet.ps1 -Version 2.0.0-beta.3 -SkipPush

# Build and push
pwsh -File nuget/scripts/Publish-NuGet.ps1 -Version 2.0.0-beta.3 -ApiKey $env:NUGET_API_KEY
```

### Linux / macOS

```bash
./nuget/scripts/publish-nuget.sh 2.0.0-beta.3
```

The API key is read from `NUGET_API_KEY` (environment variable) or from
`nuget/api-key.txt` (gitignored - copy `api-key.txt.template` to create it).

## Publishing from GitHub Actions

`.github/workflows/publish-nuget.yml` publishes the package automatically:

1. **build-linux** (ubuntu) packs `netstandard2.0`, `netstandard2.1`, `net10.0`,
   `net10.0-windows` and `net10.0-android`.
2. **build-macos** (macOS) packs `net10.0-ios` - Apple targets only build on macOS.
3. **merge** combines both into a single `.nupkg`.
4. **publish** pushes it to NuGet.org.

The workflow runs on a published GitHub release, or manually via
*Actions → Publish NuGet → Run workflow* (with an optional version and dry-run flag).

**Required repository secret:** `NUGET_API_KEY` - a NuGet.org API key with
"Push" rights on the `CSLibrary2026` package. Create it at
<https://www.nuget.org/account/apikeys> and add it under
*Settings → Secrets and variables → Actions*.

## Target framework selection

MSBuild's command line treats `;` as a property separator, so
`-p:TargetFrameworks=a;b` fails with `MSB1006`. The target frameworks for a pack
are therefore selected with the `PackTargetFrameworks` property (or the
`PACK_TARGET_FRAMEWORKS` environment variable), which carries the `;` list verbatim:

```bash
PACK_TARGET_FRAMEWORKS='netstandard2.0;net10.0' dotnet pack CSLibrary2026.csproj -c Release
```

```powershell
$env:PACK_TARGET_FRAMEWORKS = 'netstandard2.0;net10.0'
dotnet pack CSLibrary2026.csproj -c Release
```

When the property is not set, the default matrix from `CSLibrary2026.csproj` is
used (every target framework the current SDK supports, minus `net48` in the package).

## Version numbering

Published versions so far: `0.0.1`, `1.0.0-beta.1` … `1.0.0-beta.13`,
`2.0.0-beta.1`, `2.0.0-beta.2`. The version is **not** taken from a Git tag
automatically in the local scripts - pass it explicitly with `-Version`, or let
the GitHub workflow derive it from the release tag (`v2.0.0-beta.3` → `2.0.0-beta.3`).

The version in `CSLibrary2026.csproj` is only the fallback for a pack without an
explicit `-p:Version`.

## Notes

- The `net10.0-ios` flavour can only be built on a macOS agent. A package built
  on Linux therefore contains no iOS binary - use the GitHub workflow (or the
  merge script) to publish a complete package.
- `dotnet nuget push --skip-duplicate` is used, so re-running a publish does not
  fail when the version already exists.
