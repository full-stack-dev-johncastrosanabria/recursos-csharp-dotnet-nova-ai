# Recursos C# · .NET · Nova AI

A thirty-module training path that takes a team from C# runtime semantics —
how the GC actually behaves, what a struct costs you, what `async` really
does to your call stack — through to the judgement a senior engineer applies
to a distributed system: messaging, resilience, observability, and the
trade-offs behind each of those. Each module is a guide plus exercises you
solve yourself, not a set of slides to skim.

It is for a team, not a solo learner working through a textbook. Every
module has a fixed anatomy, a real-world case with a derived (not invented)
cost, and a CI pipeline that proves every exercise is actually solvable
before it's ever handed to anyone. If you are hiring or onboarding engineers
into C#/.NET and distributed-systems work and want them building working
software from day one rather than reading about it, this path is shaped for
exactly that.

There is a Spanish-language sibling repository, `recursos-python-nova-ai`,
covering the same territory for Python. This one is English, and it is
.NET.

## The Docker split, honestly

Roughly **85% of exercises need only the .NET SDK.** No Docker, no
containers, no network — `dotnet test` against an in-memory fixture, done in
seconds. The remaining exercises are the **integration tier**: nine modules
(covering databases, messaging, and distributed coordination) also ship a
separate `tests/IntegrationTests` project that runs against a real
PostgreSQL 18 container via Testcontainers. That tier is Docker-only by
design — it exists specifically to prove your code against the real thing,
not a mock of it.

**Every module is at least partially reachable without Docker.** Where a
module's core skill genuinely needs a live database, the SDK-only exercises
substitute the closest honest proxy instead of skipping the material. For
example, module 09 has you write a query-plan analyser against **captured,
real `EXPLAIN ANALYZE` output supplied as text fixtures** — the reasoning
skill without the container. Module 11's SDK-only exercises assert on the
**SQL that EF Core generates**, rather than on query results a live database
would return.

Be clear about what that is: a proxy, not a substitute. Asserting on
generated SQL is not the same as asserting on query results — it tells you
whether EF Core produced the query you intended, not whether that query
behaves correctly against a real planner, real indexes, and real
concurrency. A reader without Docker gets the reasoning skill but not the
verification that the reasoning was right. Faking that verification —
asserting against a mock database tuned to return whatever the "correct"
answer is supposed to be — would teach something false: it would pass
regardless of whether the SQL actually works, which defeats the entire
point of an exercise. So the SDK-only exercises are honestly labelled as
partial, and the integration tier stays available for whoever can run it,
in the same module, testing the same skill.

## The three commands

```bash
./run.sh test [NN]   # run a module's unit tests (all modules if NN omitted)
./run.sh status       # show which modules are solved
./run.sh reset NN     # restore a module's stubs so you can do it again
```

`NN` is the module number, e.g. `03`. Integration tests are excluded from
`test` by design — they need Docker. Run them explicitly with
`dotnet test --project modules/NN-*/tests/IntegrationTests`.

## The first run is red on purpose

Clone the repo and run `./run.sh test` before touching anything, and it
fails. Every exercise starts as a stub that throws `NotImplementedException`.
That failure is not a bug in the repository — it is the entire design. The
tests define the contract; you make them pass by writing the implementation.
A course where the starting state is green has already given you the
answer. See [START-HERE.md](START-HERE.md) for what that first red run looks
like and what to do next.

---

Verified against .NET 10.0.11 and PostgreSQL 18 on 2026-08-20.

## Licence

This repository is dual-licensed:

- **Code** — `src/`, `tests/`, `tools/`, and all other source — is licensed
  under the [MIT License](LICENSE).
- **Content** — guides, cheatsheets, and link lists — is licensed under
  [Creative Commons Attribution 4.0 International](LICENSE-CONTENT)
  (CC BY 4.0).
