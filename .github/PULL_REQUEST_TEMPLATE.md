## What this changes

<!-- One or two sentences: what module or tool, and why. -->

## Checklist

- [ ] `dotnet format --verify-no-changes` passes
- [ ] `dotnet run --project tools/Training.Audit -- all` is clean (or the finding is expected and explained below)
- [ ] If this touches a module: `./run.sh test NN` fails red against the stubs, and passes against `dotnet test --project modules/NN-*/tests/UnitTests -p:UseSolutions=true`
- [ ] If this adds or edits a guide: the eight sections are present and in order, and the prose word count is 3,000-5,000
- [ ] If this touches `mkdocs.yml`, `README.md`, `START-HERE.md`, `CONTRIBUTING.md`, or a guide: `mkdocs build --strict` still passes

## Notes for reviewers

<!-- Anything a reviewer should look at closely, or context that doesn't fit above. -->
