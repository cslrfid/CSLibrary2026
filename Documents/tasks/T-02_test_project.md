# T-02 — Create CSLibrary2026.Tests/ xUnit Project

**Status:** Pending
**Phase:** 0 — Foundation
**Priority:** 🔴 High
**Estimated:** 2 hours
**Constitution:** §6 (Testing Strategy), §6.2 (Test Conventions)
**Spec reference:** Success Metrics (unit test coverage: 0% → ≥80%)
**Prerequisite:** T-01 (CI workflow must be in place or added in same PR)

---

## Objective

Create `CSLibrary2026/Tests/CSLibrary2026.Tests/` as an xUnit test project targeting `net10.0`. This project does not need to target `netstandard2.0` — only `net10.0`.

---

## Deliverable

```
CSLibrary2026/Tests/
└── CSLibrary2026.Tests/
    ├── CSLibrary2026.Tests.csproj
    ├── TestMocks/
    │   ├── MockAdapter.cs
    │   ├── MockDevice.cs
    │   ├── MockService.cs
    │   └── MockCharacteristic.cs
    └── Transport/
        └── ITransportContractTests.cs   ← bare minimum to validate the project builds
```

### CSLibrary2026.Tests.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.0" />
    <PackageReference Include="Moq" Version="4.20.70" />
    <PackageReference Include="xunit" Version="2.8.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>
    <PackageReference Include="coverlet.collector" Version="6.0.2">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\CSLibrary2026.csproj" />
  </ItemGroup>

</Project>
```

### TestMocks/MockDevice.cs (example)

```csharp
using Plugin.BLE.Abstractions.Contracts;

namespace CSLibrary2026.Tests.TestMocks;

public class MockDevice : IDevice
{
    public Guid Id => Guid.NewGuid();
    public string Name => "MockCS108";
    public DeviceState State => DeviceState.Connected;
    public int Rssi => -50;
    public object NativeDevice => new { };
    public IReadOnlyList<IService> Services => throw new NotImplementedException();

    public Task<IService?> GetServiceAsync(Guid id) =>
        Task.FromResult<IService?>(new MockService(id));

    public Task<IReadOnlyList<IService>> GetServicesAsync() =>
        Task.FromResult<IReadOnlyList<IService>>(Array.Empty<IService>());
}
```

### Transport/ITransportContractTests.cs

```csharp
using Xunit;

namespace CSLibrary2026.Tests.Transport;

/// <summary>
/// Validates that BLETransport and TCPTransport both satisfy ITransport.
/// </summary>
public class ITransportContractTests
{
    [Fact]
    public void BLETransport_Implements_ITransport()
    {
        var transport = new BLETransport();
        Assert.IsAssignableFrom<ITransport>(transport);
    }

    [Fact]
    public void TCPTransport_Implements_ITransport()
    {
        var transport = new TCPTransport();
        Assert.IsAssignableFrom<ITransport>(transport);
    }

    [Fact]
    public void ITransport_HasRequiredMembers()
    {
        var type = typeof(ITransport);
        Assert.NotNull(type.GetProperty(nameof(ITransport.IsConnected)));
        Assert.NotNull(type.GetProperty(nameof(ITransport.ConnectionInfo)));
        Assert.NotNull(type.GetMethod(nameof(ITransport.ConnectAsync)));
        Assert.NotNull(type.GetMethod(nameof(ITransport.SendAsync)));
        Assert.NotNull(type.GetMethod(nameof(ITransport.Disconnect)));
        Assert.NotNull(type.GetMethod(nameof(ITransport.SetReceiveCallback)));
    }
}
```

---

## Step-by-Step Commands

```bash
cd CSLibrary2026

# 1. Create the test project structure
mkdir -p Tests/CSLibrary2026.Tests/TestMocks
mkdir -p Tests/CSLibrary2026.Tests/Transport
mkdir -p Tests/CSLibrary2026.Tests/DeviceFinder
mkdir -p Tests/CSLibrary2026.Tests/BluetoothProtocol
mkdir -p Tests/CSLibrary2026.Tests/ModelDispatch

# 2. Create the .csproj
# (use write tool to create Tests/CSLibrary2026.Tests/CSLibrary2026.Tests.csproj)

# 3. Create mock files
# (use write tool for each TestMocks/*.cs file)

# 4. Create the ITransport contract test
# (use write tool for Transport/ITransportContractTests.cs)

# 5. Verify the project builds
export PATH="/home/node/.dotnet:$PATH"
dotnet build Tests/CSLibrary2026.Tests/CSLibrary2026.Tests.csproj -c Release

# 6. Verify tests run
dotnet test Tests/CSLibrary2026.Tests/CSLibrary2026.Tests.csproj -c Release

# 7. Commit
git add Tests/ .gitignore
git commit -m "test: create CSLibrary2026.Tests xUnit project with ITransport contract tests

Co-authored-by: GithubProject-CSLibrary2026"
```

---

## CI Integration Note

Once T-01 (CI workflow) exists, this project's tests will run in the `test` job of `.github/workflows/ci.yml`. The project path used in the CI YAML is `CSLibrary2026/Tests/CSLibrary2026.Tests/CSLibrary2026.Tests.csproj`.

---

## Definition of Done

- [ ] `Tests/CSLibrary2026.Tests/CSLibrary2026.Tests.csproj` exists and targets `net10.0`
- [ ] Project builds with `dotnet build -c Release`
- [ ] `dotnet test -c Release` runs and reports at least 3 passing tests
- [ ] `ITransportContractTests` passes (validates BLETransport and TCPTransport both implement ITransport)
- [ ] Mock classes exist for IDevice, IAdapter, IService, ICharacteristic
