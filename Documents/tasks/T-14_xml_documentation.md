# T-14 — Complete XML Documentation on All Public Types

**Status:** Pending
**Phase:** 2 — Documentation
**Priority:** 🟡 Medium
**Estimated:** 2 days
**Constitution:** §10.1 (Inline XML Docs — every public type and member must have `/// <summary>`)
**Spec reference:** NFR-M02 (every public type and member SHALL have XML documentation)

---

## Objective

Audit and document all undocumented public types and members in the 6 public entry-point namespaces.

---

## Scope: What to Document

### Public Entry Points (from constitution §5.1)

| Namespace | Entry Point | Priority |
|---|---|---|
| `CSLibrary` | `RFIDReader` | 🔴 High |
| `CSLibrary` | `BarcodeReader` | 🔴 High |
| `CSLibrary` | `Battery` | 🟡 Medium |
| `CSLibrary` | `DeviceFinder` (static) | 🔴 High |
| `CSLibrary` | `Notification` | 🟡 Medium |
| `CSLibrary` | `Debug` (static) | 🟢 Low |
| `CSLibrary.Constants` | All public enums and constants | 🟡 Medium |
| `CSLibrary.Events` | All `On*EventArgs` classes | 🔴 High |
| `CSLibrary.Structures` | All public structs | 🟡 Medium |

---

## Step 1 — Generate an Undocumented Members List

```bash
cd CSLibrary2026
export PATH="/home/node/.dotnet:$PATH"

# Use a tool to check for undocumented public members
# Option A: Use DocFX to generate a report
# Option B: Manual audit using reflection

# Quick manual approach — list all public classes and check each:
find Source -name "*.cs" \
  | xargs grep -l "^namespace CSLibrary" \
  | xargs grep -E "^\s*(public|internal public)" \
  | grep -v "internal" | head -40
```

---

## Step 2 — Document Them

For each undocumented public type, add:

```csharp
/// <summary>
/// [One clear sentence describing what this type does, in present tense.
/// Include the reader model or transport if relevant.]
/// </summary>
public class RFIDReader
{
    /// <summary>
    /// Initiates a BLE connection to a CSL RFID reader.
    /// </summary>
    /// <param name="adapter">The Plugin.BLE IAdapter instance.</param>
    /// <param name="device">The Plugin.BLE IDevice returned from ConnectToDeviceAsync.</param>
    /// <param name="model">The CSL reader model, e.g. MODEL.CS108.</param>
    /// <returns>True if the connection was established successfully.</returns>
    /// <exception cref="ArgumentException">Thrown when adapter or device is null.</exception>
    public async Task<bool> ConnectAsync(IAdapter adapter, IDevice device, MODEL model);
}
```

---

## Step 3 — Priority Order

Work in this order (downstream dependencies first):

```
1. CSLibrary.Events  — all On*EventArgs (used by everything else)
2. CSLibrary.Constants — MODEL, Result, CallbackType enums
3. CSLibrary.Structures — TagCallbackInfo, INV_PKT, ACCESS_PKT
4. RFIDReader class   — public methods and events
5. DeviceFinder class — static public API
6. BarcodeReader      — public methods and events
7. Battery            — public methods
8. Notification       — public methods
9. Debug              — static utility methods
```

---

## Step 4 — Commit Per Group

```bash
git add [files in group]
git commit -m "docs(xml): document public API in [namespace/group]

Added /// <summary> to [N] undocumented public members.
Priority order: Events → Constants → Structures → RFIDReader → ...

Constitutes: constitution §10.1, plan T-14"
```

---

## Definition of Done

- [ ] All 6 public entry-point classes have class-level `/// <summary>`
- [ ] All public methods on `RFIDReader` have `/// <summary>` and `/// <param>` where applicable
- [ ] All `On*EventArgs` event arg classes have `/// <summary>`
- [ ] All public enums in `CSLibrary.Constants` have `/// <summary>` and enum-value comments
- [ ] `dotnet build -c Release` continues to pass with 0 errors (no new build breaks)
