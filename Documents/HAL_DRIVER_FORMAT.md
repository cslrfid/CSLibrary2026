# HAL Communication Driver Format

This document defines the target plug-in driver format for communication protocols under `Source/HAL/`, so multiple protocols can be plugged in (or a single one selected) without scattering protocol-specific logic through the core library.

**Status:** guidance/target format based on the current implementation flow (`ITransport`, HAL partials, Bluetooth protocol engine). Existing HAL folders may not yet fully conform; migrate incrementally.

## Current flow (as implemented)

- **Core transport contract:** `Source/Transport/ITransport.cs`
  - `bool IsConnected { get; }`
  - `string ConnectionInfo { get; }` (e.g. BLE MAC, `ip:port`)
  - `Task<bool> ConnectAsync(object[] args)` (transport-specific args)
  - `Task<int> SendAsync(byte[] data)` (raw bytes to link)
  - `void Disconnect()`
  - `void SetReceiveCallback(Action<byte[]> callback)` (raw bytes from link)
- **Protocol engine (shared):** `Source/BluetoothProtocol/BTSend.cs`, `BTReceive.cs`, and `Source/CSLibrary.cs`
  - Outbound: queued CSL API packets are sent via `_transport.SendAsync(...)`.
  - Inbound: link bytes are fed into `CharacteristicOnValueUpdated(byte[])` → packet framing/CRC/assembly → `ProcessAPIPacket(...)` → reader subsystems and response/state handling.
  - `HighLevelInterface` owns reader state, `HardwareInit()`, command/response sequencing, and dispatch to RFID/barcode/notification/IC handlers.
- **HAL integration today:** protocol folders in `Source/HAL/` (e.g. `Plugin.BLE/`, `TCPIP/`) provide `HighLevelInterface` partials (`CodeFile*.cs`) that expose `ConnectAsync(...)` overloads, create/configure a transport, assign `_transport`, set `_sp._ConnectionMode`, wire receive to `CharacteristicOnValueUpdated`, start timers, and perform protocol-specific disconnect; they may also include discovery helpers (`DeviceFinder`/`NetFinder`) and debug utilities. Concrete transports live in `Source/Transport/` (e.g. `BLETransport.cs`, `TCPTransport.cs`).
- **Single owner for link state:** the transport owns the GATT characteristics / socket handles. A HAL partial must **not** keep a second copy of them — the leftovers from before the transport refactor were never assigned and caused null-reference failures at runtime (e.g. a HAL level `BLE_Send` writing through a null characteristic).
- **The protocol engine never calls a HAL-specific send method:** `BTSend.cs` sends through `_transport.SendAsync(...)`, so the core stays free of any BLE/TCP specific dependency.
- **Connection loss signalling:** the transport signals "link lost" by invoking the receive callback with a `null` payload; the HAL must translate that into the reader-state event instead of forwarding `null` into the packet parser.
- **Build selection:** `CSLibrary2026.csproj` explicitly includes/excludes HAL folders and may define symbols (e.g. `TCP`); compiled-in drivers are currently controlled by project item groups/defines.

## Design goals

- **One subfolder = one communication protocol/driver** under `Source/HAL/<DriverName>/`.
- **Plug-in capable:** support registering/compiling multiple drivers and selecting one (or more, if applicable) at runtime by interface/connection request; allow builds that include only a subset.
- **Uniform boundary:** drivers provide link I/O through a common transport contract; core protocol parsing/state machine remains shared unless a protocol truly requires a different framing layer.
- **Explicit project wiring:** any driver source required for a build must be referenced in `CSLibrary2026.csproj` (per project conventions).

## Target driver contract

Define a common driver interface in shared source (e.g. `Source/HAL/` or `Source/Transport/`), for example:

```csharp
public interface ICommDriver
{
    // Identity / capability
    string Id { get; }                         // stable id, e.g. "ble.pluginble", "tcpip"
    string Name { get; }                       // display name
    RFIDDEVICE.CONNECTIONMODE[] SupportedModes { get; } // or equivalent capability flags
    RFIDDEVICE.MODEL[] SupportedModels { get; }         // if model-specific

    // Lifecycle / transport
    ITransport CreateTransport();              // or expose an ITransport instance
    Task<bool> ConnectAsync(HighLevelInterface host, object context, object[] args);
    void Disconnect(HighLevelInterface host);

    // Optional capabilities (separate interfaces preferred)
    // IDeviceDiscovery (scan/find), IReconnectable, IMtuConfigurable, etc.
}
```

Notes:
- **`ITransport` remains the data-plane boundary** (connect/send/disconnect/receive callback/connection info). Drivers may implement `ITransport` directly or compose an internal transport class.
- **`HighLevelInterface` (`host`) integration points** a driver must satisfy when connecting:
  - Validate reader state/args and supported `MODEL`/mode.
  - Establish the link and obtain `ConnectionInfo` (e.g. MAC, endpoint).
  - Set `host._transport` (or equivalent), connection mode, device type, and reader state as required.
  - Wire inbound data: `transport.SetReceiveCallback(bytes => host.CharacteristicOnValueUpdated(bytes))` (or the current receive entry point) so shared framing/dispatch runs.
  - Start required timers/engine and invoke common init (`HardwareInit()` / equivalent) consistent with existing behavior.
  - On disconnect or connection loss, stop protocol-specific resources, then run common cleanup (`DisconnectCommon()` / equivalent) and raise the appropriate reader state events.
- **Discovery is optional:** not all protocols have discovery (e.g. TCP may use `NetFinder`). Model discovery as an optional capability/interface rather than forcing it on every driver. A unified search façade across interfaces is proposed in `Documents/READER_DISCOVERY.md`.
- **No direct dependency from core logic to a specific HAL** except through the driver/transport abstractions and explicit registration/selection.

## Driver subfolder layout (per protocol)

Each `Source/HAL/<DriverName>/` subfolder should contain, as applicable:

- **Driver entry:** implementation of the common driver contract (registration metadata, connect/disconnect orchestration).
- **Transport/link implementation:** protocol-specific `ITransport` (or equivalent) handling sockets/GATT/serial/USB I/O and exposing raw byte read/write.
- **Discovery (optional):** device/network finder types.
- **Protocol-specific helpers:** debug, constants, platform interop—kept inside the driver folder when not shared.
- **Partial `HighLevelInterface` glue (transitional):** existing `ConnectAsync` overloads/wrappers may remain for API compatibility but should delegate to the driver.

Example (conceptual):
- `Source/HAL/Plugin.BLE/` — BLE (Plugin.BLE) driver: GATT service/characteristic setup, notify/read/write, model-based service UUID/MTU handling.
- `Source/HAL/TCPIP/` — TCP/IP driver: socket stream connect/send/receive, endpoint info, reconnect hooks if supported.
- Future: `Source/HAL/Serial/`, `Source/HAL/USB/`, etc., each encapsulating its link details.

## Registration and selection

- **Registry:** maintain a driver registry/factory (e.g. static registry or DI-style provider) that can enumerate compiled-in drivers, query capabilities, and resolve a driver by `Id`, `CONNECTIONMODE`, or connection request type.
- **Selection:** `HighLevelInterface` (or a connection facade) chooses a driver based on the `ConnectAsync` overload/parameters and device model; only the selected driver’s transport becomes active (`_transport`).
- **Multiple vs single:** multiple drivers may be compiled/registered simultaneously; a session uses one active transport at a time. Builds targeting constrained environments may include only one driver subfolder.
- **Compatibility:** preserve existing public connection APIs where possible by routing them to driver selection internally.

## Implementation checklist for a new driver

1. Add `Source/HAL/<DriverName>/` with driver + transport (and optional discovery) types.
2. Implement/compose `ITransport` and map connect args, `ConnectionInfo`, send/receive, and disconnect/loss semantics.
3. Integrate with host receive path and common init/cleanup/state events as above.
4. Register the driver and expose a compatible `ConnectAsync`/API entry point or route existing ones.
5. Update `CSLibrary2026.csproj` to include the new sources (and any required packages/defines/conditions per TFM/platform); exclude from builds where unsupported.
6. Validate against relevant readers/interfaces and reference implementations in `S:\Projects\GitHub\CSLibrary` when porting Serial/USB or other protocols.

## Non-goals / cautions

- Do not move CSL command framing, RFID/barcode/notification dispatch, or reader business logic into HAL drivers; drivers are responsible for the communication link.
- Do not assume all HAL subfolders currently in the repo are active—some are excluded from compilation in the project file and may serve as legacy references.
- Keep changes consistent with the project’s multi-target and explicit-include build model.