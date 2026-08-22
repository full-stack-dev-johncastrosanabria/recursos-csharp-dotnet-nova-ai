# Contributing

This repository is built to a fixed structure and enforced automatically.
Most of what would normally be "please remember to..." in a contributing
guide is instead a CI job that fails your pull request. This document covers
that structure, how to work within it, and — separately — the handful of
conventions that exist precisely *because* no check can catch them.

## Guide anatomy

Every module's `GUIDE.md` has the same eight sections, in the same order.
`Training.Audit` fails the build if a section is missing or out of order.

1. **Before you start** — prerequisites, realistic time, and where to skip
   to if you already know the material.
2. **Objectives** — what the reader can do afterwards, stated as verbs.
3. **Sections** — the teaching content. Every section opens with when to
   use the thing it covers and when not to.
4. **Real-world case** — a specific, reproducible failure caused by not
   understanding this module, with a derived cost. See "The voice" below.
5. **Exercises** — 5–8, split Core / Challenge, each tied to a test you can
   run.
6. **Summary**.
7. **Review questions**.
8. **Resources** — verified links, each with one line on why it's worth the
   reader's time.

## The word-count rule

Every guide must land between **3,000 and 5,000 prose words**. Under 3,000
means the module stopped before the material did; CI treats that as a
build failure, not a warning. `Training.Audit`'s `guides` check enforces
both the anatomy and the word count.

The count is over prose only. Front matter, fenced code blocks, table rows,
and heading lines are excluded before counting — see "Four conventions no
check can enforce" below for why that matters when you're writing.

## Adding a module

Use the scaffolder rather than copying an existing module by hand:

```bash
dotnet run --project tools/Training.Scaffold -- new-module <slug> "<title>"
# example:
dotnet run --project tools/Training.Scaffold -- new-module 07-the-middleware-pipeline "The middleware pipeline"
```

`<slug>` must start with the two-digit module number followed by a hyphen
(the number in the slug and the module's position must agree). This
generates `modules/<slug>/` with:

- `src/Exercises/{Core,Challenge}` and `src/Solutions/{Core,Challenge}` —
  matching `.csproj` files with an identical root namespace, ready for
  exercises and their reference implementations.
- `tests/UnitTests/{Core,Challenge}` — a test project pre-wired for
  `xunit.v3`, `Shouldly`, and TRX reporting.
- `examples/` — an empty directory for the runnable programs your
  real-world case will reproduce.
- `GUIDE.md` — a skeleton with the eight required section headings already
  in place and in order, so a scaffolded module can never start life
  failing the anatomy check.

The scaffolder refuses to touch a module directory that already exists,
specifically so a second run can never silently erase a guide you've
already written. If you need to regenerate a module, delete its directory
first.

Nine modules also need an integration project (`tests/IntegrationTests`,
Testcontainers against PostgreSQL) — the scaffolder does not generate that
tier; add it by hand, following the pattern of another integration module,
and mark its tests `[Trait("Category", "Integration")]`.

## Running the audit locally

`Training.Audit` is what CI's `audit` job runs, and you should run it
before opening a pull request:

```bash
dotnet run --project tools/Training.Audit -- all
```

This runs three checks together: pair check (every test file has a
counterpart in both `src/Exercises` and `src/Solutions`), API surface
parity (the two projects' public APIs must be identical), and guide
anatomy plus word count. You can run any one of them on its own with
`pairs`, `api`, or `guides` instead of `all`. Two further subcommands take
a `--trx <path>` pointing at test results: `stub-leak` confirms no stub
ships with the answer already written into it, and `status` renders the
per-module solved/unsolved table that `./run.sh status` uses.

Exit code `0` means clean, `1` means there are findings (each printed to
stderr as `[check] path: message`), and `2` means a usage error — a
malformed command, or a `--trx` file the tool couldn't read.

There's also a pre-commit hook (`tools/hooks/pre-commit`) that runs
formatting on staged `.cs` files and then this same `audit -- all`. Install
it with `tools/install-hooks.sh` if you want the check to run before every
commit rather than only in CI.

## Four conventions no check can enforce

Everything above is a CI gate: get it wrong and the build tells you.
These four are not gated — each one either fails silently (the build stays
green, the guide is wrong anyway) or is a matter of judgement a linter
cannot rule on. Know them going in.

### 1. Don't name a type after its own namespace segment

This is a compiler trap that a green build can still walk into later. If a
module's root namespace is, say, `Training.Module07`, do not declare a type
literally named `Module07` (or, more generally, any type whose name matches
one of its own namespace's segments). The concrete failure mode: the
throwaway module in this repo lives at `Training.Probe.*`; if it declared a
type named `Probe`, that type could not be referenced from its own test
project at `Training.Probe.Tests.*`, because the compiler resolves the
identifier `Probe` against the namespace segment first and reports
`CS0234` — "the type or namespace name does not exist" — even though the
type is right there. This compiles fine in isolation and only breaks once
something downstream tries to reference it by its short name, so review
for it by eye; nothing in CI catches it.

### 2. Tables must use leading-pipe rows

The word counter excludes a line from the prose count only if it starts
with `|`. Standard Markdown tables are lenient about this —
`Column A | Column B` without a leading pipe still renders as a table in
most viewers — but here it means the row is counted as prose instead of
being excluded, quietly inflating the word count toward the 5,000 ceiling.
Always write:

```markdown
| Column A | Column B |
|---|---|
| value | value |
```

not `Column A | Column B` without the leading and trailing pipes.

### 3. Code samples must be fenced, never four-space indented

The word counter only recognises fenced blocks (opened and closed with
`` ``` `` or `~~~`) as code; an indented block is invisible to it and gets
counted as prose, again pushing the count in the wrong direction and,
worse, reading as narration instead of as code on the rendered site.

### 4. The voice: judgement before syntax

Every section in "Sections" opens with when to reach for the thing it's
teaching and when not to — not a syntax dump followed by a caveat at the
end. A reader should be able to decide whether a technique applies to
their situation before they've seen a single line of code that implements
it.

The real-world case in every guide follows the same discipline:

- **The mechanism is reproducible.** Every case is backed by a runnable
  program in the module's own `examples/` folder. The reader runs it, sees
  the failure, then runs the fix. A case the reader can only read is a
  story; a case they can run is practice.
- **The cost is derived, never asserted.** Not "this cost the company
  $40,000" but arithmetic over named, checkable assumptions — a volume, a
  rate, a unit price — so the reader can verify the number or substitute
  their own and get a different, equally defensible one. An invented figure
  has no defensible number behind it and teaches the reader to repeat a
  claim they can't back up.

Guides in this repo are read by people who will practice the material
repeatedly, not skim it once. Padding survives a single read; it does not
survive the fifth.
