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
    return 1
  fi
  if (( ${#matches[@]} > 1 )); then
    echo "Ambiguous module number ${number}: ${matches[*]}" >&2
    return 1
  fi
  echo "${matches[0]}"
}

# Every discovered unit-tier test project, repo-relative, one per line.
# Discovery comes from Training.Audit rather than a glob on a hardcoded tier
# name, so a module that adds a new tier (say tests/ContractTests) is never
# silently skipped here. Filtered to the unit tier on purpose: integration
# tests need Docker and are deliberately excluded from the everyday
# test/status loop (see README.md).
#
# A failure of the discovery command itself must be loud and non-zero; a
# genuinely empty repo (no modules yet, or none with a unit tier) must stay
# a clean, silent success — those two look identical by output alone (both
# produce no project paths), so the discovery command's own exit status is
# checked explicitly rather than inferred from empty output. This function
# is always invoked through a command substitution, where bash's -e is not
# honoured by default (no `inherit_errexit`), so the check below cannot
# rely on `set -e` catching the failure for us — it has to be explicit.
unit_test_projects() {
  local discovered
  discovered="$(dotnet run --project tools/Training.Audit -- test-projects)" || return 1
  printf '%s\n' "$discovered" | grep '/UnitTests$' || true
}

case "${1:-}" in
  test)
    if [[ -n "${2:-}" ]]; then
      target="$(module_path "$2")" || exit 1
      dotnet test --project "$target/tests/UnitTests"
    else
      # dotnet test takes one project at a time, so iterate rather than glob.
      # A failing module must not hide every module after it, so keep going
      # on failure — but remember it happened, so the script's own exit
      # code still tells the truth about whether everything passed.
      #
      # Captured into a variable rather than piped via process substitution:
      # a process substitution's exit status is invisible to the parent
      # shell, which would silently swallow a discovery failure.
      projects="$(unit_test_projects)" || exit 1
      failed=0
      while IFS= read -r project; do
        [[ -n "$project" ]] || continue
        dotnet test --project "$project" || failed=1
      done <<< "$projects"
      exit "$failed"
    fi
    ;;

  status)
    mkdir -p artifacts
    rm -f artifacts/*.trx
    projects="$(unit_test_projects)" || exit 1
    while IFS= read -r project; do
      [[ -n "$project" ]] || continue
      name="$(basename "$(dirname "$(dirname "$project")")")"
      # A non-zero exit is expected: unsolved exercises are failing tests.
      dotnet test --project "$project" \
        --report-trx --report-trx-filename "$name.trx" --results-directory artifacts || true
    done <<< "$projects"
    dotnet run --project tools/Training.Audit -- status --trx artifacts
    ;;

  reset)
    [[ -n "${2:-}" ]] || { usage; exit 2; }
    module="$(module_path "$2")" || exit 1
    target="$module/src/Exercises"

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
