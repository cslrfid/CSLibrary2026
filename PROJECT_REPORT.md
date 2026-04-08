# CSLibrary2026 — Project Report

**Repository:** https://github.com/cslrfid/CSLibrary2026  
**Branch:** `develop` (active development)  
**Version:** 0.0.1  
**Last Updated:** 2026-03-31  
**License:** MIT (Copyright © 2026 Convergence Systems Limited)  

---

## 📋 Project Overview

**CSLibrary2026** is Convergence Systems Limited's next-generation C# RFID reader SDK. It's a modern, multi-target .NET library supporting Bluetooth LE communication with CSL RFID readers (CS108, CS710S, CS203XL).

### Key Characteristics
- **Targets:** .NET Standard 2.0 + .NET 10.0 (cutting-edge)
- **BLE Support:** Plugin.BLE 3.0.0
- **NuGet Ready:** Configured for package publishing
- **Architecture:** Unified API layer over hardware-specific implementations

---

## 📦 Project Structure

```
CSLibrary2026/
├── CSLibrary2026.csproj          # Project file (.NET Standard 2.0 + net10.0)
├── Properties/
│   └── AssemblyInfo.cs
├── Source/
│   ├── HAL/                       # Hardware Abstraction Layer
│   │   ├── Plugin.BLE/           # BLE via Plugin.BLE (current)
│   │   ├── MvvmCross.Plugin.BLE/ # BLE via MvvmCross
│   │   ├── btframework/           # wclBluetoothFramework (proprietary BLE)
│   │   ├── TCPIP/                # Ethernet/TCP communication
│   │   └── UWP/                  # UWP-specific BLE
│   ├── RFIDReader/
│   │   ├── CSLUnifiedAPI/        # Unified public API
│   │   │   ├── Basic_API/        # Core RFID operations
│   │   │   │   ├── CS108/        # CS108-specific implementation
│   │   │   │   ├── CS710S/       # CS710S-specific implementation
│   │   │   │   └── ClassRFID.*   # Common base classes
│   │   │   ├── Basic_Constants/  # Enums, constants
│   │   │   ├── Basic_Events/     # Event definitions
│   │   │   ├── Basic_Structures/ # Data structures
│   │   │   ├── TAG_ASYN/         # Async tag operations
│   │   │   ├── TAG_EM4325/       # EM4325 sensor tag support
│   │   │   └── TAG_FM13DT160/    # FM13DT160 sensor tag support
│   │   └── Comm_Protocol/        # Low-level protocol handlers
│   │       ├── Ex10Commands/     # EX10 chip commands
│   │       └── RX000Commands/    # RX000 chip commands
│   ├── BarcodeReader/            # Integrated barcode scanner
│   ├── Battery/                   # Battery management
│   ├── BluetoothIC/              # Bluetooth IC control
│   ├── BluetoothProtocol/         # BLE protocol implementation
│   ├── Notification/              # System notifications
│   ├── SiliconLabIC/             # Silicon Lab IC support
│   ├── SystemInformation/         # Device info, country/freq tables
│   └── Tools/                    # Utilities (CRC, TCP, FIFO, etc.)
└── LICENSE
```

---

## 🔑 Key Files

| File | Purpose |
|------|---------|
| `CSLibrary2026.csproj` | Multi-target project (netstandard2.0 + net10.0) |
| `Source/CSLibrary.cs` | Main library entry point |
| `Source/RFIDReader/CSLUnifiedAPI/Basic_API/ClassRFID.cs` | Core RFID class |
| `Source/RFIDReader/CSLUnifiedAPI/Basic_API/ClassRFID.UnifiedAPI.cs` | Unified API facade |
| `Source/RFIDReader/CSLUnifiedAPI/Basic_Constants/CSLibrary.Constants.cs` | Constants/enums |
| `Source/RFIDReader/CSLUnifiedAPI/Basic_Events/CSLibrary.Events.cs` | Event definitions |
| `Source/HAL/Plugin.BLE/CodeFileBLE.cs` | BLE hardware abstraction |

---

## ⚙️ .NET Targets

| Target | Purpose |
|--------|---------|
| `netstandard2.0` | Cross-platform (Xamarin, legacy .NET) |
| `net10.0` | Modern .NET 10 (latest) |

---

## 📦 Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `Plugin.BLE` | 3.0.0 | Bluetooth Low Energy |

---

## 🏷️ NuGet Configuration

```xml
PackageId: CSLibrary2026
Title: CSL RFID Reader Library
Description: CSL RFID Reader Library for .NET - Supports Bluetooth LE communication
License: MIT
Tags: RFID; CSL; Bluetooth; BLE; Reader; UHF
```

---

## 📊 Current Status

### Git Info
- **Branches:** `main` (stable), `develop` (active)
- **Latest commit:** `develop` branch updated with full source
- **Version:** 0.0.1 (pre-release)

### Repository Health
| Metric | Status |
|--------|--------|
| Open Issues | Unknown |
| Open PRs | Unknown |
| CI/CD | Not configured yet |
| NuGet published | No |

### ⚠️ Observations
1. Many `.bak` files present (e.g., `ClassRFID.Public.Power.cs.bak`) — may need cleanup
2. `net10.0` is very cutting-edge (.NET 10 not yet released as of 2026) — may need adjustment
3. Some excluded paths (Antenna, EM4325, FM13DT160) may need to be re-enabled later
4. No GitHub Actions CI/CD configured yet
5. README.md still minimal ("# CSLibrary2026")

---

## 📝 Task List

### Phase 1 — Immediate Cleanup
- [ ] Remove `.bak` files from source tree
- [ ] Update `net10.0` to `net8.0` or `net9.0` (depending on actual .NET release status)
- [ ] Expand README.md with overview, installation, usage examples
- [ ] Verify build succeeds locally

### Phase 2 — CI/CD & Release
- [ ] Add GitHub Actions workflow (build + NuGet publish)
- [ ] Configure code signing (if needed)
- [ ] Add code coverage / unit tests
- [ ] Create NuGet package and publish

### Phase 3 — Documentation
- [ ] Write API documentation
- [ ] Add usage examples / demo app reference
- [ ] Document supported readers and firmware versions
- [ ] Add CHANGELOG.md

### Phase 4 — Ongoing
- [ ] Enable excluded components (Antenna, EM4325, FM13DT160) if needed
- [ ] Keep Plugin.BLE updated
- [ ] Monitor for issues and contributions

---

## 🔗 Related CSL Repositories

| Repo | Description |
|------|-------------|
| [CSLibrary](https://github.com/cslrfid/CSLibrary) | Original SDK (legacy) |
| [CSLibrary2024](https://github.com/cslrfid/CSLibrary) | Previous generation (.NET 8 MAUI) |
| [CSLRFIDReader-C-Sharp-MAUIAPP](https://github.com/cslrfid/CSLRFIDReader-C-Sharp-MAUIAPP) | Demo MAUI app |

---

*Report generated by GithubProject-CSLibrary2026 agent — 2026-03-31*
