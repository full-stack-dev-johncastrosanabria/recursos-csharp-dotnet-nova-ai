#!/usr/bin/env bash
# Regenerates training.slnx from scratch so it always reflects the .csproj
# files actually present in the repo. Deterministic: the project list is
# sorted, so running this twice in a row produces a byte-identical file,
# and CI can detect drift with `git diff --exit-code training.slnx`.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
cd "$REPO_ROOT"

rm -f training.slnx
dotnet new sln --format slnx -n training

PROJECTS=()
while IFS= read -r project; do
  PROJECTS+=("$project")
done < <(
  find . \
    \( -type d \( -name bin -o -name obj -o -path './.superpowers' -o -path './.superpowers/*' \) \) -prune -o \
    -type f -name '*.csproj' -print |
    sed 's|^\./||' |
    sort
)

if [ "${#PROJECTS[@]}" -gt 0 ]; then
  dotnet sln training.slnx add "${PROJECTS[@]}"
fi
