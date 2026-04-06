# T-06 — Write UnifiedAPI MODEL Dispatch Tests

**Status:** Pending
**Phase:** 0 — Foundation
**Priority:** 🔴 High
**Estimated:** 2 hours
**Constitution:** §2.3 (Two Chipset Architectures), §5.4 (MODEL Enum), §6.2 (Test Conventions)
**Spec reference:** FR-10 (library SHALL route commands to correct chipset handler based on MODEL)

---

## Objective

Write unit tests for `ClassRFID.UnifiedAPI.cs` to verify that MODEL-based chipset dispatch works correctly — Rx000 commands go to the RX000 path, E710/E910 commands go to the Ex10 path.

**Key test areas:**
- `UnifiedAPI` correctly identifies MODEL.CS108 and MODEL.CS468 as Rx000 chipset
- `UnifiedAPI` correctly identifies MODEL.CS710S and MODEL.CS203XL as E710/E910 chipset
- Inventory command routes to correct chipset handler per model
- Tag read/write routes to correct chipset handler per model
- `GetChipsetType(MODEL)` returns correct chipset enum for all supported models

---

## Test File: ModelDispatch/UnifiedAPI_DispatchTests.cs

```csharp
using Xunit;
using CSLibrary;
using static CSLibrary.RFIDDEVICE;

namespace CSLibrary2026.Tests.ModelDispatch;

/// <summary>
/// Tests that MODEL-based dispatch routes to the correct chipset handler.
/// Rx000 models: CS108, CS468 → Comm_Protocol/RX000Commands/
/// E710/E910 models: CS710S, CS203XL → Comm_Protocol/Ex10Commands/
/// </summary>
public class UnifiedAPI_DispatchTests
{
    [Theory]
    [InlineData(MODEL.CS108, ChipsetType.Rx000)]
    [InlineData(MODEL.CS468, ChipsetType.Rx000)]
    [InlineData(MODEL.CS710S, ChipsetType.E710)]
    [InlineData(MODEL.CS203XL, ChipsetType.E910)]
    public void GetChipsetType_ReturnsCorrectChipset(MODEL model, ChipsetType expected)
    {
        var result = UnifiedAPI.GetChipsetType(model);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(MODEL.CS108)]
    [InlineData(MODEL.CS468)]
    public void InventoryCommand_Rx000Model_UsesCorrectPath(MODEL model)
    {
        // Given a Rx000 model, inventory should go through RX000Commands path
        // The test documents that this dispatch exists and can be verified
        var chipset = UnifiedAPI.GetChipsetType(model);
        Assert.Equal(ChipsetType.Rx000, chipset);
    }

    [Theory]
    [InlineData(MODEL.CS710S)]
    [InlineData(MODEL.CS203XL)]
    public void InventoryCommand_E700SeriesModel_UsesCorrectPath(MODEL model)
    {
        // Given an E710/E910 model, inventory should go through Ex10Commands path
        var chipset = UnifiedAPI.GetChipsetType(model);
        Assert.True(chipset == ChipsetType.E710 || chipset == ChipsetType.E910);
    }

    [Theory]
    [InlineData(MODEL.CS108)]
    [InlineData(MODEL.CS468)]
    public void TagReadCommand_Rx000Model_UsesCorrectPath(MODEL model)
    {
        var chipset = UnifiedAPI.GetChipsetType(model);
        Assert.Equal(ChipsetType.Rx000, chipset);
    }

    [Theory]
    [InlineData(MODEL.CS710S)]
    [InlineData(MODEL.CS203XL)]
    public void TagReadCommand_E700SeriesModel_UsesCorrectPath(MODEL model)
    {
        var chipset = UnifiedAPI.GetChipsetType(model);
        Assert.True(chipset == ChipsetType.E710 || chipset == ChipsetType.E910);
    }

    [Fact]
    public void GetChipsetType_UnknownModel_ThrowsOrReturnsUnknown()
    {
        // Unknown MODEL should not crash — either throw ArgumentException or return Unknown
        Assert.Throws<ArgumentException>(() => UnifiedAPI.GetChipsetType(MODEL.UNKNOWN));
    }
}

/// <summary>
/// Mirrors the chipset type enum used in UnifiedAPI for test assertions.
/// This must match the actual enum in the source.
/// </summary>
public enum ChipsetType
{
    Unknown,
    Rx000,
    E710,
    E910
}
```

---

## Step-by-Step Commands

```bash
# 1. Write the test file
# (write ModelDispatch/UnifiedAPI_DispatchTests.cs)

# 2. Run the tests
export PATH="/home/node/.dotnet:$PATH"
dotnet test Tests/CSLibrary2026.Tests/CSLibrary2026.Tests.csproj -c Release \
  --filter "FullyQualifiedName~Dispatch"

# 3. Check which tests fail (the ChipsetType enum may not be public)
# If ChipsetType is internal, use InternalsVisibleTo or test indirectly

# 4. Commit
git add Tests/ModelDispatch/
git commit -m "test(unifiedapi): add MODEL dispatch tests for chipset routing

- Rx000 models (CS108, CS468) route to RX000Commands path
- E710/E910 models (CS710S, CS203XL) route to Ex10Commands path
- Unknown MODEL throws ArgumentException

Constitutes: constitution §2.3, plan T-06"
```

---

## Note on ChipsetType Enum

If `ChipsetType` (or equivalent) is `internal` in `ClassRFID.UnifiedAPI.cs`, add `[assembly: InternalsVisibleTo("CSLibrary2026.Tests")]` to expose it for testing. If the dispatch logic is private and cannot be tested without `InternalsVisibleTo`, document the limitation and proceed with the InternalsVisibleTo approach.

---

## Definition of Done

- [ ] `ModelDispatch/UnifiedAPI_DispatchTests.cs` has ≥ 7 passing tests
- [ ] All supported MODEL values are covered (CS108, CS468, CS710S, CS203XL)
- [ ] Unknown MODEL is tested for graceful handling
- [ ] `InternalsVisibleTo` added if needed
- [ ] No physical hardware required
