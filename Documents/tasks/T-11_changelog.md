# T-11 — Create and Maintain CHANGELOG.md

**Status:** Pending
**Phase:** 1 — Quality Gates
**Priority:** 🔴 High
**Estimated:** 1 hour
**Constitution:** §10.2 (Required Documents), §12 TD-5
**Spec reference:** Success Metrics (CHANGELOG: not maintained → updated per release)

---

## Objective

Create `CSLibrary2026/CHANGELOG.md` in Keep a Changelog format, covering all releases including a populated `[Unreleased]` section.

---

## Deliverable: CHANGELOG.md

```markdown
# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
### Changed
### Fixed
### Removed
### Security

---

## [1.0.0-beta.2] — YYYY-MM-DD

*(Populated after beta.2 is ready to publish — leave placeholder until then)*

### Added
- `Documents/constitution.md` — immutable project principles, architecture rules, and technical debt register
- `Documents/specs/spec.md` — project specification (goals, requirements, acceptance criteria, domain model)
- `Documents/plan.md` — technical implementation roadmap with phases, tasks, and risk register
- `Documents/tasks/` — PR-ready individual task files for each deliverable
- `Source/Transport/ITransport.cs` — transport abstraction interface separating BLE and TCP from HighLevelInterface
- `Source/Transport/BLETransport.cs` — Plugin.BLE GATT transport implementing ITransport
- `Source/Transport/TCPTransport.cs` — TCP/IP socket transport implementing ITransport
- `Source/HAL/Plugin.BLE/DeviceFinder.cs` — extracted BLE device discovery logic (previously inline in CodeFileBLE)

### Changed
- Refactored `HighLevelInterface` to hold `ITransport _transport` set by whichever ConnectAsync overload is called
- `BTSend.cs` now calls `_transport.SendAsync()` generically regardless of BLE or TCP
- `MODEL.CS468` added to BLE service UUID switch in `BLETransport` (was falling through = couldn't connect)

### Fixed
- `.csproj` Readme file mismatch: `Readme.md` → `README.md`

### Security
- N/A

---

## [1.0.0-beta.1] — 2026-04-01

### Added
- Initial NuGet package publication
- Support for CS108, CS468, CS710S, CS203XL readers
- Plugin.BLE 3.0.0 BLE support
- TCP/IP support for CS203XL
- Multi-target: `netstandard2.0` and `net10.0`
- Barcode scanning via integrated scanner on supported readers
- Battery status monitoring
- Frequency band and country regulation management
- Event-driven async callback architecture (`OnAsyncCallback`, `OnAccessCompleted`, `OnStateChanged`)
```

---

## Step-by-Step Commands

```bash
cd CSLibrary2026

# 1. Write the CHANGELOG.md
# (use write tool to create CSLibrary2026/CHANGELOG.md)

# 2. Commit
git add CHANGELOG.md
git commit -m "docs: create CHANGELOG.md

Format: Keep a Changelog 1.0.0
Sections: Added, Changed, Fixed, Removed, Security
Populated [Unreleased] and [1.0.0-beta.2] placeholder
Added entries for ITransport refactor, DeviceFinder extraction, MODEL.CS468 fix

Constitutes: constitution §10.2, plan T-11"
```

---

## Ongoing Rule (Per Constitution §10.2)

> Every PR merged to `develop` that has a user-visible change MUST add an entry to the `[Unreleased]` section of CHANGELOG.md before merge.

Enforcement: T-12 adds a CI regex gate for this.

---

## Definition of Done

- [ ] `CHANGELOG.md` exists at `CSLibrary2026/CHANGELOG.md`
- [ ] Format matches Keep a Changelog 1.0.0
- [ ] Sections: Added, Changed, Fixed, Removed, Security in `[Unreleased]`
- [ ] Beta.1 entry exists under its own version header
- [ ] Beta.2 placeholder exists with `YYYY-MM-DD` (to be updated on publish)
