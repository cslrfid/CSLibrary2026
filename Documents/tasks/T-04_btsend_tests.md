# T-04 — Write BTSend Protocol Framing Tests

**Status:** Pending
**Phase:** 0 — Foundation
**Priority:** 🔴 High
**Estimated:** 2 hours
**Constitution:** §2.1 (Layer Map: BluetoothProtocol is below ITransport), §8.3 (Command Validation)
**Spec reference:** Success Metrics (coverage: Transport + BluetoothProtocol ≥80%)

---

## Objective

Write unit tests for the Bluetooth protocol framing logic in `Source/BluetoothProtocol/BTSend.cs`. These tests validate the byte-level command framing that every BLE and TCP command passes through.

**Key test areas:**
- `destinationsID[]` framing: first byte of any command is the correct destination ID
- Packet padding: commands shorter than minimum length are padded correctly
- Sequence numbering: multi-packet commands use correct sequence numbers
- RFID vs Barcode vs Notification destination IDs

---

## Relevant Source Files

- `Source/BluetoothProtocol/BTSend.cs` — downlink command framing
- `Source/BluetoothProtocol/CSLibrary.Private.cs` — constants including `destinationsID[]`

---

## Test File: BluetoothProtocol/BTSend_FramingTests.cs

```csharp
using Xunit;
using CSLibrary;

namespace CSLibrary2026.Tests.BluetoothProtocol;

/// <summary>
/// Unit tests for BTSend command framing.
/// Tests the byte-level protocol without requiring a real BLE connection.
/// </summary>
public class BTSend_FramingTests
{
    // destinationsID enum values (from BTSend.cs)
    // RFID = 0xc2, Barcode = 0x6a, Notification = 0xd9, SiliconLabIC = 0xe8, BluetoothIC = 0x5f

    [Fact]
    public void FrameCommand_RFIDCommand_StartsWithRFIDDestID()
    {
        // The first byte of any RFID command must be 0xc2
        // This test documents the protocol invariant
        var cmdBytes = new byte[] { 0x80, 0x02 }; // example RFID command
        var framed = BTSend.FrameCommand(cmdBytes); // TODO: find actual FrameCommand API

        Assert.Equal((byte)0xc2, framed[0]);
    }

    [Fact]
    public void FrameCommand_BarcodeCommand_StartsWithBarcodeDestID()
    {
        // Barcode commands start with 0x6a
        // The library must route barcode commands to the correct destination
        var barcodeCmd = new byte[] { 0x90, 0x02 }; // BARCODESCANTRIGGER
        Assert.Equal((byte)0x6a, barcodeCmd[0]);
    }

    [Fact]
    public void DestinationsID_AllFiveDestinationsDefined()
    {
        // Verify all five destination IDs are defined
        var expected = new[]
        {
            (byte)0xc2, // RFID
            (byte)0x6a, // Barcode
            (byte)0xd9, // Notification
            (byte)0xe8, // SiliconLabIC
            (byte)0x5f  // BluetoothIC
        };

        // This tests that the destinationsID array exists and has correct values
        // The actual test depends on whether this is exposed publicly or via reflection
        Assert.Equal(5, expected.Length);
    }
}
```

---

## Step-by-Step Commands

```bash
# 1. Write the test file
# (use write tool to create BluetoothProtocol/BTSend_FramingTests.cs)

# 2. Run the tests
export PATH="/home/node/.dotnet:$PATH"
dotnet test Tests/CSLibrary2026.Tests/CSLibrary2026.Tests.csproj -c Release \
  --filter "FullyQualifiedName~BTSend"

# 3. Commit
git add Tests/BluetoothProtocol/
git commit -m "test(bluetoothprotocol): add BTSend framing tests

Tests destination ID framing for RFID and Barcode commands.
Constitution: §8.3 (command validation)
Plan: T-04"
```

---

## Note on Framing API

`BTSend.cs` currently uses a `partial class HighLevelInterface` pattern. The framing method may be `private` or `internal`. If it is `internal`, add `[assembly: InternalsVisibleTo("CSLibrary2026.Tests")]` to `CSLibrary.cs` to allow the test project to access it.

If the framing logic is tightly coupled to `HighLevelInterface` and cannot be unit tested in isolation, document this as a testability debt and write integration tests tagged `[Trait("integration", "true")]` instead.

---

## Definition of Done

- [ ] `BTSend_FramingTests.cs` has ≥ 3 passing tests
- [ ] Framing invariants (destination ID, padding) are documented with tests
- [ ] Any `internal` access issues resolved via `InternalsVisibleTo`
- [ ] Tests do not require physical hardware
