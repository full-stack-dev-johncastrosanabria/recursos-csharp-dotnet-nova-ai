# Module 04 · LINQ and the modern language surface

## Before you start

You need module 01 for equality and module 02 for what keeps an object alive.
Module 03 is not required, but a query that closes over a `DbContext` and is
enumerated later is the same class of bug as a continuation that outlives its
scope, and it helps to have met one already.

Budget three to four hours. The exercises are short. The difficulty is that
several of them have two implementations that produce identical results, and
only one is correct — which is the module's actual subject.

If you can already explain why `AsEnumerable()` in the middle of a query is a
production incident, and say without looking what `DistinctBy` does that
`GroupBy().Select(g => g.First())` does not, skip to `Challenge/` and start with
`TranslatableQuery`. Run
`dotnet test --project modules/04-linq-and-the-modern-language-surface/tests/UnitTests --filter-class "*TranslatableQueryTests"`.
If the "never falls back to client evaluation" case does not make immediate
sense, read section 4 before continuing.

## Objectives

By the end of this module you can:

- Say what a LINQ query object actually holds, and when the work happens.
- Spot multiple enumeration and explain what it costs in each context.
- Tell `IEnumerable` and `IQueryable` apart by where the code runs.
- Keep a filter translatable, and prove it without a database.
- Write a lazy operator that validates its arguments eagerly.
- Use pattern matching and records where they replace real code, not as syntax.

## Sections

### 1. A query is a recipe, not a result

Assume deferred execution whenever you see a LINQ operator that returns a
sequence. `Where`, `Select`, `OrderBy`, `Take` and friends build a description
of work. None of them touch the source. The work happens when something
enumerates: a `foreach`, a `ToArray`, a `Count`, an `await foreach`.

Two things follow that surprise people, and both are in
`examples/DeferredCapture`.

A query closes over *variables*, not their values. Build a query with
`region = "EU"`, change `region` to `"US"`, enumerate, and you get US orders.
The lambda runs at enumeration, so it reads the variable then. This is correct
and documented, and it still catches everyone, because the code reads as though
the filter was decided when it was written.

A query returned from a method is a promise to do work later, in the caller's
context. If it closed over something that has since changed — or a `DbContext`
that has since been disposed — the failure surfaces at the caller's
enumeration, with a stack trace pointing nowhere near the cause.

The habit is not "materialise everything defensively". Deferral is what lets
`Where`, `OrderBy` and `Skip` compose into one database round trip instead of
three passes over intermediate lists. Materialise *deliberately*, at the point
where you want the answer fixed, and leave it deferred everywhere else.

### 2. Walk the source once

Apply this rule inside any method that accepts `IEnumerable<T>`: enumerate it
once. Not because multiple enumeration is always expensive, but because you
cannot see from the parameter type whether it is.

The natural implementation of a summary — `Any()`, then `Count()`, then `Sum()`,
then `Max()` — walks the sequence four times. `examples/DoubleEnumeration`
measures it: four walks against one, for identical output.

What that costs depends entirely on what was passed in, and the method cannot
tell. Against a `List<T>` it is waste you will never notice. Against an EF Core
query it is four round trips. Against a network stream or a data reader the
second walk may throw, because the sequence is not replayable.

There is a worse failure than slowness. Because a query re-executes, two walks
can see two different states — a `Count()` from before an insert and a `Sum()`
from after it, printed side by side as one report. That total never existed.

So: fold it yourself, or call `ToArray()` once and say why. Exercise 1 asserts
the walk count, because no assertion about output can catch this.

### 3. `IEnumerable` and `IQueryable` are not variations of one thing

Choose by asking where you want the code to run. `IEnumerable<T>` holds
compiled delegates and runs them in your process. `IQueryable<T>` holds an
**expression tree** — the code as data — which a provider inspects and
translates into something else, usually SQL.

That is the whole difference, and it explains everything downstream. A provider
can read `o => o.Amount > 100` because it is a tree it can walk. It cannot read
a compiled `Func<Order, bool>`, because a delegate is opaque: there is nothing
to inspect, only something to call.

So when a provider meets something it cannot translate, it has exactly two
options — throw, or fetch everything and run the rest locally. EF Core made
client evaluation of `Where` an error precisely because the silent version cost
too many people too much. But the moment you write `.AsEnumerable()`,
`.ToList()`, or call a helper typed `Func<>`, you have asked for the fallback
explicitly, and no provider will warn you about a decision you made.

### 4. Keeping a query translatable

Check this whenever a method returns or accepts a query. The rule is simple to
state: everything you want the database to do must still be in the expression
tree when the provider sees it.

`examples/ClientSideFilter` shows why this cannot be caught by ordinary tests.
Two implementations of "paid orders over 100" — one that stays queryable, one
with an `AsEnumerable()` in the middle. Same results. Same rows read. In memory
there is no boundary to cross, so there is nothing to observe. The only thing
that differs is the tree the provider is handed: one contains
`Where(o => o.State == "Paid" && o.Amount > threshold)`, the other is an opaque
already-executed iterator.

Three rules keep you on the right side of it. Return `IQueryable<T>` from
repository methods that a caller may want to narrow further, so their `Where`
joins yours in one query rather than filtering a materialised list. Type shared
predicates as `Expression<Func<T, bool>>`, never `Func<T, bool>` — this is the
one that arrives disguised as good factoring, because extracting a rule into a
helper is exactly what you should be doing, and the wrong type silently undoes
it. And materialise at the edge, once, when you have finished composing.

Exercise 6 asserts on the expression tree by counting operators in it. That is
not exotic: it is a plain `ExpressionVisitor`, and it is the only check that
fails before production.

### 5. Grouping, aggregating, and reports that do not move

Use `GroupBy` when you need every item under each key, `ToDictionary` or
`ToLookup` when you need a lookup afterwards, and a plain fold when you need one
number and the source is large.

Two things go wrong here regularly.

`GroupBy` buffers. It cannot yield a group until it has seen the whole source,
because a matching key may still arrive. That is fine over a list and wrong over
a stream, and it is why exercise 7 exists — chunking *consecutive* runs can
stream, because the run ends the moment the key changes.

Sort order needs a tie-break. `OrderByDescending` on revenue is stable in
LINQ-to-Objects, but the order it preserves is grouping order, which nobody
promised and which a different provider will not reproduce. A report that
reshuffles equal rows between runs looks broken to whoever reads it, so name the
second key: `.ThenBy(r => r.Region, StringComparer.Ordinal)`.

While you are there, be explicit about string comparison generally. Grouping
customers by email without `StringComparer.OrdinalIgnoreCase` treats
`ana@example.com` and `ANA@example.com` as two people, and the second invoice is
how you find out. Exercise 4 is that bug.

`ToDictionary` and `ToLookup` are worth telling apart while we are here. Both
build a lookup in one pass, but `ToDictionary` throws on a duplicate key and
holds one value each, while `ToLookup` accepts duplicates and holds a group.
Reaching for `ToDictionary` on data you do not control is how a report becomes
an exception the first time the upstream system sends the same id twice — and
choosing between them is a statement about whether keys are unique, so make it
deliberately rather than by habit.

### 6. Writing an operator

Write one when you need behaviour the BCL does not have and you want it to
compose like everything else. `ChunkByKey` in exercise 7 is a real example:
grouping consecutive runs is genuinely useful and genuinely absent.

Two rules make an operator behave like a native one.

**Stay lazy.** Yield as you go. An implementation that buffers the source to
produce its output has thrown away the property that made a sequence worth
having. Taking one chunk from a million-row source should read a handful of
rows, and exercise 7 asserts exactly that.

**Validate eagerly.** This is the part that looks wrong until you have been bitten.
An iterator method runs *none* of its body until the first `MoveNext`. So an
`ArgumentNullException` written at the top of an iterator does not fire when the
method is called — it fires whenever somebody eventually enumerates, which may
be in a different method, in a different class, with a stack trace that does not
mention the caller who passed the bad argument. The fix is the standard shape:
a public non-iterator method that validates and then returns a private
iterator.

### 7. Pattern matching as control flow

Reach for a switch expression when a value maps to one of several outcomes and
the cases are about the *shape* of the value. Keep `if` when there is one
condition and one consequence — a switch with two arms is not clearer.

The thing worth internalising is that **arm order is behaviour**. Arms are tested
top to bottom, first match wins. In exercise 5, the "not paid" arm must come
first: move it below the amount checks and a large unpaid order matches the
cross-border rule and gets held for review instead of being rejected. A ladder
of `if`s with early returns has the same property and hides it across twenty
lines; a switch expression puts the precedence in reading order.

You get real checking with it. The compiler reports arms that can never match,
and warns when the cases are not exhaustive — a `switch` on an enum that gains a
member tells you at the next build. List patterns go further: `[var first, var
second]` binds two elements and the compiler knows they exist, which a `Count ==
2` check followed by indexing does not.

Property patterns compose with all of it: `{ State: not OrderState.Paid }`,
`{ Amount: >= 1000m }`, `{ Region: "EU", Amount: > 0 }`. Each reads as the
condition it is, and none of them needs a temporary variable.

### 8. Records, `with`, and collection expressions

Use a record when the type is defined by its data. Use `with` when you need a
changed copy. Use collection expressions when you are building a collection and
the target type is already known.

`with` is the one that changes how you write code rather than just how it looks.
`line with { UnitPrice = discounted }` produces a copy, leaving the original
untouched. Mutating in place rewrites data underneath every caller still holding
a reference to it — the failure mode from module 01, where two variables turn out
to name one object. Records make the safe version the short one.

Collection expressions — `[]`, `[a, b]`, `[.. existing, extra]` — are worth
adopting for a reason beyond brevity: the compiler picks the construction
strategy from the target type. Assigning to `IReadOnlyList<T>` does not force a
`List<T>`, and `[.. source]` can avoid an intermediate copy that
`source.ToList()` would make.

Primary constructors deserve a mention because they are easy to misread.
`class Foo(IService service)` puts the parameter in scope for the whole body,
which is genuinely less ceremony for dependency injection. But the parameter is
not a field you declared: it is captured only where it is used, it is mutable
unless you assign it to a `readonly` field yourself, and on a non-record class
it is not part of equality or `ToString`. Treat it as a constructor parameter
with a longer scope, not as shorthand for a property.

`required` members are the other addition worth adopting deliberately. Marking a
property `required` moves "you must set this" from a runtime null check into a
compile error at every construction site, which is strictly better than
validating in a constructor you then have to keep in sync. It composes with
object initialisers, so it gives you mandatory data without writing a
constructor at all — and unlike a nullable annotation, it is enforced rather
than advisory.

Two cautions. Type inference does not flow *out* of a collection expression, so
`Chunk(["EU"], r => r)` cannot infer its element type where `Chunk(new[] {
"EU" }, r => r)` can — the tests in this module hit that and say so. And a
record's generated equality is structural and shallow: a record holding an array
compares by reference, which is rarely what the shape of the type suggests.

## Real-world case

**The mechanism.** A repository exposes `IQueryable<Order> Orders`. A reporting
endpoint filters it for a date range and a status, and it is fast. Later someone
extracts the status rule into a shared helper so two endpoints can use it:

```csharp
public static Func<Order, bool> IsSettleable(string region)
    => o => o.State == OrderState.Paid && o.Region == region;
```

That is a good instinct, well executed, and it is the bug. `Where` on an
`IQueryable` has two overloads: one taking `Expression<Func<T, bool>>`, which
the provider translates, and one taking `Func<T, bool>`, which it cannot. Passing
a compiled delegate silently selects the `IEnumerable` overload. Everything after
it runs in your process, over every row the table will hand you.

The same failure arrives through `.AsEnumerable()` or `.ToList()` placed
mid-query to "make the types work". `examples/ClientSideFilter` prints the two
expression trees side by side: one carries the filter, the other is an opaque
iterator with nothing left to translate.

**The detection gap.** Nothing fails, and the tests are actively misleading. The
repository is tested against an in-memory list, where both versions produce
identical results in identical time — there is no boundary, so there is nothing
to observe. Code review sees a helper being reused, which is what review rewards.
The endpoint returns correct data.

Then the table grows. There is no deploy to correlate with, because the change
that broke it shipped weeks earlier and was harmless at the time. The symptom is
a slow endpoint and a database working hard on a query nobody wrote; the query
log shows an unfiltered select the codebase does not contain. Most teams find it
by reading the query log, not the code.

**What it would cost you.** Every number here is an input, not a finding;
substitute your own. Take an orders table of 4 million rows at roughly 400 bytes
each, a report endpoint hit 200 times an hour, and a filter that should have
returned about 500 rows.

Server-side, each call moves 500 × 400 B ≈ 200 KB. Client-side it moves the
whole table: 4M × 400 B ≈ 1.6 GB per call. At 200 calls an hour that is 320 GB
an hour crossing the wire, against 40 MB — a factor of 8,000. Long before the
bandwidth bill arrives, the database is saturated by repeated full scans and
every other query on that instance slows down with it.

The interesting part of that arithmetic is not the total. It is that the factor
is set by table size, so the cost of the mistake grows on its own, without
anybody touching the code.

Run it rather than believing it:

```bash
dotnet run --project modules/04-linq-and-the-modern-language-surface/examples/ClientSideFilter
```

The repair is exercise 6: type the rule as an `Expression`, and assert on the
tree so the next one fails in CI instead of in production.

## Exercises

All eight live in `modules/04-linq-and-the-modern-language-surface`. Run the
whole module with `./run.sh test 04` — all 52 tests fail until you implement the
stubs in `src/Exercises`. Work one class at a time with `--filter-class`.

**Core**

1. **`OrderQueries`** — deferred execution, and summarising in one walk.
   `dotnet test --project modules/04-linq-and-the-modern-language-surface/tests/UnitTests --filter-class "*OrderQueriesTests"`
2. **`LedgerAggregates`** — grouping, and a sort order that does not move.
   `dotnet test --project modules/04-linq-and-the-modern-language-surface/tests/UnitTests --filter-class "*LedgerAggregatesTests"`
3. **`PagedResults`** — a total and a slice from a single walk.
   `dotnet test --project modules/04-linq-and-the-modern-language-surface/tests/UnitTests --filter-class "*PagedResultsTests"`
4. **`CustomerDeduplication`** — comparers, and staying lazy while deduplicating.
   `dotnet test --project modules/04-linq-and-the-modern-language-surface/tests/UnitTests --filter-class "*CustomerDeduplicationTests"`
5. **`SettlementRules`** — a switch expression where arm order is the logic.
   `dotnet test --project modules/04-linq-and-the-modern-language-surface/tests/UnitTests --filter-class "*SettlementRulesTests"`

**Challenge**

6. **`TranslatableQuery`** — the real-world case, asserted on the expression tree.
   `dotnet test --project modules/04-linq-and-the-modern-language-surface/tests/UnitTests --filter-class "*TranslatableQueryTests"`
7. **`ChunkByKey`** — a real operator: lazy body, eager validation.
   `dotnet test --project modules/04-linq-and-the-modern-language-surface/tests/UnitTests --filter-class "*ChunkByKeyTests"`
8. **`InvoiceProjection`** — list patterns and `with`.
   `dotnet test --project modules/04-linq-and-the-modern-language-surface/tests/UnitTests --filter-class "*InvoiceProjectionTests"`

Reference solutions are in `src/Solutions`. Read them after you have a passing
test of your own, not before.

## Summary

What should have changed is your default suspicion. LINQ's ergonomics are so
good that the expensive version and the cheap version look the same, read the
same, and return the same results. Nearly every problem in this module is
invisible at the call site and invisible in the output.

Three specifics worth keeping. A query is a recipe: it holds variables rather
than values, and it re-runs every time you enumerate it, so two walks can
describe two different moments. `IQueryable` is code-as-data, and it stays that
way only while everything in the chain remains inspectable — one `Func<>` or one
`AsEnumerable()` ends it silently. And an iterator validates nothing until
somebody enumerates, so argument checks belong outside the iterator.

One habit: when a method takes `IEnumerable<T>`, decide explicitly whether you
are walking it once or materialising it, and make that visible in the code.
"It's just a list" is an assumption about a caller you have not met.

## Review questions

Answer before expanding. If you reach for the compiler, that is the answer.

**1. You build a query, change a captured variable, then enumerate. Which value is used?**

<details><summary>Answer</summary>
The new one. The lambda runs at enumeration and reads the variable then. The
query closed over the variable, not its value.
</details>

**2. A method takes `IEnumerable<T>` and calls `Any()`, `Count()` and `Sum()`. What is the cost?**

<details><summary>Answer</summary>
Three walks. Over a list, waste; over an EF query, three round trips; over a
stream, possibly an exception. And the three can see three different states.
</details>

**3. Why can a provider translate `Expression<Func<T,bool>>` but not `Func<T,bool>`?**

<details><summary>Answer</summary>
The expression is a tree it can inspect. A delegate is compiled code — opaque,
nothing to read, only something to call.
</details>

**4. Your repository helper returns `Func<Order,bool>` and everything still works. What changed?**

<details><summary>Answer</summary>
`Where` bound to the `IEnumerable` overload, so the filter now runs in your
process over every row the table returns. Results are identical; the query is not.
</details>

**5. Why must `GroupBy` buffer, and what can chunk consecutive runs instead?**

<details><summary>Answer</summary>
A matching key may appear at the end, so no group is complete until the source
is exhausted. Consecutive chunking knows a run ended when the key changes, so it
streams.
</details>

**6. You put `ArgumentNullException.ThrowIfNull` at the top of an iterator method. When does it fire?**

<details><summary>Answer</summary>
At the first `MoveNext`, not at the call — possibly in a different method
entirely. Validate in a non-iterator wrapper that returns the iterator.
</details>

**7. In a switch expression, does arm order matter?**

<details><summary>Answer</summary>
Yes. First match wins, so precedence is reading order. Moving a broad arm above
a narrow one silently changes behaviour.
</details>

**8. `OrderByDescending(r => r.Revenue)` with two regions tied. What decides the order?**

<details><summary>Answer</summary>
Whatever order grouping produced, which nobody promised. Add an explicit
tie-break or the report reshuffles between runs.
</details>

If any answer above surprised you, this module is not finished with you yet.
Modules 09 and 11 assume all of it, and they assume it silently.

## Resources

- [Language Integrated Query (LINQ)](https://learn.microsoft.com/en-us/dotnet/csharp/linq/) — the overview, including the deferred-execution model in section 1.
- [Deferred execution and lazy evaluation](https://learn.microsoft.com/en-us/dotnet/standard/linq/deferred-execution-lazy-evaluation) — the distinction between deferred and lazy, which are not the same thing.
- [`IQueryable<T>`](https://learn.microsoft.com/en-us/dotnet/api/system.linq.iqueryable-1) — read the Remarks on expression trees; that is where section 3 comes from.
- [Client vs. server evaluation](https://learn.microsoft.com/en-us/ef/core/querying/client-eval) — EF Core's own account of the real-world case, including why it made client `Where` an error.
- [Expression trees](https://learn.microsoft.com/en-us/dotnet/csharp/advanced-topics/expression-trees/) — code as data, and `ExpressionVisitor`, which exercise 6 uses.
- [Pattern matching](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/functional/pattern-matching) — every pattern form in section 7, with the precedence rules stated.
- [Collection expressions](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/collection-expressions) — including the spread element and how the target type picks the construction.
