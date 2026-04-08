# T-20 — New Reader Model Checklist

**Status:** Template (run per new CSL reader release)
**Phase:** 5 — Per-Model Checklist
**Priority:** 🟡 Medium (per model)
**Estimated:** 2–4 hours per new model
**Constitution:** §5.4 (MODEL Enum — adding a new model requires updating 4+ files)
**Spec reference:** §13.1 (Future Extensibility — adding a new CSL reader)

---

## When to Use This

Use this checklist when CSL releases a new RFID reader model that needs to be supported by CSLibrary2026.

---

## Pre-Check: Which Chipset Does the New Model Use?

Before starting, determine whether the new reader uses:
- **Rx000 chipset** → controlled via direct MAC register writes → `Comm_Protocol/RX000Commands/`
- **E710/E910 chipset** → controlled via high-level command/response → `Comm_Protocol/Ex10Commands/`

If neither, a new `Comm_Protocol/{ChipsetName}Commands/` folder is required — this is a major addition needing its own spec and review.

---

## Checklist: Files to Update

For a new model `CSXXX`:

### 1. MODEL Enum

**File:** `Source/RFIDReader/CSLUnifiedAPI/Basic_Constants/CSLibrary.Constants.cs`

Add to the `MODEL` enum:
```csharp
CSXXX = N,  // Replace N with next available integer
```

---

### 2. BLE Service UUID in BLETransport

**File:** `Source/Transport/BLETransport.cs`

Add to `ConnectAsync` MODEL switch:
```csharp
case MODEL.CSXXX:
    // Determine the correct service UUID from the CSL firmware spec
    // CS108/CS468 family: 00009800
    // CS710S/CS203XL family: 00009802
    _service = await device.GetServiceAsync(Guid.Parse("0000980X-0000-1000-8000-00805f9b34fb"));
    break;
```

---

### 3. Device Model Detection

**File:** `Source/HAL/Plugin.BLE/DeviceFinder.cs`

Add to `DetectDeviceModel()`:
```csharp
// Option A: Service UUID-based detection (preferred)
// [already handled by scanning for 0x9800/0x9802 service UUIDs]

// Option B: MAC OUI prefix detection (fallback)
// If CSXXX has a different OUI from CS108/CS710S:
private static MODEL DetectDeviceModel(IDevice device)
{
    // existing logic...
    string address = device.NativeDevice.ToString().ToUpperInvariant();
    if (address.StartsWith("XX:XX:XX")) // CSXXX OUI
        return MODEL.CSXXX;
}
```

---

### 4. Chipset Dispatch in UnifiedAPI

**File:** `Source/RFIDReader/CSLUnifiedAPI/Basic_API/ClassRFID.UnifiedAPI.cs`

Verify the new model's dispatch:
```csharp
// Confirm whether CSXXX uses Rx000 or E710/E910
// If E710/E910: already covered by existing E710/E910 dispatch
// If Rx000: already covered by existing Rx000 dispatch
// If new chipset: this requires a new Comm_Protocol folder (major change)
```

---

### 5. BLE Connection in HighLevelInterface (if needed)

**File:** `Source/HAL/Plugin.BLE/CodeFileBLE.cs` (partial to `HighLevelInterface`)

Check if any connection-time handling is model-specific. If the CSXXX needs special handling at connect time (not just different UUIDs), add a model branch here.

---

### 6. README.md

**File:** `CSLibrary2026/README.md`

Add to the supported readers table:
```markdown
| CSXXX | Bluetooth LE | [Rx000|E710|E910] | [Handheld|Fixed] |
```

---

### 7. CHANGELOG.md

**File:** `CSLibrary2026/CHANGELOG.md`

Add under `[Unreleased]` → `### Added`:
```markdown
- Support for CSXXX reader (MODEL.CSXXX)
```

---

### 8. spec.md

**File:** `CSLibrary2026/Documents/specs/spec.md`

Add to the reader compatibility matrix in §7.3:
```markdown
| CSXXX | BLE | [Chipset] | [Form factor] |
```

---

## Verification Steps

After all files are updated:

```bash
# 1. Build
export PATH="/home/node/.dotnet:$PATH"
dotnet build -c Release

# 2. Verify new MODEL is in the enum
grep "CSXXX" Source/RFIDReader/CSLUnifiedAPI/Basic_Constants/CSLibrary.Constants.cs

# 3. Verify BLETransport has the new case
grep "CSXXX" Source/Transport/BLETransport.cs

# 4. Verify DeviceFinder has detection
grep "CSXXX" Source/HAL/Plugin.BLE/DeviceFinder.cs

# 5. Run all tests
dotnet test -c Release
```

---

## Commit Template

```bash
git add \
  Source/RFIDReader/CSLUnifiedAPI/Basic_Constants/CSLibrary.Constants.cs \
  Source/Transport/BLETransport.cs \
  Source/HAL/Plugin.BLE/DeviceFinder.cs \
  README.md \
  CHANGELOG.md \
  Documents/specs/spec.md

git commit -m "feat(reader): add support for CSXXX reader

MODEL.CSXXX added to MODEL enum
BLE service UUID added to BLETransport
DeviceFinder detection added for CSXXX OUI/MAC prefix
Updated README reader table and spec matrix

Constitutes: constitution §5.4, plan T-20"
```

---

## Definition of Done

- [ ] `MODEL.CSXXX` exists in `CSLibrary.Constants.cs`
- [ ] `BLETransport.ConnectAsync` handles `MODEL.CSXXX`
- [ ] `DeviceFinder.DetectDeviceModel` identifies `MODEL.CSXXX`
- [ ] README.md updated with new reader
- [ ] CHANGELOG.md has `[Unreleased]` entry
- [ ] spec.md reader matrix updated
- [ ] `dotnet build -c Release` passes with 0 errors
- [ ] Functional test on physical hardware completed
