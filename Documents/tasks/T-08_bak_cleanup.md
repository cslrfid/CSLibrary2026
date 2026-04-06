# T-08 — Remove All .bak Files

**Status:** Pending
**Phase:** 1 — Quality Gates
**Priority:** 🔴 High
**Estimated:** 30 minutes
**Constitution:** §3.4 — "No `.bak` files may be committed to the repository"
**Spec reference:** TD-6 (`.bak` files present in source tree)

---

## Objective

Find and delete all `.bak` files from the repository. These are legacy artifacts from the migration from the old CSLibrary repo.

---

## Step-by-Step Commands

```bash
cd CSLibrary2026

# 1. Find all .bak files (preview before deleting)
find . -name "*.bak" -type f -print

# Expected output: list of .bak files (e.g. ClassRFID.Public.Power.cs.bak, etc.)

# 2. Delete all .bak files
find . -name "*.bak" -type f -delete

# 3. Verify none remain
find . -name "*.bak" -type f -print
# Expected: no output

# 4. Commit
git add -A
git commit -m "chore(cleanup): remove all .bak files from source tree

Constitutes: constitution §3.4 (no .bak files in repository), plan T-08"
```

---

## Expected .bak Files

Based on the PROJECT_REPORT.md observation:
```
ClassRFID.Public.Power.cs.bak
ClassRFID.Public.Operation.cs.bak
(possibly others — find output determines the full list)
```

---

## Verification

After commit:
```bash
git ls-files | grep '\.bak$'
# Expected: no output
```

---

## Definition of Done

- [ ] `find . -name "*.bak" -type f` returns empty
- [ ] `git ls-files | grep '\.bak$'` returns empty
- [ ] Single commit with conventional commit message `chore(cleanup): remove all .bak files`
