# T-09 — Fix Compiler Warnings (472 → <20)

**Status:** Pending
**Phase:** 1 — Quality Gates
**Priority:** 🔴 High
**Estimated:** 2 days
**Constitution:** §3.1 — "fix them rather than suppressing"
**Spec reference:** Success Metrics (compiler warnings: 472 → <20)

---

## Objective

Reduce `dotnet build -c Release` warning count from 472 to below 20. The majority are `CS0649`/`CS0169`/`CS0414` unused-field warnings. Fix the root cause, not the symptom.

---

## Step 1 — Baseline: Get the Full Warning Report

```bash
cd CSLibrary2026
export PATH="/home/node/.dotnet:$PATH"
dotnet build -c Release 2>&1 | grep "warning CS" | sed 's|/.*||' | sort | uniq -c | sort -rn | head -30
```

This groups warnings by file with counts, sorted descending.

---

## Step 2 — Prioritized File Audit

Based on the current build output, here are the highest-impact files to fix, in order:

### Priority Group A — Dead Fields (delete or assign)

| File | Warning Types | Fields | Action |
|---|---|---|---|
| `Source/Tools/ClassSystemParameters.cs` | CS0649 | `_CountryCode`, `_EnableFrequencySequence`, `_deviceName`, `_MaxTargetPower`, `_SpecialCountryVersion` | Investigate: if truly dead → delete |
| `Source/RFIDReader/CSLUnifiedAPI/Basic_Structures/CSLibrary.Structures.cs` | CS0169 | `TAG_ACCESS_PKT.flags`, `TAG_ACCESS_PKT.ISO_*` flag fields, `INVENTORY_PKT.flags` | Investigate: if truly dead → delete |
| `Source/SystemInformation/ClassFrequencyBandInformation.cs` | CS0169 | `FrequencyBand_CS710S.m_save_country_list` | Delete unused field |
| `Source/HAL/Plugin.BLE/CodeFileBLE.cs` | CS0169 | `_characteristicDeviceInfoRead`, `_service` | Remove unused private fields |

### Priority Group B — Assigned but Never Read (CS0414)

| File | Warning Count | Action |
|---|---|---|
| `Source/HAL/TCPIP/Class_CSLibrary.CS203XL.NetFinder.cs` | ~3 | Remove `m_tcpsocket`, `u_timeout`, `m_timeout` if truly unused |
| `Source/BarcodeReader/ClassBarCode.cs` | ~4 | Remove `_m_state`, `_goodRead`, `_perfix` if truly unused |
| `Source/Notification/ClassNotification.cs` | ~2 | Remove `_currentAutoReportStatus`, `_receiveOnWithinOffcycle`, `_receiveOffWithin1s` |
| `Source/RFIDReader/CSLUnifiedAPI/Basic_API/ClassRFID.Private.cs` | ~1 | Remove `CurrentOperationResult` |
| `Source/RFIDReader/CSLUnifiedAPI/Basic_API/ClassRFID.Public.FrequencyChannel.cs` | ~1 | Remove `m_save_hoppingorder` |
| `Source/RFIDReader/Comm_Protocol/Ex10Commands/ClassRFID.WriteRegister.cs` | ~1 | Remove `_sequencedNumber` |
| `Source/RFIDReader/CSLUnifiedAPI/TAG_ASYN/ClassRFID.ASYN.cs` | ~1 | Remove `OnAsyncCallback` |

### Priority Group C — Partial Class Fields (use in another partial)

These may appear unused because they ARE used in a different HAL partial class file. Do NOT delete until verified across all partial class files:

```bash
# Check if a field is referenced in any other partial file
cd Source
grep -r "m_tcpsocket\|u_timeout\|m_timeout" --include="*.cs" .
```
If the field is found in another HAL partial file, it is genuinely used and the warning is a false positive from the compiler's partial-class analysis. In that case, add a `#pragma warning disable` with a comment explaining why.

---

## Step 3 — Fix Pattern

For each warning, open the file and:

1. **Field is dead** → delete it entirely
2. **Field is used in another partial** → add `#pragma` at the field declaration:
   ```csharp
   #pragma warning disable CS0169 // Used by Source/HAL/TCPIP/CodeFileTCPIP.cs
   private object m_tcpsocket;
   #pragma warning restore CS0169
   ```
3. **Field is initialized but never read** → check if it should be exposed as a property or if the assignment is accidental

---

## Step 4 — PR Per Logical Group

Split into multiple PRs to keep reviews manageable:

```
PR-1: fix(warnings): resolve unused fields in Tools/ and SystemInformation/
PR-2: fix(warnings): resolve unused fields in HAL/ (TCPIP, Plugin.BLE)
PR-3: fix(warnings): resolve unused fields in RFIDReader/Basic_Structures
PR-4: fix(warnings): resolve unused fields in remaining files
```

---

## Commands for Each PR

```bash
# After fixing files in the group
cd CSLibrary2026
export PATH="/home/node/.dotnet:$PATH"

# Check warning count for just the files you touched
dotnet build -c Release 2>&1 | grep -c "warning CS"
# Target: < 20 total after all PRs merged

# Commit
git add [fixed files]
git commit -m "fix(warnings): resolve unused fields in [affected folder]

Dead fields deleted: [list]
#pragma added for partial-class fields: [list]
Compiler warnings reduced: 472 → [new count]

Constitutes: constitution §3.1, plan T-09"
```

---

## Definition of Done

- [ ] `dotnet build -c Release 2>&1 | grep -c "warning CS"` returns < 20
- [ ] No `CS0649` or `CS0169` warnings (these are the bulk of the 472)
- [ ] All PRs merged to `develop`
- [ ] Build passes on both `netstandard2.0` and `net10.0`
