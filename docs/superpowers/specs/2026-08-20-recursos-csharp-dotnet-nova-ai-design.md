# recursos-csharp-dotnet-nova-ai — design

Status: approved in brainstorm, pending spec review
Date: 2026-08-20
Author: John Castro Sanabria (with Claude)

A public training repository that takes a working developer from C# runtime
semantics to senior-level distributed system judgement, in 30 modules of 2–4
hours each. Sibling to `recursos-python-nova-ai`, same teaching machinery,
different language and a harder ceiling.

---

## 1. What this is, and what it refuses to be

This is not an API catalogue. Every section opens with **when to reach for
something and when not to**, and every module carries a real-world case: a
concrete bug caused by not understanding that section, and what it cost.

The test of a module is not that it covers the surface area. It is that a
developer who finishes it can answer *the tell* — the question from the source
roadmap that separates recall from understanding. Each module's review
questions end with its tell, verbatim.

Prose is in English. Identifiers, folder names and commit messages are in
English too, so nothing is half-translated.

### Non-goals

- Not a C# syntax introduction. The reader can already write and run a console app.
- Not a cloud-provider course. Azure and AWS appear as context, never as prerequisites.
- Not a certification cram. Nothing is included because an exam mentions it.
- Not a framework tour. Libraries appear when a problem justifies them and are
  compared against doing without.

---

## 2. Verified technical baseline

Everything in this section was checked against primary sources on **2026-08-20**.
Nothing here is recalled from training data. Where a fact could not be
confirmed, it is marked and excluded from the build until it is.

### Platform

| Fact | Value | Source |
|---|---|---|
| Current LTS | **.NET 10.0**, `active` support phase, latest patch 10.0.11 (2026-08-11), EOL **2028-11-14** | `dotnet/core` releases-index.json |
| Previous LTS | .NET 8.0, `maintenance`, EOL **2026-11-10** — dies within 12 weeks of this spec | same |
| Next release | .NET 11.0, STS, currently preview.7 — **not used** | same |
| Language | **C# 14** — extension members, `field` keyword, null-conditional assignment, unbound generics in `nameof`, user-defined compound assignment | dotnet/docs whats-new/csharp-14 |
| Local SDK present | 10.0.201 | `dotnet --list-sdks` |
| PostgreSQL | **18.6** current stable; 19 is beta and not used. Testcontainers image `postgres:18-alpine` | docker-library/postgres versions.json |

Target framework is `net10.0`, pinned in `global.json` with
`rollForward: latestFeature` so a newer patch works and a newer major does not
silently change the material.

### Framework guidance, as the docs state it today

- **Minimal APIs**: current ASP.NET Core docs say *"Start with Minimal APIs for
  new projects."* Controllers remain the answer for model-binding extensibility
  (`IModelBinderProvider`, `IModelBinder`), advanced validation
  (`IModelValidator`), application parts, and OData. Module 08 teaches the
  choice, not a winner.
- **`IHttpClientFactory`**: still the recommended path. The docs name exactly
  one alternative — a long-lived `SocketsHttpHandler` with
  `PooledConnectionLifetime` configured to DNS refresh times. Module 08 teaches
  both, because the tell is *"why is a `static` HttpClient not the complete
  answer either?"*
- **Resilience**: `Microsoft.Extensions.Http.Resilience` (10.9.0) is the current
  path. The older `Microsoft.Extensions.Http.Polly` package and
  `AddTransientHttpErrorPolicy` still appear in parts of the docs and in most
  blog posts; module 19 says which is current and why.
- **Native AOT**: verified support matrix from the ASP.NET Core docs —
  supported: CORS, gRPC, HealthChecks, HttpLogging, JWT auth, Localization,
  OutputCaching, RateLimiting, RequestDecompression, ResponseCaching,
  ResponseCompression, Rewrite, StaticFiles, WebSockets. **Partial**: Minimal
  APIs, SignalR. **Not supported**: MVC, OData, Blazor Server, Session, and all
  authentication schemes other than JWT. Module 21 states this table rather
  than the usual "AOT is the future" hand-wave.
- **EF Core 10** (10.0.11): complex types gain table splitting, JSON mapping and
  struct support; named query filters; `ExecuteUpdate` over relational JSON
  columns; the .NET 10 `LeftJoin`/`RightJoin` operators; more consistent split-
  query ordering. Two security changes matter for teaching: **inlined constants
  are redacted from logs by default**, and EF now **warns on string
  concatenation with raw SQL APIs**. Npgsql provider 10.0.3.

### Licence audit — the finding that changed three decisions

Three libraries a .NET course would reach for by reflex are no longer free for
commercial use. Verified from the actual licence files and NuGet package
metadata, not from memory:

| Package | Current licence | Last permissive version |
|---|---|---|
| **FluentAssertions** | Xceed **Community License — Non-Commercial Use only**; commercial use of v8+ requires a paid licence (~$130/dev/yr) | 7.x, Apache-2.0, bug-fixes only |
| **MediatR** | custom `LICENSE.md` (Lucky Penny); 14.2.0 ships no SPDX expression | 12.4.1, Apache-2.0 |
| **AutoMapper** | custom `LICENSE.md` from 15.0.x onward; current is 16.2.0 | **14.0.0**, MIT |
| **MassTransit** | 9.x ships **no licence expression**, project URL moved to massient.com | 8.5.4, Apache-2.0 |

The Xceed licence text is explicit that non-commercial excludes use "by or for
an organisation… that charges fees or earns revenues". A public training repo
whose default `dotnet test` obliges the reader's employer to buy a licence is
not acceptable.

**Decision: free-only, and teach the mechanism.** See §4.6.

### Verified free and current

`xunit.v3` 4.0.0 (Apache-2.0) · `Shouldly` 4.3.0 (BSD-3) · `NSubstitute` 6.2.0
(BSD-3) · `Testcontainers.PostgreSql` 4.14.0 (MIT) ·
`Microsoft.EntityFrameworkCore` 10.0.11 · `Npgsql.EntityFrameworkCore.PostgreSQL`
10.0.3 · `Microsoft.AspNetCore.Mvc.Testing` 10.0.11 ·
`Microsoft.Extensions.Http.Resilience` 10.9.0 · `Polly` 8.7.0 ·
`OpenTelemetry.Extensions.Hosting` 1.17.0 · `Microsoft.Testing.Extensions.TrxReport`
2.3.3 · `Microsoft.Testing.Extensions.CodeCoverage` 18.10.0 · `Respawn` 7.0.0 ·
`Bogus` 35.6.5 · `Microsoft.CodeAnalysis.CSharp` 5.9.0 ·
`Scalar.AspNetCore` 2.17.1 · `MassTransit` 8.5.4 (Apache-2.0).

**`xunit.v3` 4.0.0 runs on Microsoft Testing Platform, not VSTest.** The stable
metapackage depends on `xunit.v3.mtp-v2` 4.0.0, so test projects are executables
(`<OutputType>Exe</OutputType>`) with `<TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>`,
and they take **neither** `Microsoft.NET.Test.Sdk` **nor** `coverlet.collector` —
both belong to the VSTest stack this repo does not use. TRX output for the
`stubs-are-empty` and `status` checks comes from
`Microsoft.Testing.Extensions.TrxReport`; coverage, if added later, from
`Microsoft.Testing.Extensions.CodeCoverage`. The xUnit getting-started page still
shows a `4.0.0-pre.108` package in its example; 4.0.0 stable is what this repo pins.

Every version is pinned in `Directory.Packages.props`. Versions not listed here
are verified against nuget.org at the moment the module that needs them is built.

---

## 3. Decisions taken in the brainstorm

| # | Decision | Rejected alternative |
|---|---|---|
| D1 | All 30 roadmap topics kept, in order; the four `[experience]` modules recast into testable form | Swapping them out; forcing the mechanism unchanged |
| D2 | Per-module project triple | One repo-wide triple; per-tier triples |
| D3 | One shared domain + five runnable system checkpoints | Domain-only; a full parallel capstone track |
| D4 | Domain is **order-to-cash for a marketplace** | Freight dispatch; ticketing |
| D5 | **MkDocs Material** with `same-dir`, `docs_dir: .` | Docusaurus; VitePress |
| D6 | Free-licence-only; teach the mechanism where the default went commercial | Pinning to last free versions; paying |
| D7 | Docker split happens **inside** each infra module, not between modules | Nine Docker-required modules; a cloud escape hatch |
| D8 | Two exercise tiers: `Core/` and `Challenge/` | Flat ordering; three tiers |
| D9 | Guide word count **hard-fails CI** outside 3,000–5,000 prose words | Warning only |

---

## 4. Architecture

### 4.1 Repository layout

```
recursos-csharp-dotnet-nova-ai/
├── README.md                     the map
├── START-HERE.md                 first 20 minutes + environment check
├── CONTRIBUTING.md
├── LICENSE                       MIT — code
├── LICENSE-CONTENT                CC BY 4.0 — guides
├── global.json                   SDK 10.0.2xx, rollForward latestFeature
├── Directory.Build.props         net10.0 · C# 14 · nullable · warnings-as-errors
├── Directory.Build.targets       the Exercises/Solutions swap, defined once
├── Directory.Packages.props      central package management, all versions pinned
├── .editorconfig                 analyzer rules at severity=error
├── run.sh / run.ps1              the learner's entry point
├── mkdocs.yml
├── .devcontainer/devcontainer.json
├── .github/
│   ├── workflows/{ci,docs,links}.yml
│   ├── ISSUE_TEMPLATE/{guide-error,resource}.yml
│   └── PULL_REQUEST_TEMPLATE.md
├── tools/
│   ├── Training.Scaffold/        generates a module triple from templates
│   └── Training.Audit/           invariant checker, also runs in CI
├── resources/
│   ├── links/                    curated per tier, one line on why each matters
│   ├── cheatsheets/
│   └── templates/
├── system/                       order-to-cash at five checkpoints
│   ├── checkpoint-1-runtime/
│   ├── checkpoint-2-http/
│   ├── checkpoint-3-data/
│   ├── checkpoint-4-domain/
│   └── checkpoint-5-distributed/
├── modules/
│   └── NN-slug/
│       ├── GUIDE.md
│       ├── examples/             runnable console/web projects
│       ├── src/
│       │   ├── Exercises/        Core/ + Challenge/, stubs that throw
│       │   └── Solutions/        Core/ + Challenge/, reference implementations
│       └── tests/
│           ├── UnitTests/        default tier, no infrastructure
│           └── IntegrationTests/ only in the nine modules that need it
└── docs/superpowers/{specs,plans}
```

### 4.2 The exercise mechanism

Exercises are unfinished classes that throw `NotImplementedException`. Their
tests fail on purpose until solved. C# cannot swap implementations at runtime,
so the swap happens at build time.

Each module holds two source projects with **identical namespaces and identical
public signatures**, differing only in assembly name:

- `modules/NN-slug/src/Exercises/Exercises.csproj` — `RootNamespace=Training.ModuleNN`
- `modules/NN-slug/src/Solutions/Solutions.csproj` — `RootNamespace=Training.ModuleNN`

Test projects reference exactly one, chosen by an MSBuild property. The
condition is written **once**, in the root `Directory.Build.targets`, rather
than pasted into 39 test projects:

```xml
<Project>
  <ItemGroup Condition="'$(IsTrainingTestProject)' == 'true'">
    <ProjectReference Include="$(MSBuildProjectDirectory)\..\..\src\Exercises\Exercises.csproj"
                      Condition="'$(UseSolutions)' != 'true'" />
    <ProjectReference Include="$(MSBuildProjectDirectory)\..\..\src\Solutions\Solutions.csproj"
                      Condition="'$(UseSolutions)' == 'true'" />
  </ItemGroup>
</Project>
```

`tests/UnitTests` and `tests/IntegrationTests` both sit two directories below
the module root, so the relative path resolves for either.

Test code contains no conditional compilation and no reflection. It does
`using Training.Module01.Core;` and compiles unchanged against whichever
assembly MSBuild supplied.

### 4.3 Keeping the two projects identical

A filename-pair check — the script requested in the brief — catches a missing
file. It does not catch the failure that actually bites: a stub whose parameter
was renamed, a solution that quietly grew an overload, a nullability annotation
that drifted. Those pass a filename check and hand the learner a test that
cannot compile against their stub.

`Training.Audit` therefore performs **three** checks:

1. **Pair check.** Every test file has a counterpart in both `src/Exercises`
   and `src/Solutions`, at the mirrored path.
2. **API surface parity.** Both assemblies are loaded and their public surface
   diffed — types, members, parameter names and types, return types, generic
   constraints, nullable annotations. Any drift fails.
3. **Anatomy check.** Every `GUIDE.md` has the eight required sections in
   order, the header carries prerequisites / time / skip-to, the module has
   5–8 exercises and 2–3 examples, and the prose word count is within
   3,000–5,000.

Word count is measured over **prose only** — fenced code blocks, tables and
front matter are excluded before counting. Counting raw words would let a guide
reach 3,000 with code listings, which is exactly the failure mode the gate
exists to prevent.

### 4.4 Test tiers

**Unit tier** — `tests/UnitTests`, xunit.v3 + Shouldly + NSubstitute. No
network, no Docker, no filesystem beyond test fixtures. Runs in seconds. This
is what the learner runs on save, and it is the large majority of exercises.

**Integration tier** — `tests/IntegrationTests`, a separate project *and*
`[Trait("Category","Integration")]`. Separate project so the default command
cannot reach it by accident; trait so `--filter` works when someone runs a whole
module from an IDE. Testcontainers 4.14.0 against real PostgreSQL 18, Respawn
between tests.

Nine modules have an integration project: 09, 10, 11, 12, 17, 18, 19, 24, 25.

The Docker-free split is **inside** each of those modules, not between modules.
Every module keeps a Core tier that needs nothing but the SDK. Concretely, in
module 09 the learner reasons over **captured real `EXPLAIN ANALYZE` output
supplied as text fixtures** — writing a plan analyser, identifying the scan that
should have been an index seek — while the live-database exercises are marked
as the fuller version of the same skill. In module 11, most exercises assert on
the **SQL EF Core generates** rather than on query results.

Target: ~85% of all exercises need only the SDK, and all 30 modules are
partially reachable without Docker. The README states this plainly, including
what a Docker-less reader is missing and why a fake would teach them something
false.

### 4.5 Quality gates

`Directory.Build.props` sets, for every project:

```xml
<TargetFramework>net10.0</TargetFramework>
<Nullable>enable</Nullable>
<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
<AnalysisMode>Recommended</AnalysisMode>
<ImplicitUsings>enable</ImplicitUsings>
```

**The stub problem.** A method body that only throws will trip unused-parameter
and related analyser rules. With warnings-as-errors that means a freshly cloned
repo does not build before the learner has written a line — an unacceptable
first experience. Fix: a nested `.editorconfig` under `src/Exercises/` relaxing
exactly four rules, each of which fires *only* because the body is a throw and
each of which stops applying the moment the learner writes a real
implementation:

| Rule | Why it fires on a stub |
|---|---|
| `IDE0060` | parameter is unused because nothing consumes it yet |
| `CA1801` | same, from the analyser package |
| `CA1822` | member could be static, because it never touches `this` yet |
| `CS1998` | `async` method has no `await`, because it only throws |

Nothing else is relaxed. Solutions, examples, tools, system checkpoints and the
learner's own completed code all stay under the full ruleset — so the moment an
exercise is solved, the full bar applies to it. The nested file is commented
with this table, which makes it a small lesson in analyser scoping rather than
a hole in the rules.

`dotnet format` enforces `.editorconfig` in CI and in a pre-commit hook. The
hook is a plain script installed by `./tools/install-hooks.sh` — no Husky, no
extra package, no third licence to re-audit in two years.

### 4.6 Commercially licensed libraries

Following D6, the repo depends on nothing that requires payment for commercial
use. Where the industry default has moved behind a licence, the guide teaches
the mechanism and then names the package honestly:

- **Assertions** — Shouldly throughout. Testing is a spine in the source
  roadmap rather than a module, so the FluentAssertions licence change is
  explained in `START-HERE.md` where the toolchain is introduced, and given its
  full treatment in module 29's supply-chain section.
- **CQRS (module 16)** — a hand-rolled mediator, roughly sixty lines over
  `IServiceProvider`, is the teaching vehicle. This is not a workaround; it is
  the better lesson, and it answers module 14's own question about which
  patterns the framework already gives you. MediatR is named, priced and
  compared in the same guide, because the reader will meet it in real codebases.
- **Mapping** — explicit mapping methods. AutoMapper discussed, not depended on.
- **Messaging (module 17)** — RabbitMQ and Kafka via Testcontainers for the
  semantics that differ between them; MassTransit 8.5.4 (Apache-2.0) shown as
  the abstraction layer, with its v9 licence change stated. Azure Service Bus
  is discussed, never required.
- **Mocking** — NSubstitute over Moq. Both are BSD-3 and free; NSubstitute has
  the cleaner syntax, and Moq's 2023 SponsorLink episode becomes a worked
  example in module 29's supply-chain section rather than a dependency.

### 4.7 CI

| Job | Command | Proves |
|---|---|---|
| `format` | `dotnet format --verify-no-changes` then a full build | style and analyser rules hold, warnings-as-errors passes |
| `solvable` | `dotnet test -p:UseSolutions=true` | **every published exercise is actually solvable** |
| `stubs-are-empty` | `dotnet test` + report analysis | no solution leaked into a stub |
| `audit` | `Training.Audit` | pair check, API parity, guide anatomy, word count |
| `integration` | `dotnet test --filter Category=Integration` | the Docker tier works against real PostgreSQL 18 |
| `docs` | `mkdocs build --strict` | site builds, no broken internal links |

**`stubs-are-empty` is a deliberate improvement on the sibling repo.** The
Python pipeline asserts the whole suite fails without solutions. That catches a
total leak but not a partial one: if module 14's stub ships with the answer,
module 14 goes green, everything else stays red, the aggregate still fails, and
CI reports success at catching nothing. Here the job parses the test report and
asserts **every exercise test class contains at least one failure**. One run,
no false green.

`solvable` and `audit` are the two jobs that make this repo different from most
teaching material, where the published exercise and its test have quietly
drifted apart.

### 4.8 Documentation site

MkDocs Material, `docs_dir: .` with the `same-dir` plugin, so every `GUIDE.md`
renders on the site *and* reads correctly on GitHub — no duplication, no copy
step, no link drift between the two. Excluded from the site: `src/Exercises`,
`src/Solutions`, `tests`, `tools`, `docs/superpowers`.

Deployed to GitHub Pages by `.github/workflows/docs.yml`. Navigation is grouped
by the six tiers. A separate `links.yml` workflow checks external links on a
schedule, as in the sibling repo.

### 4.9 The learner's loop, built for repetition

Exercises are one-shot by nature: once solved, they are solved, and a repo
optimised only for the first pass stops being useful the week after someone
finishes it. Three commands in `run.sh` / `run.ps1` fix that at near-zero cost:

| Command | What it does |
|---|---|
| `./run.sh test 03` | the default loop — module 03's unit tier, seconds, no Docker |
| `./run.sh status` | which modules are green, which are untouched, which are partly done |
| `./run.sh reset 03` | restores module 03's stubs from git so the module can be done again |

`status` matters more than it looks. Thirty modules is long enough that people
lose their place, and a visible map of what is done is the difference between a
path someone returns to and a repo someone abandons in tier 3.

`reset` is what makes the material re-practisable. It is a `git checkout` of one
module's `src/Exercises/` directory, refuses to run with uncommitted work
elsewhere, and prints what it is about to discard. Someone revisiting module 03
before an interview can redo it cold rather than reading their own past answers.

Neither command is infrastructure. Both are shell over `git` and `dotnet test`,
and both are covered by the same audit that guards everything else.

---

## 5. The domain: order-to-cash

One marketplace, seen from thirty depths. Orders, payments and a double-entry
ledger, inventory reservations, shipments, notifications.

It was chosen because it carries the three things a teaching domain must have:
a **money invariant** for tier 3 (payment capture under READ COMMITTED,
inventory rows under optimistic concurrency), a **genuine aggregate boundary**
for tier 4, and a workflow with an **uncompensable step** for module 18 — you
can refund a payment, you cannot un-ship a package.

`system/` holds the same service at five checkpoints, one per tier boundary.
Each checkpoint is runnable and each is also the "runnable example" budget for
the modules in that tier, so the marginal cost is wiring rather than new
material. A reader can diff checkpoint 3 against checkpoint 4 and see exactly
what introducing a domain model changed.

EF Core migrations are explicit everywhere. `EnsureCreated` appears nowhere in
the repository, including in examples — module 11 explains why it exists and
why using it costs you the ability to evolve a schema.

---

## 6. The 30 modules

⚓ = depth anchor (deep enough to teach). ♻ = recast from `[experience]`.
"Infra" = has an integration project; the module still has a Docker-free
Core tier.

| # | Slug | Tier | Infra |
|---|---|---|---|
| 01 | `type-system-and-memory` | Runtime | — |
| 02 | `object-lifetime-gc-and-disposal` | Runtime | — |
| 03 | `async-await-and-the-thread-pool` ⚓ | Runtime | — |
| 04 | `linq-and-the-modern-language-surface` | Runtime | — |
| 05 | `host-configuration-and-options` | ASP.NET Core | — |
| 06 | `dependency-injection` | ASP.NET Core | — |
| 07 | `the-middleware-pipeline` | ASP.NET Core | — |
| 08 | `the-http-surface` | ASP.NET Core | — |
| 09 | `sql-fluency` ⚓ | Data | ✔ |
| 10 | `transactions-and-concurrency` | Data | ✔ |
| 11 | `ef-core-internals` | Data | ✔ |
| 12 | `caching-and-read-paths` | Data | ✔ |
| 13 | `oop-and-solid-applied` | Design | — |
| 14 | `patterns-in-dotnet-idiom` | Design | — |
| 15 | `layered-architecture-and-ddd` | Design | — |
| 16 | `cqrs` | Design | — |
| 17 | `messaging-and-async-integration` ⚓ | Distributed | ✔ |
| 18 | `distributed-data-consistency` ⚓ | Distributed | ✔ |
| 19 | `resilience-and-observability` | Distributed | ✔ |
| 20 | `security-and-identity` | Distributed | — |
| 21 | `performance-engineering` | Senior | — |
| 22 | `concurrency-beyond-async` | Senior | — |
| 23 | `system-design-under-constraints` | Senior | — |
| 24 | `data-at-scale` | Senior | ✔ |
| 25 | `evolutionary-architecture-zero-downtime` | Senior | ✔ |
| 26 | `inherited-and-legacy-systems` ♻ | Senior | — |
| 27 | `production-ownership` ♻ | Senior | — |
| 28 | `cost-and-capacity` ♻ | Senior | — |
| 29 | `threat-modeling-at-design-time` | Senior | — |
| 30 | `technical-influence` ♻ | Senior | — |

Tier 3 is pinned to PostgreSQL specifically rather than left engine-agnostic.
Tier 5 carries the microservices weight, because service boundaries are decided
by messaging semantics and consistency requirements, not by a diagram.

### 6.1 The four recast modules

**26 — Inherited and legacy systems.** The strongest of the four as exercise
material. The learner receives a 500-line `LegacyPricingEngine`: statics,
`DateTime.Now`, no seams, one real bug. Core: pin current behaviour with
characterization tests *including its bugs*; introduce seams (`IClock`,
`ITaxTable`) without changing behaviour; extract a pure function. Challenge:
identify the behaviour that is genuinely a bug, change it deliberately, and
leave the test that documents the decision. Also: decide what to leave alone,
and write down why.

**27 — Production ownership.** Pure functions over realistic fixtures: latency
percentiles from a request log — including why a p99 averaged across buckets is
arithmetically meaningless; error-budget burn rate; a multi-window alert rule;
symptom-versus-cause alert classification; correlation-ID stitching across a
three-service log. Plus a written postmortem graded against a rubric printed in
the guide, with the guide stating outright that CI cannot grade it.

**28 — Cost and capacity.** Unit cost per request as a calculator (compute,
egress, storage, per-query); capacity planning against a growth rate and a
headroom target; build-versus-buy over a system's life; ranking three
optimisations by payback period; spotting from a plan-plus-row-count fixture
which query's cost scales with data volume. Testable arithmetic most engineers
have never done once.

**30 — Technical influence.** The weakest fit, and the guide's header says so
rather than pretending otherwise. The testable half is real: **architecture
fitness functions** — tests that enforce the dependency rule, that no endpoint
touches `DbContext`, that every public endpoint declares its status-code set. A
fitness function is an argument encoded so you stop having to win it in review
every quarter. Plus an ADR linter (does the record name its rejected
alternatives and its reversal cost?) and a design-doc review exercise scored
against an expected findings set. The un-gradable half — write two ADRs, one
arguing against something you would personally enjoy building — is rubric-only
and labelled as such in the guide.

---

## 7. Guide anatomy — fixed, and enforced

Every `GUIDE.md`, in this order. `Training.Audit` fails the build if a section
is missing or out of order.

1. **Header** — prerequisites, realistic time, and where to skip to if you
   already know this. The skip-to line points at `Challenge/`, which the reader
   can actually run, not at a vague reassurance.
2. **Objectives** — what you can do afterwards, in verbs.
3. **Sections** — each opening with when to use the thing and when not to.
   Judgement before syntax, without exception.
4. **REAL-WORLD CASE** — a specific bug caused by not understanding this
   module, with what it cost. Not a parable. Names the mechanism, the symptom,
   how long it went unnoticed, and the price. **The failure must be
   mechanically reproducible in the module's own `examples/`** — the reader can
   run the bug, then run the fix. **The cost is derived, not asserted**: stated
   as arithmetic over named assumptions (volume, rate, unit price) that the
   reader can check and disagree with, never as a fabricated invoice. See §7.1.
5. **Exercises** — 5–8, split Core / Challenge, each linked to its test command.
6. **Summary**.
7. **Review questions** — ending with the module's tell from the roadmap,
   verbatim. Answers in a collapsible block, which renders as a spoiler on the
   site and as `<details>` on GitHub.
8. **Resources** — verified links only, each with one line on why it is worth
   the reader's time.

3,000–5,000 prose words. Under 3,000 means the module is incomplete: go back to
the source and find what is missing. This is enforced, not requested.

### 7.1 How a real-world case is written

Two failure modes are available here and both are bad. A case with invented
specifics — "it cost us $40,000 and took three weeks to find" — has narrative
punch and is folklore: the reader repeats it in an interview and cannot defend
a single number in it. A case with no cost at all is an academic footnote that
nobody remembers by module 12.

The rule, chosen because this repo is for repeated practice rather than one
read-through:

- **The mechanism is real and runnable.** Every case is reproduced by a program
  in the module's `examples/` folder. The reader runs it, sees the wrong
  behaviour, then runs the fixed version. A case the reader can execute is
  practice; a case they can only read is a story.
- **The cost is derived in front of the reader.** Not "$40,000" but "a duplicate
  charge on the retried checkouts — at 20,000 orders/day, a 0.3% retry rate and
  a $45 average basket, that is roughly $2,700/day before chargeback fees at
  ~$15 each." Every input is named, and the reader is invited to substitute
  their own company's numbers.
- **The detection gap is part of the cost.** How long it ran before anyone
  noticed, and what would have caught it sooner. This is where the case earns
  its place: the fix is usually one line, and the lesson is never the fix.

This has a structural payoff. Module 28 teaches unit-cost-per-request
arithmetic; by the time the reader arrives there they have already done that
arithmetic twenty-seven times in the real-world cases, and module 28 names the
skill they have been practising unnamed.

---

## 8. Module 01 in full — the bar for the other 29

`modules/01-type-system-and-memory/`

**Header.** Prerequisites: you can write and run a console app; no .NET
internals assumed. Time: 3–4 hours. Skip to `Challenge/` if you can already
explain why `record struct` equality differs from `class` equality and name
three places boxing hides.

**Objectives.** Predict whether two values compare equal and say which member
decided it · name the places boxing occurs and measure the cost · choose
between `class`, `struct`, `readonly struct` and `record` with a reason that is
not style · implement the equality contract so a type survives a hash-based
collection · recognise the defensive-copy tax · explain what `string` interning
does and does not buy.

**Sections.** Values and references: what the variable actually holds ·
why "stack and heap" is the wrong mental model to lean on, and the three places
it does matter · `struct`, `readonly struct`, `ref struct`, and the defensive
copy · the six places boxing hides · the `==` / `Equals` / `GetHashCode` /
`IEquatable<T>` contract, and what breaks when you satisfy one and not the
others · records, and the three times value equality is wrong · string interning
· mutability, and `readonly` fields that are not.

**Real-world case.** A `BasketKey` record with a `List<LineItem>` member. Record
value equality compares that list **by reference**, so two structurally
identical baskets are unequal, so the idempotency cache never hits, so a retried
checkout charges the customer twice. Not caught by monitoring — caught by a
customer. The guide walks the cost: chargebacks, the manual reconciliation, the
support load, and the fact that the fix is one line and the detection gap was
three weeks. It plants the seed for module 17, where the identical mistake
reappears as a non-idempotent consumer.

**Exercises.** Core (5): `Money` as a `readonly record struct` with a
currency-mismatch guard · the full equality contract on a class, verified by a
`HashSet` round trip · an allocation exercise whose test asserts against
`GC.GetAllocatedBytesForCurrentThread()` — real measurement, no Docker · fixing
`BasketKey` so the idempotency cache hits · a `readonly struct` defensive-copy
puzzle. Challenge (3): avoiding boxing under an interface constraint · a symbol
table where reference equality is safe · a zero-allocation `foreach` over a
custom collection.

**Examples (3).** Equality surprises printed side by side · boxing costs with
allocation counters before and after · the basket bug reproduced, then fixed.

**Review questions** end with the roadmap tell: *"Two objects compare equal but
aren't the same instance. Walk me through what `==`, `Equals` and `GetHashCode`
each did — and what changes if it's a record."*

Word target 3,500–4,500.

---

## 9. Build phases

**Phase 0 — machinery.** Repo skeleton, `Directory.Build.*`, central package
management, `.editorconfig`, `run.sh`, `Training.Scaffold`, `Training.Audit`,
all six CI jobs, MkDocs site, devcontainer, licences, issue and PR templates.
Proven green against a throwaway module that is deleted before phase 1. Nothing
in this phase depends on content decisions.

**Phase 1 — module 01, complete, then stop.** Guide, three examples, eight
exercises in triplicate, all CI jobs green. Committed. **Work halts here for
review**, because this module sets the bar — voice, depth, exercise difficulty,
how a real-world case reads — for the other twenty-nine.

**Phase 2 — modules 02–30.** Subagents, one module per agent, one commit per
module. Each agent receives the module spec, the module-01 exemplar as the
explicit bar, and the verification requirements. No module is committed until
`solvable`, `stubs-are-empty` and `audit` pass for it locally.

**Phase 3 — system checkpoints.** The five `system/` snapshots, landing at tier
boundaries as the modules in each tier complete.

**Phase 4 — publish.** Public repo under `full-stack-dev-johncastrosanabria`,
GitHub Pages live, README and START-HERE final.

**Scale, stated honestly.** Thirty guides at 3,000–5,000 words is
105,000–150,000 words. Add roughly 200 exercises in triplicate, ~90 example
programs and five runnable checkpoints. This is a multi-session build.

---

## 10. Risks

**Word-count gate versus quality.** A hard gate can be satisfied by padding.
Mitigation: the gate counts prose only, and the module-01 exemplar sets a
density bar that subagents are measured against; thin-but-long guides are
caught in review, not by CI. The gate catches the failure it can catch — a
guide that stopped early — and no more.

**Subagent voice drift.** Thirty agents will not sound like one author.
Mitigation: module 01 shipped and reviewed first, a written voice section in
`CONTRIBUTING.md`, and the fixed anatomy doing structural work.

**Version drift.** .NET 11 lands November 2026 and .NET 8 dies November 2026.
Mitigation: `global.json` pins the SDK band; every guide's Resources section
links the versioned doc; a dated "verified against" line in the README.

**Real-world cases must be true.** A fabricated incident is worse than none —
the reader repeats it and cannot defend it. Mitigation: the policy in §7.1. The
mechanism is reproducible in the module's own `examples/`, and the cost is
arithmetic over named assumptions the reader can check, never an asserted
figure. No invented dates, companies or invoices appear anywhere in the repo.

**Docker-free proxies teaching something false.** Asserting on generated SQL is
not the same as asserting on query results. Mitigation: every proxy exercise
names what it is standing in for, and the integration tier exists in the same
module for readers who can run it.

---

## 11. Open items

None blocking. The three items flagged during the brainstorm are now resolved
and recorded in §2: the Native AOT support matrix, the MassTransit v9 licence
change, and the current PostgreSQL major.

Package versions for modules not yet built are verified against nuget.org at
the time that module is written, not assumed from this document.
