# Unified Reader Discovery

Design for a single discovery façade that searches every supported reader interface
(BLE, TCP/IP, Serial, USB) and returns readers in one uniform shape, so that an app can
list readers of several kinds at once and connect to the one the user picked.

**Status:** design proposal — not implemented. This document records the intended shape,
the work required to get there, and the decisions still to be made.

Related documents:

- HAL communication driver format (driver contract, registration, folder layout):
  `Documents/HAL_DRIVER_FORMAT.md`
- Internal notes and current status: `Documents/PROJECT_NOTES.md`
- Build targets and item groups: `CSLibrary2026.csproj`

## Motivation

Discovery is currently split across two unrelated implementations:

| Path | Entry point | Result type | Model known? |
|---|---|---|---|
| BLE | `CSLibrary.DeviceFinder` (`Source/HAL/Plugin.BLE/DeviceFinder.cs`) | `DeviceFinder.DeviceInformation { Id, Name, DeviceType, NativeDevice, Rssi }` | yes (`MODEL`) |
| TCP/IP | `CSLibrary.NetFinder.NetFinder` (`Source/HAL/TCPIP/NetFinder.cs`) | `NetFinder.DeviceInfo { DeviceName, IPAddress, MacAddress, Port }` | no |

Consequences today:

- An app cannot list BLE and TCP readers in one list without writing the merge itself.
  The two result shapes have nothing in common (BLE exposes the native `IDevice`, TCP
  exposes strings).
- The two implementations are static classes with static state, so their lifecycle
  (start / stop / completion) cannot be coordinated.
- TCP discovery returns no `MODEL`; the CS203XL app hardcodes `MODEL.CS203XL`.
- Every new interface (Serial for CS203, USB for CS468, TCP/IP for CS463) would add
  another bespoke finder instead of extending a common one.

The requirement that triggered this: the CS710S app (BLE) and the CS203XL app (TCP/IP)
should be able to search both interfaces in one pass, and connect using the interface the
selected reader was found on.

## Scope

### Goals

1. One façade that searches all available interfaces and reports every reader found in a
   uniform shape.
2. A per-interface provider contract so a new interface is added by implementing one
   interface, not by touching the core.
3. Uniform connect dispatch: the app hands the selected reader back and the library
   connects over the interface it was discovered on.
4. Backwards compatibility: the existing public finders keep working.

### Non-goals

- Simultaneous connections over several interfaces. The library holds a single
  `_transport` (`Source/CSLibrary.cs`), so one session uses one interface. Discovery may
  span interfaces; a connection may not.
- Replacing the HAL folder layout or the driver contract described in
  `Documents/HAL_DRIVER_FORMAT.md`.
- Implementing the Serial and USB interfaces. They are described here so the contract
  fits them, but their providers are future work (no hardware available yet).

## Interfaces to be supported

| Interface | Readers | Provider location | Status |
|---|---|---|---|
| BLE 4/5 | CS108, CS468, CS710S, CS203XL | `Source/HAL/Plugin.BLE/` | implementable now |
| TCP/IP (CS203XL series) | CS203XL | `Source/HAL/TCPIP/` | implementable now |
| TCP/IP (CS463 series) | CS463 | `Source/HAL/TCPIP/` (separate provider) | discovery protocol not defined yet |
| Serial port | CS203 | `Source/HAL/Serial/` | future |
| USB | CS468 | `Source/HAL/USB/` | future |

Reader model identifiers come from the `MODEL` enum in
`Source/SystemInformation/ClassDeviceInformation.cs` (CS101, CS203, CS333, CS468,
CS468INT, CS463, CS469, CS208, CS209, CS103, CS108, CS206, CS468X, CS203X, CS468XJ,
CS710S, CS203XL).

## Proposed API

Names below are proposals; the namespace and the enum granularity are open decisions
(see [Open decisions](#open-decisions)).

```csharp
namespace CSLibrary.Discovery
{
    /// <summary>
    /// Interface a reader can be reached over.
    /// </summary>
    public enum RFIDREADERINTERFACE
    {
        /// <summary>Bluetooth LE (4/5) — CS108, CS468, CS710S, CS203XL.</summary>
        BLUETOOTH,

        /// <summary>TCP/IP Network API — CS203XL series (and CS463 in future).</summary>
        TCPIP,

        /// <summary>Serial port — CS203 series.</summary>
        SERIAL,

        /// <summary>USB — CS468 series.</summary>
        USB,
    }

    /// <summary>
    /// A reader discovered over one interface. Interface independent: the app can list
    /// BLE and TCP readers in the same collection and hand the selected entry back for
    /// connecting.
    /// </summary>
    public class RFIDReaderInfo
    {
        /// <summary>Reader name as reported by the device (BLE name / Network API name).</summary>
        public string Name { get; }

        /// <summary>Reader model. May be <see cref="RFIDDEVICE.MODEL.UNKNOWN"/> when the
        /// discovery protocol does not carry the model (current TCP/IP broadcast).</summary>
        public RFIDDEVICE.MODEL Model { get; }

        /// <summary>Interface the reader was discovered on.</summary>
        public RFIDREADERINTERFACE Interface { get; }

        /// <summary>Human readable address: BLE MAC, or "ip:port" for TCP/IP.</summary>
        public string Address { get; }

        /// <summary>Signal strength in dBm for radio interfaces, 0 otherwise.</summary>
        public int SignalStrength { get; }

        /// <summary>
        /// Native handle required to connect: the Plugin.BLE <c>IDevice</c> for BLE,
        /// null for interfaces that only need an address.
        /// </summary>
        public object NativeHandle { get; }
    }

    /// <summary>
    /// Discovery capability of one communication interface. Implemented once per
    /// interface in its HAL folder; the core never depends on a concrete interface.
    /// </summary>
    public interface IReaderDiscovery
    {
        /// <summary>Interface this provider searches.</summary>
        RFIDREADERINTERFACE Interface { get; }

        /// <summary>Reader models this interface can serve (for app side filtering).</summary>
        RFIDDEVICE.MODEL[] SupportedModels { get; }

        /// <summary>False when the interface is not usable on this platform or build.</summary>
        bool IsAvailable { get; }

        /// <summary>Begins searching.</summary>
        void Start();

        /// <summary>Stops searching.</summary>
        void Stop();

        /// <summary>Readers found so far.</summary>
        IReadOnlyList<RFIDReaderInfo> Readers { get; }

        /// <summary>Raised for every reader discovered.</summary>
        event EventHandler<RFIDReaderInfo> OnReaderFound;

        /// <summary>Raised when this provider finished searching.</summary>
        event EventHandler OnSearchCompleted;
    }

    /// <summary>
    /// Unified reader search across all registered interfaces.
    /// </summary>
    public class RFIDReaderFinder
    {
        /// <summary>Registers a provider. Called by the HALs during library start-up.</summary>
        public static void Register(IReaderDiscovery provider);

        /// <summary>Starts every available provider.</summary>
        public static void StartSearch();

        /// <summary>Stops every running provider.</summary>
        public static void StopSearch();

        /// <summary>All readers found by the last search, across all interfaces.</summary>
        public static IReadOnlyList<RFIDReaderInfo> GetReaders();

        /// <summary>Raised for every reader found, on any interface.</summary>
        public static event EventHandler<RFIDReaderInfo> OnReaderFound;

        /// <summary>Raised once all providers have finished.</summary>
        public static event EventHandler OnSearchCompleted;
    }
}
```

## Provider implementations

Each provider lives in the HAL folder of the interface it serves and converts that
interface's native result into `RFIDReaderInfo`.

### BLE provider

Wraps the existing `CSLibrary.DeviceFinder` (already compiled into every target):

- `Initialize(IAdapter, IBluetoothLE)` must have been called before `StartDeviceSearch()`.
- `DeviceFinder.DeviceInformation` maps to `RFIDReaderInfo` as
  `Name = DeviceInformation.Name`, `Model = DeviceInformation.DeviceType`,
  `Address = DeviceInformation.Id`, `SignalStrength = DeviceInformation.Rssi`,
  `NativeHandle = DeviceInformation.NativeDevice`.
- The model detection already lives in the library
  (`DeviceFinder.DetectDeviceModel`: advertisement service UUIDs `00009800-…` → CS108,
  `00009802-…` → CS710S, plus MAC OUI fallbacks), so an app no longer has to parse
  advertisement bytes itself — the CS710S app does that today in
  `MobileMvxApp/BLE.Client/ViewModels/DeviceListViewModel.cs`.

### TCP/IP provider

Wraps `CSLibrary.NetFinder.NetFinder` (the singleton façade over
`CSLibrary.NetFinder.CS203XL.NetFinder`, which listens for the UDP broadcast on port 3000
on a background thread):

- `NetFinder.DeviceInfo` maps to `RFIDReaderInfo` as `Name = DeviceName`,
  `Address = $"{IPAddress}:{Port}"`, `Interface = TCPIP`, `NativeHandle = null`.
- The CS203XL broadcast reply carries no model, so `Model` is set by the provider
  (`MODEL.CS203XL` for the CS203XL protocol); a future CS463 provider sets its own.
- The CS203XL implementation type also exposes `DeviceInfomation` with `IP` / `MAC`
  structs; those are converted to strings in the mapping.

### Serial / USB providers (future)

`Source/HAL/Serial/` and `Source/HAL/USB/` follow the same pattern: enumerate ports or
devices, map each to `RFIDReaderInfo` with `Interface = SERIAL` / `USB`, and expose the
address needed by the corresponding `ConnectAsync` overload.

## Registration and lifecycle

- Providers are registered during library start-up, next to the existing HAL wiring in
  `Source/CSLibrary.cs` (where `_transport`, the reader state and the timers are set up).
- The TCP/IP provider must only be registered when the TCP HAL is compiled in, i.e. under
  the same `#if TCP` guard as `Source/HAL/TCPIP/*` (`<DefineConstants>TCP</DefineConstants>`
  in `CSLibrary2026.csproj`).
- `StartSearch()` starts every provider with `IsAvailable == true`; `StopSearch()` stops
  all of them.
- `RFIDReaderFinder.OnSearchCompleted` fires **after all providers have finished**, so
  apps get one consistent "search finished" signal instead of one per interface.
- Providers raise their events on their own threads (BLE: the Plugin.BLE callback
  thread; TCP/IP: the UDP receive thread). Either the façade marshals to a single thread
  (capture `SynchronizationContext.Current` in `StartSearch`) or the contract documents
  that events arrive on background threads. The CS203XL app already marshals with
  `InvokeOnMainThread` in `DeviceListViewModel.AddOrUpdateDevice`.

## Connect dispatch

The selected reader carries the information needed to connect:

```csharp
// BLE
await _reader.ConnectAsync(adapter, (IDevice)reader.NativeHandle, reader.Model);

// TCP/IP
await _reader.ConnectAsync(reader.Address, port);
```

Two options were considered:

| Option | Description | Trade-off |
|---|---|---|
| A | App dispatches on `reader.Interface` | Small library change, but every app repeats the dispatch |
| B (preferred) | Library adds `HighLevelInterface.ConnectAsync(RFIDReaderInfo reader)` and dispatches internally | One implementation for all apps; `RFIDReaderInfo` becomes the library's reader handle |

Option B keeps the dispatch next to the HAL code that owns the connect overloads
(`Source/HAL/Plugin.BLE/CodeFileBLE.cs` and `Source/HAL/TCPIP/CodeFileTCPIP.cs`).

## Compatibility

The following are public API today and must keep working:

| Type | Role after the change |
|---|---|
| `CSLibrary.DeviceFinder` | stays as the BLE implementation; the BLE provider calls it |
| `CSLibrary.NetFinder.NetFinder` | stays as the TCP/IP implementation; the TCP provider calls it |
| `CSLibrary.TCPDeviceFinder` | legacy TCP finder (`Source/HAL/TCPIP/ClassTCPDeviceFinder.cs`); redundant with `NetFinder`, may be marked `[Obsolete]` later |

The unified façade is additive; no existing type changes behaviour. Once apps use
`RFIDReaderFinder`, the legacy finders can be marked obsolete in a later release.

## Platform prerequisites

Multi-interface search adds requirements that single-interface apps did not have:

- **Android** — the UDP broadcast used by the TCP/IP discovery is subject to Wi-Fi power
  saving; a `WifiManager.MulticastLock` is required for the broadcast replies to arrive.
  The library currently has no multicast lock (no `MulticastLock` usage anywhere under
  `Source/`), so the TCP/IP provider (or an Android specific HAL) has to acquire one.
- **iOS** — local network access needs `NSLocalNetworkUsageDescription` in `Info.plist`
  (iOS 14+); neither UI test app declares it today. UDP broadcast additionally needs the
  multicast entitlement.
- **UWP** — both interfaces work as-is (the CS203XL UWP app already uses TCP/IP, and the
  BLE path works once `Microsoft.Toolkit.Uwp.Connectivity` 7.1.3 is referenced).

## Open decisions

1. **Namespace and type name** — `RFIDReaderFinder` in the `CSLibrary` root would sit
   next to `DeviceFinder` and `NetFinder.NetFinder`, which are easy to confuse.
   Recommended: namespace `CSLibrary.Discovery`, type name `RFIDReaderFinder`.
2. **Enum granularity** — one value per interface (`BLUETOOTH`, `TCIP`, `SERIAL`, `USB`)
   with `SupportedModels` on the provider (recommended), versus one value per interface
   *and* reader family (`TCIP_CS203XL`, `TCIP_CS463`, …).
3. **Model for TCP/IP results** — extend the discovery protocol to carry the model, or
   have the provider assign it per family (current plan), or report `MODEL.UNKNOWN`.
4. **Connect dispatch** — option A or B above.
5. **Threading contract** — marshal inside the façade or document background events.
6. **Serial / USB** — ship stubs with `IsAvailable == false`, or defer until hardware is
   available.
7. **Deprecation** — whether and when to mark `DeviceFinder`, `NetFinder.NetFinder` and
   `TCPDeviceFinder` as `[Obsolete]`.

## Work breakdown

| Item | Size | Notes |
|---|---|---|
| Core: `RFIDReaderInfo`, `IReaderDiscovery`, `RFIDReaderFinder`, registration, lifecycle | medium (1–2 days) | `Source/Discovery/` |
| BLE provider (wrap `DeviceFinder`, map to `RFIDReaderInfo`) | small (0.5 day) | `Source/HAL/Plugin.BLE/` |
| TCP/IP provider (wrap `NetFinder`, map to `RFIDReaderInfo`) | small (0.5 day) | `Source/HAL/TCPIP/` |
| Connect dispatch `ConnectAsync(RFIDReaderInfo)` (option B) | small (0.5 day) | `Source/CSLibrary.cs` |
| Android multicast lock for UDP broadcast | small | TCP/IP provider or Android HAL |
| iOS `NSLocalNetworkUsageDescription` + multicast entitlement | small | app projects |
| App migration: CS710S app and CS203XL app to the unified finder | medium (0.5–1 day each) | `UITest/` |
| Serial / USB providers | large | needs hardware |

## Acceptance criteria

- One search in the app lists BLE and TCP/IP readers in the same list, each entry showing
  name, model and interface.
- Selecting a BLE entry connects over BLE; selecting a TCP/IP entry connects over
  TCP/IP — no app side transport switch.
- A reader found on one interface is never offered for an interface that cannot serve its
  model (`SupportedModels`).
- The CS710S app and the CS203XL app build and run unchanged in behaviour, both against
  the unified finder.
- Existing public finders keep working for apps that have not migrated.
