# DELEGATE.md — Code Delegation Protocol

## Core Rule
**OpenClaw NEVER modifies code directly.** All code changes go to OpenCode.

OpenClaw's role is: write specs, delegate, review, merge, manage.

---

## 🔴 HARD RULE: English Only in Project Content

**All content written in the project must be in English.**
This applies to ALL of the following:
- SPEC.md and all documentation
- README files
- Code comments and commit messages
- Task descriptions in `.specify/tasks/`
- Any generated documentation
- GitHub PR descriptions and issues
- OpenCode task descriptions

**This rule must be included in every OpenCode delegation.**

---

## Tools

### OpenCode (AI Coding Agent) — v1.4.3
- **Location:** `/home/convergenceclaw/.local/bin/opencode`
- **Reads tasks from:** `.specify/tasks/` directory in the repo
- **Usage:** `opencode run 'task description' --dir /home/convergenceclaw/.openclaw/githubrepo/CSLibrary2026`
- **PTY required:** yes

### specify CLI — v0.6.1
- **Location:** `/home/convergenceclaw/.local/bin/specify`
- **Commands:** `specify init`, `specify check`, `specify integration`
- **Template structure:** `.specify/templates/`

---

## Project Structure

| Path | Purpose |
|------|---------|
| `.specify/SPEC.md` | Project-level specification |
| `.specify/tasks/` | Actionable task files for OpenCode |
| `.specify/templates/` | Spec templates (spec.md, tasks.md, contracts/, etc.) |
| `.specify/memory/` | Project memory/constitution |

---

## The Task File (for OpenCode)

Every code change starts with a **task file** in `.specify/tasks/<slug>.md`.

### Task File Template

```markdown
# Task: [Title]

## Objective
One clear sentence: what does success look like?

## HARD CONSTRAINTS
- All content in **English only**
- DO NOT delete any existing class, method, or field
- DO NOT restructure or reformat existing code
- DO NOT move code between files
- DO NOT change anything outside the exact lines specified
- If unsure, ASK instead of assuming

## Files to Modify
- `path/to/file.cs` — exact change (line numbers or snippet)

## Files NOT to Modify
- All other files

## Requirements
1. [Specific requirement with exact location]

## Branch
`feature/XX-<short-description>`

## Build & Test
```bash
export PATH="/home/convergenceclaw/.dotnet:$PATH"
cd /home/convergenceclaw/.openclaw/githubrepo/CSLibrary2026
dotnet pack -c Release
```

## Acceptance Criteria
- [ ] Build succeeds on all targets
- [ ] No breaking changes
- [ ] No code deleted outside the spec
```

---

## Delegation Workflow

### Step 1 — Analyze
OpenClaw reads files, understands the codebase.

### Step 2 — Write Task File
OpenClaw writes the task file at `.specify/tasks/<slug>.md`.
**Include HARD CONSTRAINTS and English-only rule.**

### Step 3 — Delegate to OpenCode
```bash
opencode run "Read and implement .specify/tasks/<slug>.md exactly.
All content must be in English.
After completing the code change, run: export PATH=/home/convergenceclaw/.dotnet:$PATH && cd /home/convergenceclaw/.openclaw/githubrepo/CSLibrary2026 && dotnet pack -c Release
If build fails, do NOT commit. Report the error." \
  --dir /home/convergenceclaw/.openclaw/githubrepo/CSLibrary2026
```

### Step 4 — OpenCode Implements
OpenCode reads the task file, writes code, commits.

### Step 5 — OpenClaw Reviews
- Pull the branch
- Run `dotnet pack -c Release`
- If clean → push and merge to `develop`
- If broken → file feedback, return to Step 3

---

## When to Delegate

| Task | Delegate? |
|------|-----------|
| Bug fixes | ✅ Yes — write task, delegate |
| New features | ✅ Yes — write task, delegate |
| Refactors | ✅ Yes — write task (with extra caution) |
| Code review | ✅ Yes — write task for changes |
| Read files / explore | ❌ No — OpenClaw does this |
| Documentation | ❌ No — OpenClaw does this |
| GitHub issue triage | ❌ No — OpenClaw does this |
| Build verification | ❌ No — OpenClaw does this |

---

## Anti-Patterns to Avoid

- ❌ OpenClaw writing code directly
- ❌ Delegating without a task file
- ❌ Delegating without HARD CONSTRAINTS
- ❌ Vague specs — be specific about every file and line
- ❌ Non-English content in project files
- ❌ "Can you also just..." — one task per delegation
- ❌ Skipping review — always verify build before merging

---

## CSLibrary2026 Specifics

| Item | Value |
|------|-------|
| Repo | `/home/convergenceclaw/.openclaw/githubrepo/CSLibrary2026` |
| Dotnet | `/home/convergenceclaw/.dotnet/dotnet` (v10.0.202) |
| Targets | `netstandard2.0`, `net10.0`, `net10.0-windows` |
| Build | `dotnet pack -c Release` |
| Task folder | `.specify/tasks/` |
| Stable branch | `main` |
| Active branch | `develop` |
| Default delegation target | `develop` |

---

## Handoff Checklist

Before calling OpenCode:
- [ ] Task file written at `.specify/tasks/<slug>.md`
- [ ] HARD CONSTRAINTS section included
- [ ] English-only rule stated
- [ ] Files to modify with exact line numbers
- [ ] Branch name defined
- [ ] Build command with correct PATH specified
- [ ] Acceptance criteria defined
