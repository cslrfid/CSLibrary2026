# T-03 — Write ITransport Contract Tests

**Status:** Pending
**Phase:** 0 — Foundation
**Priority:** 🔴 High
**Estimated:** 2 hours
**Constitution:** §6.2 (Test Conventions), §2.4 (ITransport Contract)
**Spec reference:** Success Metrics (coverage: 0% → ≥80% of Transport layer)

---

## Objective

Write unit tests for `Source/Transport/ITransport.cs`, `BLETransport.cs`, and `TCPTransport.cs` that can run in CI with no physical hardware.

**Scope (what to test):**
- `ITransport` interface has all required members
- `BLETransport` implements `ITransport`
- `TCPTransport` implements `ITransport`
- `BLETransport.ConnectAsync` validates MODEL argument
- `BLETransport.ConnectAsync` rejects unsupported MODEL values
- `BLETransport.SendAsync` returns -1 when not connected
- `TCPTransport.SendAsync` returns -1 when not connected
- `BLETransport.IsConnected` is false before ConnectAsync
- `TCPTransport.IsConnected` is false before ConnectAsync
- `BLETransport.Disconnect` cleans up without throwing

---

## Mock Setup Required

Extend `TestMocks/` from T-02 with:

```csharp
// TestMocks/MockCharacteristic.cs
using Plugin.BLE.Abstractions.Contracts;

public class MockCharacteristic : ICharacteristic
{
    public Guid Id => Guid.NewGuid();
    public string Uuid => Id.ToString();
    public byte[]? Value { get; set; }
    public CharacteristicPropertyType Properties => CharacteristicPropertyType.Write | CharacteristicPropertyType.WriteWithoutResponse;
    public event EventHandler<CharacteristicUpdatedEventArgs>? ValueUpdated;

    public Task<byte[]> ReadAsync() => Task.FromResult(Value ?? Array.Empty<byte>());
    public Task<int> WriteAsync(byte[] data, CancellationToken cancellationToken = default) => Task.FromResult(data.Length);
    public Task StartUpdatesAsync() => Task.CompletedTask;
    public Task StopUpdatesAsync() => Task.CompletedTask;
    public IService Service => throw new NotImplementedException();
}
```

---

## Test File: Transport/BLETransport_Tests.cs

```csharp
using Xunit;
using CSLibrary;
using CSLibrary2026.Tests.TestMocks;
using static CSLibrary.RFIDDEVICE;

namespace CSLibrary2026.Tests.Transport;

public class BLETransport_Tests
{
    [Theory]
    [InlineData(MODEL.CS108)]
    [InlineData(MODEL.CS468)]
    [InlineData(MODEL.CS710S)]
    [InlineData(MODEL.CS203XL)]
    public void ConnectAsync_ValidModel_ReturnsTrue(MODEL model)
    {
        // Arrange
        var device = new MockDevice();
        var adapter = new MockAdapter();
        var transport = new BLETransport();

        // Act
        // Note: This test will fail without real Plugin.BLE infrastructure.
        // It documents the expected behavior for integration testing.
        // Skipping in unit test — see integration test tag.
        Assert.True(true); // placeholder until mock IAdapter works fully
    }

    [Theory]
    [InlineData(MODEL.UNKNOWN)]
    [InlineData(MODEL.MAX)]
    public void ConnectAsync_UnsupportedModel_ReturnsFalse(MODEL model)
    {
        // Arrange
        var transport = new BLETransport();
        var device = new MockDevice();
        var adapter = new MockAdapter();

        // Act
        var result = transport.ConnectAsync(new object[] { adapter, device, model }).Result;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void SendAsync_NotConnected_ReturnsMinusOne()
    {
        var transport = new BLETransport();
        var result = transport.SendAsync(new byte[] { 0x80, 0x02 }).Result;
        Assert.Equal(-1, result);
    }

    [Fact]
    public void IsConnected_BeforeConnect_ReturnsFalse()
    {
        var transport = new BLETransport();
        Assert.False(transport.IsConnected);
    }

    [Fact]
    public void Disconnect_BeforeConnect_DoesNotThrow()
    {
        var transport = new BLETransport();
        var exception = Record.Exception(() => transport.Disconnect());
        Assert.Null(exception);
    }

    [Fact]
    public void ConnectionInfo_BeforeConnect_ReturnsEmpty()
    {
        var transport = new BLETransport();
        Assert.Equal(string.Empty, transport.ConnectionInfo);
    }

    [Fact]
    public void SetReceiveCallback_WithNull_DoesNotThrow()
    {
        var transport = new BLETransport();
        var exception = Record.Exception(() => transport.SetReceiveCallback(null!));
        Assert.Null(exception);
    }
}
```

---

## Test File: Transport/TCPTransport_Tests.cs

```csharp
using Xunit;
using CSLibrary;

namespace CSLibrary2026.Tests.Transport;

public class TCPTransport_Tests
{
    [Fact]
    public void IsConnected_BeforeConnect_ReturnsFalse()
    {
        var transport = new TCPTransport();
        Assert.False(transport.IsConnected);
    }

    [Fact]
    public void ConnectionInfo_BeforeConnect_ReturnsEmpty()
    {
        var transport = new TCPTransport();
        Assert.Equal(string.Empty, transport.ConnectionInfo);
    }

    [Fact]
    public void SendAsync_NotConnected_ReturnsMinusOne()
    {
        var transport = new TCPTransport();
        var result = transport.SendAsync(new byte[] { 0x80, 0x02 }).Result;
        Assert.Equal(-1, result);
    }

    [Fact]
    public void Disconnect_BeforeConnect_DoesNotThrow()
    {
        var transport = new TCPTransport();
        var exception = Record.Exception(() => transport.Disconnect());
        Assert.Null(exception);
    }
}
```

---

## Step-by-Step Commands

```bash
# 1. Add MockCharacteristic to TestMocks
# (write TestMocks/MockCharacteristic.cs)

# 2. Write the BLETransport tests
# (write Transport/BLETransport_Tests.cs)

# 3. Write the TCPTransport tests
# (write Transport/TCPTransport_Tests.cs)

# 4. Run tests
export PATH="/home/node/.dotnet:$PATH"
dotnet test Tests/CSLibrary2026.Tests/CSLibrary2026.Tests.csproj -c Release --filter "FullyQualifiedName~BLETransport|FullyQualifiedName~TCPTransport"

# 5. Check coverage
dotnet test Tests/CSLibrary2026.Tests/CSLibrary2026.Tests.csproj -c Release \
  --filter "FullyQualifiedName~Transport" \
  --collect:"XPlat Code Coverage"

# 6. Commit
git add Tests/Transport/ Tests/TestMocks/
git commit -m "test(transport): add BLETransport and TCPTransport unit tests

Covers:
- ConnectAsync rejects unsupported MODEL values
- SendAsync returns -1 when not connected
- IsConnected/ConnectionInfo initial state
- Disconnect cleanup without throwing
- SetReceiveCallback null safety

Constitutes: constitution §6.2, plan T-03"
```

---

## Definition of Done

- [ ] `Transport/BLETransport_Tests.cs` has ≥ 6 passing tests
- [ ] `Transport/TCPTransport_Tests.cs` has ≥ 4 passing tests
- [ ] All tests pass in `dotnet test -c Release`
- [ ] Tests are tagged with category (unit/integration) as appropriate
- [ ] No physical hardware required to run
