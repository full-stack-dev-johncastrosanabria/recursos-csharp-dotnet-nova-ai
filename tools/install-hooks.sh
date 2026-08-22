#!/usr/bin/env bash
# Installs the repo's git hooks. Plain scripts, no package, nothing to
# re-audit for licence changes in two years.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cp "$REPO_ROOT/tools/hooks/pre-commit" "$REPO_ROOT/.git/hooks/pre-commit"
chmod +x "$REPO_ROOT/.git/hooks/pre-commit"
echo "Installed .git/hooks/pre-commit"
