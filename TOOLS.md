# TOOLS.md - Local Notes

Skills define _how_ tools work. This file is for _your_ specifics — the stuff that's unique to your setup.

## Project Context

This workspace is for **CSLibrary2026** — the CSL RFID Reader .NET library.

### .NET SDK
- **Location:** `/home/convergenceclaw/.dotnet/dotnet`
- **Version:** .NET 10.0.202
- **Add to PATH:** `export PATH="/home/convergenceclaw/.dotnet:$PATH"`

### Project Relationships
- **This workspace:** CSLibrary2026 library (`https://github.com/cslrfid/CSLibrary2026`)
  - Produces: `CSLibrary2026.nupkg` NuGet package
  - Consumers: MAUI apps, Xamarin apps
- **Separate project:** CSLRFIDReader-C-Sharp-MAUIAPP
  - GitHub: `cslrfid/CSLRFIDReader-C-Sharp-MAUIAPP`
  - References CSLibrary2026 as a NuGet package
  - Managed by a different agent

### NuGet Package Build
```bash
export PATH="/home/convergenceclaw/.dotnet:$PATH"
cd /home/convergenceclaw/.openclaw/githubrepo/CSLibrary2026
dotnet pack -c Release
```

---

## OpenCode + specify Toolchain

### OpenCode (AI Coding Agent)
- **Binary:** `/home/convergenceclaw/.local/bin/opencode`
- **Version:** 1.4.3
- **Reads tasks from:** `.specify/tasks/` in repo
- **Requires PTY:** yes

### specify CLI (Spec Kit Manager)
- **Binary:** `/home/convergenceclaw/.local/bin/specify`
- **Version:** 0.6.1
- **Repo structure:** `.specify/` (templates, tasks, memory)

### OpenCode Delegation
```bash
opencode run "[task description]" \
  --dir /home/convergenceclaw/.openclaw/githubrepo/CSLibrary2026
```
See `DELEGATE.md` for full protocol.

---

## GitHub Configuration
- **Owner:** cslrfid
- **Repo:** CSLibrary2026
- **Default branch:** main
- **Repo path:** `/home/convergenceclaw/.openclaw/githubrepo/CSLibrary2026`

---

## What Goes Here

Things like:

- Camera names and locations
- SSH hosts and aliases
- Preferred voices for TTS
- Speaker/room names
- Device nicknames
- Anything environment-specific

## Examples

```markdown
### Cameras

- living-room → Main area, 180° wide angle
- front-door → Entrance, motion-triggered

### SSH

- home-server → 192.168.1.100, user: admin

### TTS

- Preferred voice: "Nova" (warm, slightly British)
- Default speaker: Kitchen HomePod
```

## Why Separate?

Skills are shared. Your setup is yours. Keeping them apart means you can update skills without losing your notes, and share skills without leaking your infrastructure.

---

Add whatever helps you do your job. This is your cheat sheet.
