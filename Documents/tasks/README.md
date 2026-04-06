# CSLibrary2026 — Task Board

> Each file here is a self-contained, PR-ready unit of work.
> Completing a file = one or more PRs merged to `develop`.
> **Governed by:** [`Documents/constitution.md`](../constitution.md) · [`Documents/plan.md`](../plan.md)

---

## Task Index

| ID | File | Phase | Priority | Estimated |
|---|---|---|---|---|
| T-01 | [`T-01_ci_workflow.md`](T-01_ci_workflow.md) | 0 | 🔴 High | 1h |
| T-02 | [`T-02_test_project.md`](T-02_test_project.md) | 0 | 🔴 High | 2h |
| T-03 | [`T-03_transport_contract_tests.md`](T-03_transport_contract_tests.md) | 0 | 🔴 High | 2h |
| T-04 | [`T-04_btsend_tests.md`](T-04_btsend_tests.md) | 0 | 🔴 High | 2h |
| T-05 | [`T-05_devicefinder_tests.md`](T-05_devicefinder_tests.md) | 0 | 🔴 High | 2h |
| T-06 | [`T-06_unifiedapi_tests.md`](T-06_unifiedapi_tests.md) | 0 | 🔴 High | 2h |
| T-08 | [`T-08_bak_cleanup.md`](T-08_bak_cleanup.md) | 1 | 🔴 High | 30m |
| T-09 | [`T-09_warnings_fix.md`](T-09_warnings_fix.md) | 1 | 🔴 High | 2d |
| T-10 | [`T-10_nullable_enable.md`](T-10_nullable_enable.md) | 1 | 🟡 Medium | 1d |
| T-11 | [`T-11_changelog.md`](T-11_changelog.md) | 1 | 🔴 High | 1h |
| T-12 | [`T-12_changelog_ci_gate.md`](T-12_changelog_ci_gate.md) | 1 | 🔴 High | 1h |
| T-14 | [`T-14_xml_documentation.md`](T-14_xml_documentation.md) | 2 | 🟡 Medium | 2d |
| T-15 | [`T-15_readme.md`](T-15_readme.md) | 2 | 🟡 Medium | 2h |
| T-16 | [`T-16_api_reference.md`](T-16_api_reference.md) | 2 | 🟡 Medium | 1d |
| T-17a | [`T-17a_classrename_classbarcode.md`](T-17a_classrename_classbarcode.md) | 3 | 🟢 Low | 2h |
| T-18 | [`T-18_winforms_ble_hal.md`](T-18_winforms_ble_hal.md) | 4 | 🟡 Medium | 2w |
| T-20 | [`T-20_new_reader_model_checklist.md`](T-20_new_reader_model_checklist.md) | 5 | 🟡 Medium | per model |
| T-21 | [`T-21_nuget_publish.md`](T-21_nuget_publish.md) | All | 🔴 High | 1h |

---

## Phase 0 — Foundation (CI + Tests)

```
T-01  Create GitHub Actions CI workflow
T-02  Create CSLibrary2026.Tests/ xUnit project
T-03  Write ITransport contract tests
T-04  Write BTSend framing tests
T-05  Write DeviceFinder model detection tests
T-06  Write UnifiedAPI MODEL dispatch tests
```

## Phase 1 — Quality Gates

```
T-08  Remove all .bak files
T-09  Fix compiler warnings (472 → <20)
T-10  Enable nullable reference types on netstandard2.0
T-11  Create CHANGELOG.md
T-12  Add CHANGELOG regex gate to CI
```

## Phase 2 — Documentation

```
T-14  Complete XML documentation on all public types
T-15  Expand README.md
T-16  Create Documents/API.md
```

## Phase 3 — Class Naming (Future / Long-Horizon)

```
T-17a Rename ClassBarCode → BarcodeScanner (deprecate + alias)
```

## Phase 4 — WinForms BLE HAL (Future)

```
T-18  Implement WinFormsBLE WinRTTransport + README + .csproj activation
```

## Phase 5 — Per-Model Checklist (Triggered by New Hardware)

```
T-20  New reader model checklist (run per new CSL reader)
```

## Cross-Cutting

```
T-21  Publish NuGet beta.2 to nuget.org
```

---

## Tracking Progress

Mark complete by:
- [ ] Checking the task file's **Status** header
- [ ] Confirming the PR is merged to `develop`
- [ ] Moving the task file to `Documents/tasks/done/` (optional)

---

*Last updated: 2026-04-05*
