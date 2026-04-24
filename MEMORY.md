# MEMORY.md — Long-Term Memory

## Project: CSLibrary2026

### Project Identity
- **Repository:** https://github.com/cslrfid/CSLibrary2026
- **Type:** .NET Standard / .NET 10 NuGet library
- **Purpose:** CSL RFID Reader Library for .NET — Bluetooth LE communication with CSL RFID readers
- **License:** MIT

### GitHub Configuration
- **Token:** Configured in `auth-profiles.json` under "github" profile
- **Owner:** cslrfid
- **Repo:** CSLibrary2026 (public)
- **Token user:** mephist-cne
- **Default branch:** main
- **Clone location:** `/home/node/.openclaw/workspace-GithubProject-CSLibrary2026/CSLibrary2026/`

### CSLibrary Version Relationships

| Version | Managed By | Status |
|---------|-----------|--------|
| **CSLibrary2026** | `GithubProject-CSLibrary2026` (this agent) | Active — latest |
| **CSLibrary (older)** | `GithubProject-CSLibrary` | Legacy — being replaced |
| **CS463-203X-468XJ-PC-Callback-Unified-SDK-App** | `GithubProject-CS463-203X-468XJ-PC-Callback-Unified-SDK-App` | WinForms app, uses CSLibrary2024 |

**CSLibrary2026 is intended to replace all previous CSLibrary versions.** Overlapping concerns coordinated with `GithubProject-CSLibrary`.

### Inter-Agent Communication
- **GithubProject-CSLibrary**: Inbound works; outbound sessions_send initially timed out but resolved by 05:46 UTC.
- **GithubProject-CSLRFIDReader-C-Sharp-MAUIAPP**: Inbound works; same. Currently integrating DeviceFinder.
- Both agents can reach me; my outbound replies sometimes time out but generally work.

### Pending Actions
- ~~**NuGet publish**~~: ✅ Done — published to https://www.nuget.org/packages/CSLibrary2026/1.0.0-beta.1 (2026-04-01 09:16 UTC)
- ~~**CS203XL Bluetooth**~~: ✅ Fixed
- ~~**CS468 Bluetooth**~~: ✅ Already supported — same UUID 0x9800 as CS108, OEM data distinguishes post-connect
- **WinForms + Windows.Devices.Bluetooth HAL**: Future phase
- **R1000/R2000 TCP**: Future phase

---

## Architecture

### This is a standalone library project. NOT the MAUI app.

- **CSLibrary2026** = the library itself
  - Hosts `HighLevelInterface` / `RFIDReader` / `BarcodeReader` classes
  - Packaged as `CSLibrary2026` NuGet package
  - Target frameworks: `netstandard2.0` and `net10.0`

- **CSLRFIDReader-C-Sharp-MAUIAPP** = separate consumer project
  - GitHub: `cslrfid/CSLRFIDReader-C-Sharp-MAUIAPP`
  - MAUI mobile app that references CSLibrary2026 as NuGet dependency
  - Managed by a different agent

### HAL Architecture

`Source/HAL/` contains multiple BLE/connectivity implementations. Each is a `partial class` that combines with the base HAL class at compile time:

| Folder | Backend | WinForms? | Status |
|--------|---------|-----------|--------|
| `Plugin.BLE/` | Plugin.BLE (Xamarin/MAUI) | ❌ | Active — primary |
| `Windows.Devices.Bluetooth/` | **NEW — planned** | ✅ | Not yet implemented |
| `btframework/` | wclBluetooth wrapper | ✅ | CSLibrary2024 compat only |
| `TCPIP/` | Raw TCP socket | ✅ | CS203XL only (E910) |
| `MvvmCross.Plugin.BLE/` | MvvmCross BLE | ❌ | Alternate |
| `UWP/` | UWP BLE | ❌ | Alternate |

### WinForms + BluetoothLE Plan (.NET 10 Windows)

Goal: Use `Windows.Devices.Bluetooth` (UWP/WinRT, .NET 10 Windows) instead of `btframework` for WinForms support.

Pattern: Same `partial class` HAL approach — new `Source/HAL/Windows.Devices.Bluetooth/DeviceFinder.cs` and `CodeFileBT.cs`.

Key references from older CSLibrary (btframework HAL):
- `DeviceFinder` partial class with `wclBluetoothManager`, `wclBluetoothRadio`
- `DeviceFinderArgs` and `DeviceInfomation` with `macAdd`, `nativeDeviceInformation`
- BLE connect via `wclBluetoothDevice.Open()` → GATT service discovery

---

## Dependencies
- `Plugin.BLE 3.0.0` — Bluetooth LE abstraction (Xamarin/MAUI compatible)

---

## Supported RFID Readers & Communication Protocols

There are **two distinct CSL Bluetooth/USB API specifications**:

| API Specification | Readers | Chipset Commands |
|---|---|---|
| **CSL CS108 Bluetooth/USB API** | CS108, CS468 (Bluetooth only) | Rx000 |
| **CSL CS710 Bluetooth/USB API** | CS710S, CS203XL | E710 / E910 |

| Reader | Bluetooth | USB | TCP/IP | Chipset |
|--------|-----------|-----|--------|---------|
| **CS108** | ✅ CSL CS108 API | ✅ CSL CS108 API | — | Rx000 |
| **CS468** | ✅ CSL CS108 API | ❌ Direct Rx000 commands | ❌ Direct Rx000 commands | Rx000 |
| **CS710S** | ✅ CSL CS710 API | ✅ CSL CS710 API | — | E710 |
| **CS203XL** | ✅ CSL CS710 API | ✅ CSL CS710 API | ✅ CSL CS710 API | E910 |

**Key distinction for CS468:** Bluetooth uses the CSL CS108 API, but USB and TCP/IP bypass the protocol layer and send raw Rx000 MAC register commands directly.

---

## Chipset Assignments

| Chipset | Models |
|---------|--------|
| **Rx000** | CS101, CS203, CS208, CS209, CS103, CS206, CS333, CS468, CS468INT, CS463, CS469, CS203X, CS468XJ, CS108 |
| **E710** | CS710S |
| **E910** | CS203XL |

---

## ⚠️ CRITICAL: Two Chipset Architectures

### 1. E710 / E910 Series (CS710S, CS203XL)
- **Control:** High-level command/response protocol via byte stream
- **How it works:** Host sends commands → firmware interprets and manages Ex10 chip internally. Host does NOT directly touch registers.
- **Code:** `Source/RFIDReader/Comm_Protocol/Ex10Commands/` + `Source/RFIDReader/CSLUnifiedAPI/Basic_API/CS710S/`

### 2. Rx000 Series (CS108, CS468, CS463, CS203X, CS468XJ, legacy)
- **Control:** Direct MAC register read/write via byte stream
- **How it works:** Host directly programs Rx000 MAC registers. Reader firmware is a UART bridge.
- **Code:** `Source/RFIDReader/Comm_Protocol/RX000Commands/` + `Source/RFIDReader/CSLUnifiedAPI/Basic_API/CS108/`

---

## R1000/R2000 TCP Protocol — NOT in older CSLibrary repo

**Important:** R1000/R2000 TCP hardware support was NOT found in the older CSLibrary repo. TCP implementation there is only for CS710S (`HighLevelInterface.TCP_Connect` in `CodeFileCommNet.cs`).

CS203XL TCP already works in CSLibrary2026 via `Source/HAL/TCPIP/`.

R1000/R2000 TCP protocol (for CS463, CS203X, CS468XJ models) needs to be sourced from elsewhere — OEM firmware specs or different legacy codebase.

---

## NetFinder Reference (from older CSLibrary)

Full NetFinder (`CSLibrary/Net/CSLibrary.Net.Finder.cs`) provides:
- UDP broadcast discovery on port 3000
- TFTP firmware upgrade support (`TFTP.cs`)
- DHCP and static IP configuration
- Windows Firewall API management
- Device modes: `Unknown`, `Bootloader`, `Normal`
- Operation modes: `SEARCH`, `ASSIGN`, `UPDATE`, `IDLE`, `CLOSE`
- Result enum: `OK`, `FAIL`, `OPERATION_BUSY`, `DATA_NOT_FOUND`, etc.

CS203XL NetFinder in CSLibrary2026 is a simplified subset.

---

## Key Code Locations

| Component | Path |
|---|---|
| Main entry | `Source/CSLibrary.cs` |
| BLE/HAL (Plugin.BLE) | `Source/HAL/Plugin.BLE/CodeFileBLE.cs` |
| **DeviceFinder (Plugin.BLE)** | `Source/HAL/Plugin.BLE/DeviceFinder.cs` ← NEW |
| BT Protocol | `Source/BluetoothProtocol/` |
| RFID API (CS108) | `Source/RFIDReader/CSLUnifiedAPI/Basic_API/CS108/` |
| RFID API (CS710S) | `Source/RFIDReader/CSLUnifiedAPI/Basic_API/CS710S/` |
| Barcode | `Source/BarcodeReader/` |
| Frequency/Country | `Source/SystemInformation/ClassFrequencyBandInformation.cs` |

---

## Important Fixes Applied
- `.csproj` had `Readme.md` (capital R) but file was `README.md` — fixed to match
- README.md was created and moved to repo folder for manual upload
- .NET SDK 10 installed at `/home/node/.dotnet` to support `net10.0` target
- NuGet package built: `nupkg/CSLibrary2026.0.0.1.nupkg`

---

## Notes
- This workspace IS the CSLibrary2026 project workspace
- README.md lives at `CSLibrary2026/README.md` (repo root)
- .NET 10 SDK location: `/home/node/.dotnet`

## 2026-04-02 — ITransport Refactor Complete

### What was done
Extracted BLE and TCP transport into standalone classes implementing `ITransport`:

- `Source/Transport/ITransport.cs` — interface
- `Source/Transport/BLETransport.cs` — wraps Plugin.BLE (GATT, characteristics, connection-loss)
- `Source/Transport/TCPTransport.cs` — wraps CSLTCPSTREAM (TCP socket)

`HighLevelInterface` holds `_transport` (ITransport) set by whichever `ConnectAsync` is called, enabling `BTSend.cs` to call `_transport.SendAsync()` generically regardless of BLE or TCP.

### Key changes
- `Source/HAL/Plugin.BLE/CodeFileBLE.cs` — `ConnectAsync` delegates to `_bleTransport`, sets `_transport`
- `Source/HAL/TCPIP/CodeFileTCPIP.cs` — `ConnectAsync` sets `_transport = _tcpTransport`
- `Source/CSLibrary.cs` — removed obsolete `BLE_Init()` call

### Also fixed
- `MODEL.CS468` added to BLE service UUID switch (was falling through = couldn't connect)

### Build: ✅ clean (all 3 TFMs)
### NuGet: local only — NOT published (1.0.0-beta.1 on nuget, no beta.2 yet)

### MAUI app: zero changes needed ✅

## 2026-04-02 - GitHub Sync Set Up

### GitHub Token
- Configured: `<SEE ~/.openclaw/secrets/vault.md>` (user: mephist-cne)
- Stored: `/home/node/.openclaw/agents/githubproject-cslibrary2026/agent/auth-profiles.json` → `profiles.github.token`
- Repo: `cslrfid/CSLibrary2026` (public), default branch: `main`
- Token scope: `repo`

### Git Sync
- Cloned to: `/tmp/CSLibrary2026-git`
- Remote: `https://mephist-cne:<SEE ~/.openclaw/secrets/vault.md>@github.com/cslrfid/CSLibrary2026.git`
- Initial commit pushed: ✅ (167 files, 89604 insertions)
- Push script: `/home/node/.openclaw/workspace-GithubProject-CSLibrary2026/scripts/push-cslibrary.sh`

### Daily Cron Job
- Job ID: `d2e686a5-97e0-4852-8884-9edaa227c32c`
- Schedule: `30 18 * * *` UTC (6:30 PM UTC daily)
- Target: `agent:githubproject-cslibrary2026:main`, isolated session
- Message: instructs agent to sync + commit + push CSLibrary2026
- Delivery: announce to webchat
- Next run: 1775205000000 (Fri Apr 3 18:30 UTC)

## Operating Mode (2026-04-17)

**OpenClaw = Project Manager only. NEVER modify code directly.**

### Toolchain
- **OpenCode:** `/home/convergenceclaw/.local/bin/opencode` (v1.4.3) — AI coding agent
- **specify CLI:** `/home/convergenceclaw/.local/bin/specify` (v0.6.1) — spec management
- **.NET SDK:** `/home/convergenceclaw/.dotnet/dotnet` (v10.0.202)

### Confirmed: OpenCode Delegation Works ✅
Tested 2026-04-17: OpenCode correctly implemented a constrained task (added 1 line, build passed).

### Workflow
1. **Analyze** — OpenClaw reads files, understands codebase
2. **Write Task File** → `.specify/tasks/<slug>.md`
   - MUST include HARD CONSTRAINTS section
   - MUST include English-only rule
   - MUST specify exact lines to modify
3. **Delegate to OpenCode** → `opencode run` with task path + constraints
4. **OpenCode implements** → commits to feature branch
5. **OpenClaw reviews** → `dotnet pack -c Release` + merge

### Key Files
- `DELEGATE.md` — full delegation protocol (updated 2026-04-17)
- `.specify/` — official spec-kit directory (templates, tasks, memory)

### 🔴 HARD RULE: English Only
All project content MUST be in English — spec files, tasks, comments, commit messages, docs.

### OpenCode Constraints (Lesson Learned)
- OpenCode is AGGRESSIVE — will restructure/delete if not constrained
- Every task MUST have HARD CONSTRAINTS: DO NOT delete/change outside spec
- Specify exact line numbers or code snippets
- Test on a branch first before any real work

## Assigned Tasks

### Daily Issue Check (09:00 HKT)
- Check GitHub issues section of cslrfid/CSLibrary2026
- Analyze open issues
- Explain problems and solutions in control UI (webchat)

### Daily Backup to develop (18:30 HKT)
- Commit and push changes to `develop` branch daily at 18:30 HKT
- Backup any project changes
