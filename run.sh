#!/usr/bin/env bash
# The learner's entry point. Three verbs, nothing clever.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$REPO_ROOT"

usage() {
  cat <<'USAGE'
usage:
  ./run.sh test [NN]   run a module's unit tests (all modules if NN omitted)
  ./run.sh status      show which modules are solved
  ./run.sh reset NN    restore a module's stubs so you can do it again

  NN is the module number, e.g. 03. Integration tests are excluded from
  `test` by design: they need Docker. Run them with
  `dotnet test --project modules/NN-*/tests/IntegrationTests`.
USAGE
}

module_path() {
  local number="$1"
  shopt -s nullglob
  local matches=(modules/"${number}"-*)
  if [[ ! -d "${matches[0]:-}" ]]; then
    echo "No module numbered ${number}." >&2
    exit 1
  fi
  echo "${matches[0]}"
}

case "${1:-}" in
  test)
    if [[ -n "${2:-}" ]]; then
      dotnet test --project "$(module_path "$2")/tests/UnitTests"
    else
      # dotnet test takes one project at a time, so iterate rather than glob.
      shopt -s nullglob
      for project in modules/*/tests/UnitTests; do
        dotnet test --project "$project"
      done
    fi
    ;;

  status)
    mkdir -p artifacts
    rm -f artifacts/*.trx
    shopt -s nullglob
    for project in modules/*/tests/UnitTests; do
      name="$(basename "$(dirname "$(dirname "$project")")")"
      # A non-zero exit is expected: unsolved exercises are failing tests.
      dotnet test --project "$project" \
        --report-trx --report-trx-filename "$name.trx" --results-directory artifacts || true
    done
    dotnet run --project tools/Training.Audit -- status --trx artifacts
    ;;

  reset)
    [[ -n "${2:-}" ]] || { usage; exit 2; }
    target="$(module_path "$2")/src/Exercises"

    # Refuse only if there is uncommitted work OUTSIDE the module being reset.
    # Work inside it is exactly what the learner is choosing to discard.
    outside="$(git status --porcelain -- . ":(exclude)${target}" | wc -l | tr -d ' ')"
    if [[ "$outside" != "0" ]]; then
      echo "You have uncommitted changes outside ${target}." >&2
      echo "Commit or stash them first — reset should never touch them." >&2
      exit 1
    fi

    echo "About to discard your work in ${target}:"
    git status --porcelain -- "$target"
    read -r -p "Type the module number again to confirm: " confirm
    [[ "$confirm" == "$2" ]] || { echo "Cancelled."; exit 1; }

    git checkout HEAD -- "$target"
    echo "Reset ${target}. Run ./run.sh test $2 to start again."
    ;;

  *)
    usage
    exit 2
    ;;
esac
