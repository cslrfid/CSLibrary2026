# AGENTS.md

## Project scope
- `CSLibrary2026` is a C# library for controlling CSL RFID readers, intended to carry forward/inherit functionality from previous CSLibrary implementations.
- Legacy/reference codebase: `S:\Projects\GitHub\CSLibrary` (useful for comparing behavior/APIs when porting). `CSLibrary2024` may appear only as a transitional reference during porting and is intended to be replaced by `CSLibrary2026`.
- Hardware/protocol reference docs: `Documents/` (e.g. CS108/CS463 and CS710S Bluetooth/USB byte stream API specifications); internal notes/developer map: `Documents/PROJECT_NOTES.md`.
- Reference UI/demo apps (for integration patterns and usage examples):
  - `S:\Projects\GitHub\CS108-Mobile-CSharp-DotNetStd-App-v4`
  - `S:\Projects\GitHub\CS108-Windows-CSharp-Desktop-Demo`
  - `S:\Projects\GitHub\CS710S-C-Sharp-APP-for-iOS-ANDROID-UWP`
  - `S:\Projects\GitHub\CS710S-C-Sharp-APP-for-MAUI`
  - `S:\Projects\GitHub\CS710S-Windows-CSharp-Desktop-Demo`
  - `C:\Projects\GitHub\CS710S-C-Sharp-MAUIAPP`
  - `C:\Projects\GitHub\CS203XL-C-Sharp-APP-for-iOS-ANDROID-UWP`
- All repository files should be stored in **English** (content/filenames/docs).

## Build & project model
- Primary project file: `CSLibrary2026.csproj` (SDK-style, `<Project Sdk="Microsoft.NET.Sdk">`, root namespace/assembly `CSLibrary` / `CSLibrary2026`). Treat this as the authoritative project definition.
- Different Visual Studio / .NET SDK versions may be used to compile different target frameworks (e.g. older targets such as .NET Standard 2.0/2.1 in Visual Studio 2022, .NET 10 targets in Visual Studio 2026). Use a toolchain that supports the specific TFM you are building.
- Any source/resource file that must be part of the build must be explicitly included/referenced in `CSLibrary2026.csproj`; keep project item declarations accurate when adding/removing files.
- Current target frameworks (from csproj): `netstandard2.0;netstandard2.1;net48;net10.0;net10.0-windows10.0.19041.0;net10.0-android;net10.0-ios`. Mac Catalyst is intentionally **not** a target (the app line ships Android / iOS / Windows only; the MAUI apps' `net10.0-maccatalyst` head therefore resolves the platform-neutral `net10.0` assembly). `<EnableWindowsTargeting>true</EnableWindowsTargeting>`. The `net10.0*` targets are only added when the SDK in use is .NET 10 or newer (Visual Studio 2022 resolves to the .NET 9 SDK and cannot evaluate `net10.0-android`, which breaks restore for the Xamarin UI test apps). The Windows target carries an explicit platform version so the Windows Runtime projections are referenced — without it NuGet resolves Plugin.BLE's netstandard2.0 stub, which throws `NotImplementedInReferenceAssembly` at runtime.
- Target/platform intent (see `README.md`): support Android, iOS, and Windows across their environments (including WinForms/UWP/MAUI where applicable); plan to support ARM Linux as well. **Preferred:** a single `CSLibrary2026.csproj` serving all platforms to keep behavior consistent across the many existing UI/app variants, but feasibility (especially for targets with different project systems/toolchains such as UWP or .NET Framework-specific needs) is currently uncertain—evaluate multi-targeting/conditions first and split into separate project files only if required, while sharing the same `Source/` where possible. Keep csproj as the source of truth when adding/changing TFMs or conditionals.
- Common commands (run from repo root):
  - `dotnet restore CSLibrary2026.csproj`
  - `dotnet build CSLibrary2026.csproj -c Release`
  - `dotnet build CSLibrary2026.csproj -c Release -f netstandard2.0` (or other TFM)
  - `dotnet pack CSLibrary2026.csproj -c Release -o .\nupkg`

## Source layout & architecture
- Main entry/types: `Source/CSLibrary.cs` (`CSLibrary.HighLevelInterface`) composes subsystems (`rfid`, `barcode`, `notification`, `bluetoothIC`, `battery`, etc.).
- Core areas under `Source/`: `RFIDReader/CSLUnifiedAPI/` (unified API, reader-specific impls like `Basic_API/CS108`, `Basic_API/CS710S`), `RFIDReader/Comm_Protocol/` (`Ex10Commands`, `RX000Commands`), `BluetoothProtocol/`, `BluetoothIC/`, `SiliconLabIC/`, `HAL/`, `Transport/`, `BarcodeReader/`, `Battery/`, `Notification/`, `SystemInformation/`, `Tools/`. See `Documents/PROJECT_NOTES.md` for a selected developer file map.
- Reader/chipset families: CSL RFID readers use multiple Impinj chipsets—**R1000/R2000** (largely same command set with minor differences, collectively "Rx1000") and **E710/E910** (largely same command set with minor differences, collectively "Ex10"). Readers also expose different interfaces; legacy CSLibrary often used multiple projects with small modifications per reader, while **CSLibrary2026 aims to consolidate support for all readers into a single library**—design changes with this unification in mind.
- Transport/connection modes in this repo currently include BLE and TCP/IP (`Transport/ITransport`, `BLETransport`, `TCPTransport`; see `README.md` and `Source/CSLibrary.cs` connection/disconnect paths). Network interface support expected across the library line: **Bluetooth (4/5), TCP/IP, Serial, USB**.
- Serial/USB reference/porting guidance: see `S:\Projects\GitHub\CSLibrary\CSLibrary` (contains CS203 connection code using Serial and USB).
- HAL notes (csproj): `Source/HAL/Plugin.BLE` and `Source/HAL/TCPIP` are included; legacy/alternate HALs and some API folders are explicitly removed from compilation (e.g. `Acr.ble`, `btframework`, `UWP`, `MvvmCross.Plugin.BLE`, `WinFormsBLE`, `API_EM4325`, `API_FM13DT160`, etc.). `TCP` is defined globally via `<DefineConstants>TCP</DefineConstants>`. Target HAL driver model (plug-in, one protocol per `Source/HAL/<DriverName>/`, multiple/single selectable): see `Documents/HAL_DRIVER_FORMAT.md` (current flow via `ITransport` + HAL partials and proposed driver contract/registration).
- HAL selection per target framework (csproj): the `net48` target (WinForms desktop app) uses the **btframework** HAL instead of Plugin.BLE — Plugin.BLE 3.x cannot run on .NET Framework. `Source/HAL/btframework/**` is compiled for `net48` only and `Source/HAL/Plugin.BLE/**` plus `Source/Transport/BLETransport.cs` are excluded for it. Both HALs provide the same `HighLevelInterface` partial members and the same `CSLibrary.DeviceFinder` API, so they are mutually exclusive. The btframework HAL needs the WCL assemblies in `Source/HAL/btframework/` (`wclBluetoothFramework.dll`, `wclCommon.dll`, `wclCommunication.dll`).

## CI / branching / packaging
- GitHub Actions workflows: `.github/workflows/ci.yml` (triggers on `main`/`develop`; build matrix, pack, test job) and `.github/workflows/publish-nuget.yml` (publishes to NuGet.org). Both use the repo's root layout (`CSLibrary2026.csproj`, `Tests/...`).
- Branch intent (current/evolving): `main` holds the source code **and** the NuGet publishing assets (`nuget/`). The former `develop` branch only carried a backup of those assets; it may be removed.
- NuGet metadata is defined in `CSLibrary2026.csproj` (`PackageId` CSLibrary2026, README packed from `README.md`). The published package carries the **TCP/IP + Plugin.BLE flavours only** (`lib/net10.0*`, `lib/netstandard2.0|2.1`), so NuGet selects the matching binary per consumer. The `net48` (btframework HAL) and WinFormsBLE flavours are **not published** — those consumers build the library from source (see `nuget/README.md` and README.md "BLE HAL selection"). `net10.0-ios` can only be built on a macOS agent, so the CI publish workflow packs it in a separate macOS job and merges both packages (`nuget/scripts/Merge-NuGetPackages.ps1`).
- Publishing flow and the required `NUGET_API_KEY` secret are documented in `nuget/README.md`; the automated pipeline is `.github/workflows/publish-nuget.yml`.

## Working conventions
- **CSLibrary2026's public API is the single source of truth; applications follow the library, not the other way round.** The library unifies the previously per-reader/per-HAL APIs, so its signatures may legitimately differ from the older `CSLibrary` / `CSLibrary2026` copies (example: the unified `HighLevelInterface.DisconnectAsync()` returns `void`, while the old HAL exposed `async Task<bool> DisconnectAsync()`). When an application fails to compile against this library, **fix the application** (e.g. drop the `_ =` discard) instead of adding compatibility shims or reverting the library signature. Every app in the family (MAUI, Xamarin, UWP, WinForms and the reference demo apps) is expected to be updated to the unified API.
- Prefer small, targeted changes consistent with existing partial-class/API patterns in `Source/` (many types are split across multiple files/partials).
- When adding/removing/renaming source files, update `CSLibrary2026.csproj` compile/remove item groups as needed so the VS2012-oriented project stays consistent.
- Be cautious around hardware/protocol behavior, conditional compilation, and reader-specific implementations (CS108/CS468/CS710S/CS203XL families appear in code/docs); validate against existing APIs in this repo and, when relevant, `S:\Projects\GitHub\CSLibrary`.
- No local test project is present in this repository snapshot; CI’s test steps reference a non-existent `Tests/CSLibrary2026.Tests` path—do not assume automated tests run here unless added/fixed.
