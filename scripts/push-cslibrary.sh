#!/bin/bash
# Daily push script for CSLibrary2026 — pushes to develop branch
# Runs at 18:30 UTC every day

WORKSPACE="/home/node/.openclaw/workspace-GithubProject-CSLibrary2026"
GITDIR="/tmp/CSLibrary2026-git"
REPO="https://github.com/cslrfid/CSLibrary2026.git"
TOKEN_GH="<SEE ~/.openclaw/secrets/vault.md>"
USER_EMAIL="agent@openclaw.ai"
USER_NAME="GithubProject-CSLibrary2026"

cd "$GITDIR" || exit 1

# Pull latest
git pull origin develop 2>/dev/null

# Sync updated source files
rsync -a --delete "$WORKSPACE/CSLibrary2026/Source/" "$GITDIR/Source/"
cp "$WORKSPACE/CSLibrary2026/CSLibrary2026.csproj" "$GITDIR/"
cp "$WORKSPACE/CSLibrary2026/CSLibrary2026/README.md" "$GITDIR/"

# Remove .bak files
find "$GITDIR" -name "*.bak" -delete 2>/dev/null

# Check for changes
cd "$GITDIR"
if git diff --quiet && git diff --cached --quiet; then
    echo "No changes to push at $(date)"
    exit 0
fi

# Switch to develop branch
git checkout develop 2>/dev/null || git checkout -b develop

# Commit
git add -A
git commit -m "Auto-push $(date -u '+%Y-%m-%d %H:%M UTC')" --author="GithubProject-CSLibrary2026 <agent@openclaw.ai>"

# Push to develop
git remote set-url origin "https://mephist-cne:${TOKEN_GH}@github.com/cslrfid/CSLibrary2026.git"
git push origin develop

echo "Pushed to develop at $(date)"
