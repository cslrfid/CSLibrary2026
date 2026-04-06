# T-12 — Add CHANGELOG Regex Gate to CI

**Status:** Pending
**Phase:** 1 — Quality Gates
**Priority:** 🔴 High
**Estimated:** 1 hour
**Constitution:** §10.2 (Required Documents — CHANGELOG maintained per release), §11.3 (PR Requirements)
**Prerequisite:** T-11 (CHANGELOG.md must exist first)

---

## Objective

Add a CI step that enforces the CHANGELOG rule: every PR to `develop` must have an entry in the `[Unreleased]` section.

---

## Step 1 — Add Step to .github/workflows/ci.yml

Add a `changelog` job to the CI workflow:

```yaml
  changelog:
    name: Changelog Updated
    runs-on: ubuntu-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v4
        with:
          fetch-depth: 0  # need full history for diff

      - name: Check CHANGELOG has Unreleased entry
        run: |
          # Ensure CHANGELOG.md exists
          if [ ! -f CHANGELOG.md ]; then
            echo "ERROR: CHANGELOG.md not found"
            exit 1
          fi

          # Check that [Unreleased] section has at least one bullet under Added/Changed/Fixed/Removed/Security
          UNRELEASED_BLOCK=$(sed -n '/## \[Unreleased\]/,/^## /p' CHANGELOG.md | head -n -1)

          if echo "$UNRELEASED_BLOCK" | grep -E "^\s*[-*]\s+" > /dev/null; then
            echo "PASS: [Unreleased] section has entries"
          else
            echo "ERROR: [Unreleased] section is empty. Add at least one entry before merging."
            echo ""
            echo "Required format:"
            echo "## [Unreleased]"
            echo "### Added"
            echo "- your change description"
            exit 1
          fi
```

---

## Step 2 — Update CI Job Dependencies

Make the changelog job required for merge:

```yaml
# In the existing PR status check configuration, add:
# (GitHub branch protection rules — manual step outside the YAML)
# Required status checks: build + test + changelog
```

This step requires a repository maintainer to configure the branch protection rule in GitHub Settings → Branches → `develop` → Required status checks.

---

## Step 3 — Commit

```bash
git add .github/workflows/ci.yml
git commit -m "ci: add CHANGELOG.md [Unreleased] gate to CI

PRs to develop must add an entry under ## [Unreleased] in CHANGELOG.md
before merging. This step enforces constitution §10.2.

Constitutes: constitution §10.2, plan T-12"
```

---

## Definition of Done

- [ ] `changelog` job added to `.github/workflows/ci.yml`
- [ ] Workflow passes on the PR that adds this gate
- [ ] Repository maintainer configures branch protection rule to require `changelog` job on `develop`
