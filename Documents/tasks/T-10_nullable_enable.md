# T-10 — Enable Nullable Reference Types on netstandard2.0

**Status:** Pending
**Phase:** 1 — Quality Gates
**Priority:** 🟡 Medium
**Estimated:** 1 day
**Constitution:** §3.1 (Nullable reference types), §12 TD-8
**Spec reference:** NFR-M03 (build 0 errors), Success Metrics (warnings: 472 → <20)

---

## Objective

Enable `<Nullable>enable</Nullable>` in the shared `PropertyGroup` of `CSLibrary2026.csproj` so it applies to both `netstandard2.0` and `net10.0` TFMs. Currently nullable is only enabled implicitly for `net10.0`.

---

## Prerequisite

T-09 (warnings fix) must be largely complete first. Enabling nullable on a codebase with 472 warnings will produce hundreds of new nullable warnings that make the signal worse.

**Target warning threshold before starting:** < 50 warnings.

---

## Step 1 — Audit: Where Are the Nullable Risk Areas?

Before enabling, identify files with the highest density of reference-type fields and method return types:

```bash
cd CSLibrary2026
grep -r "string\|object\|byte\[\]\|Stream\|Task<" Source/ --include="*.cs" \
  | grep -v "string\." | head -30
```

Files most likely to produce nullable warnings:
- `Source/BluetoothProtocol/*.cs` (byte arrays — should be fine with `byte[]?`)
- `Source/HAL/Plugin.BLE/CodeFileBLE.cs` (Plugin.BLE types)
- `Source/RFIDReader/CSLUnifiedAPI/Basic_Structures/*.cs` (structs with reference-type fields)

---

## Step 2 — Add Nullable to .csproj

```xml
<!-- In CSLibrary2026.csproj, shared PropertyGroup -->
<!-- Change from implicit (no Nullable element) to explicit enable -->
<Nullable>enable</Nullable>
```

---

## Step 3 — Rebuild and Triage

```bash
export PATH="/home/node/.dotnet:$PATH"
dotnet build -c Release 2>&1 | grep "warning CS8" | head -50
```

Common nullable warnings and fixes:

| Warning | Meaning | Fix |
|---|---|---|
| `CS8600` | Converting null to non-nullable | Add `!` or guard with null check |
| `CS8601` | Possible null reference assignment | Add null coalescing `??` |
| `CS8602` | Dereference of possible null | Add null guard `if (x == null) return` |
| `CS8603` | Possible null return | Change return type to `T?` |
| `CS8618` | Non-nullable property not initialized | Initialize in constructor or use `!` |

---

## Step 4 — Use #nullable disable Selectively

For files that cannot be made nullable-clean without significant refactoring (e.g., byte array protocol handlers), use file-level nullable disable:

```csharp
#nullable disable
// All the byte array and protocol code goes here
#nullable restore
```

Rule from constitution §3.1: "use `#nullable disable` in mixed files **only when strictly necessary and documented**"

---

## Step 5 — Commit

```bash
git add CSLibrary2026.csproj
git commit -m "build: enable nullable reference types on netstandard2.0

Nullable is now enabled for both TFMs.
Files using #nullable disable:
- Source/BluetoothProtocol/BTSend.cs (byte array protocol)
- Source/BluetoothProtocol/BTReceive.cs (byte array protocol)
(add actual file list after reviewing new warnings)

Constitutes: constitution §3.1, plan T-10"
```

---

## Definition of Done

- [ ] `<Nullable>enable</Nullable>` is in the shared PropertyGroup
- [ ] `dotnet build -c Release` passes with 0 errors on both TFMs
- [ ] New nullable warnings are addressed or documented with `#nullable disable`
- [ ] Total warnings remain < 50 (from T-09 target)
