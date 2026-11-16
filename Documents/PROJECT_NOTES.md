# Project Notes

This document holds non-duplicative internal notes migrated from the former `PROJECT_REPORT.md`. For current user-facing usage and package information see `README.md`; for agent/build/architecture guidance see `AGENTS.md`; for authoritative build targets, includes/excludes, and dependencies see `CSLibrary2026.csproj`.

- HAL communication driver format (current flow + target plug-in contract): see `Documents/HAL_DRIVER_FORMAT.md`.
- Unified reader discovery across BLE / TCP-IP / Serial / USB (design proposal, not yet implemented): see `Documents/READER_DISCOVERY.md`.

## Porting context
- `CSLibrary2024` is expected to appear only during the porting process and will be replaced by `CSLibrary2026` (not a long-term parallel line).

## Reader / chipset protocol notes
Two different Impinj chipset architectures are in play; the library speaks both:

- **Rx000 series** (CS108, CS468, CS463, CS203X, CS468XJ, legacy): the host talks to the reader **directly over the byte stream**, reading and writing MAC registers (`Source/RFIDReader/Comm_Protocol/RX000Commands/`). The reader firmware is a UART bridge - it does not interpret a command protocol. CS468 is the exception within this family: its Bluetooth path uses the CSL CS108 API, while USB/TCP bypass the protocol layer and send raw Rx000 MAC register commands.
- **E710 / E910 series** (CS710S, CS203XL): the host sends high-level command/response frames that the reader firmware interprets and manages the Impinj chip internally (`Source/RFIDReader/Comm_Protocol/Ex10Commands/`). The host never touches registers directly.

- **R1000 / R2000 TCP/IP**: not implemented in the legacy `CSLibrary` repository - its TCP/IP code covers CS710S only. The protocol has to be sourced from the OEM firmware specifications.
- **Full NetFinder** (`CSLibrary/Net/CSLibrary.Net.Finder.cs` in the legacy repo) additionally does TFTP firmware upgrade, DHCP/static IP configuration and Windows Firewall API management; the CS203XL `NetFinder` in this library is a simplified subset (UDP broadcast discovery on port 3000 only). See `Documents/READER_DISCOVERY.md`.

## UI test status
Reference apps under `UITest/` validate this library on real hardware. They were copied from their `C:\Projects\GitHub\...` originals (still on `CSLibrary2024`) and are intended to replace them.

| App | Reader | Transport | UI test solution |
|---|---|---|---|
| `UITest/CS710S-C-Sharp-APP-for-iOS-ANDROID-UWP/` | CS710S | Bluetooth LE | `CS710S-C-Sharp-APP-for-iOS-ANDROID.sln` (Android head) |
| `UITest/CS203XL-C-Sharp-APP-for-iOS-ANDROID-UWP/` | CS203XL | TCP/IP (Network API) | `CS203XL-C-Sharp-APP-for-UWP.sln` (UWP head) |
| `UITest/CS710S-Windows-CSharp-Desktop-Demo/` | CS710S | Bluetooth LE (Windows stack, btframework HAL) | `CS710SDesktopDemo.sln` (WinForms, .NET Framework 4.8) |
| `UITest/CSLRFIDReader-C-Sharp-MAUIAPP/` | CS710S | Bluetooth LE (Plugin.BLE 3.0.0) | `CSLRFIDReader-C-Sharp-MAUIAPP.sln` (.NET MAUI, VS 2026, .NET 10) |

- `Library/CSLibrary2026` is a junction to the repository root, so the apps always build the working copy of the library. **SDK-style apps (the MAUI app) must not reference the library through that junction** — MSBuild's `**/*.cs` glob does not descend into a reparse point (empty assembly) and the .NET for Android AAR creation fails with `XACAAR7004`; they use a relative path to the repository root instead.
- Verified: CS710S Android build + APK + BLE communication with a reader; CS203XL UWP build + MSIX package containing `CSLibrary2026.dll`, app compiles and starts without compile/runtime errors; CS710S WinForms desktop build (net48 target with the btframework HAL); MAUI Android build + APK (VS 2026).
- Not yet verified: TCP/IP traffic against a CS203XL reader on the network; desktop demo and MAUI app against real hardware; iOS (needs a Mac).
- The `net48` target is the only one that uses the btframework HAL instead of Plugin.BLE; see `AGENTS.md` for the per-target HAL selection rules.
- Packaging: `dotnet pack` produces one package with the TCP/IP + Plugin.BLE flavours (`lib/net10.0*`, `lib/netstandard2.0|2.1`); NuGet selects the matching binary per consumer. The `net48` (btframework) and WinFormsBLE flavours are **not** published - they are built from source. `net10.0-ios` / `net10.0-maccatalyst` require a macOS agent. See `nuget/README.md` for the publishing flow.
- Build/dependency details and the CSLibrary2024 → CSLibrary2026 differences: see `UITest/README.md`.
- Open item: the legacy HAL folders under `Source/HAL/` that are currently excluded from the build (`Acr.ble`, `btframework`, `UWP`, `MvvmCross.Plugin.BLE`, `WinFormsBLE`) still keep their own GATT state and `BLE_Send`; they must be converged to `ITransport` before being re-enabled.


## Developer file map (selected)
| Path | Purpose |
|------|---------|
| `CSLibrary2026.csproj` | Project/target/dependency definition |
| `Source/CSLibrary.cs` | Main library entry (`HighLevelInterface`) |
| `Source/Transport/` | Transport abstraction and implementations |
| `Source/RFIDReader/CSLUnifiedAPI/Basic_API/ClassRFID.cs` | Core RFID class |
| `Source/RFIDReader/CSLUnifiedAPI/Basic_API/ClassRFID.UnifiedAPI.cs` | Unified API facade |
| `Source/RFIDReader/CSLUnifiedAPI/Basic_Constants/CSLibrary.Constants.cs` | Constants/enums |
| `Source/RFIDReader/CSLUnifiedAPI/Basic_Events/CSLibrary.Events.cs` | Event definitions |
| `Source/RFIDReader/Comm_Protocol/Ex10Commands/` | Ex10 (E710/E910-family) command protocol |
| `Source/RFIDReader/Comm_Protocol/RX000Commands/` | Rx000/Rx1000-family command protocol |
| `Source/HAL/Plugin.BLE/CodeFileBLE.cs` | BLE HAL (Plugin.BLE) |
| `Source/HAL/TCPIP/` | TCP/IP HAL components |