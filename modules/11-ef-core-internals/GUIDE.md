# Module 11 · EF Core internals

## Before you start

You need module 09. This module is about the gap between the LINQ you write and
the SQL that reaches the server, and you cannot judge the SQL without knowing
what a plan costs -- the module's sharpest moment is an idiomatic LINQ
expression that generates precisely the unsargable predicate module 09 showed
reading two hundred thousand rows to return four.

Module 04 helps for the same reason from the other side: it is about queries
that pass their tests against a list and hammer the database in production.
Module 06 explains why a `DbContext` is registered scoped, which section 8 turns
into a rule.

Budget three to four hours. Almost nothing here is difficult once you can see
it, and that is the whole problem: an ORM's job is to hide the database, it does
that job well, and the failures in this module are all consequences of it
succeeding. You will spend most of the time learning to ask what is actually
being sent rather than reasoning about what ought to be.

Most of the exercises need no database at all. `ToQueryString()` gives you the
SQL for a query without executing it, so the SDK-only tier is not a simulation
of EF Core -- it is EF Core, doing its real translation work, with the execution
step left off. The integration tier is where "would send" becomes "did send",
and where the round trips get counted.

If you can already explain why a re-query inside a context can return a stale
entity even though it issued SQL, skip to `Challenge/` and start with
`QueryReview`. Run
`dotnet test --project modules/11-ef-core-internals/tests/UnitTests --filter-class "*QueryReviewTests"`.
If the test asserting that a clean query produces no findings looks like the
trivial one, read section 5 first.

## Objectives

By the end of this module you can:

- Get the SQL for any LINQ query without running it, and read it.
- Choose between lazy, eager and projected loading, and say what each costs in round trips.
- Recognise an N+1 in code, in a plan, and in a command log.
- Predict when two `Include` calls become a cartesian product, and decide whether to split.
- Explain the identity map, and why a re-query can return a value the database no longer holds.
- Say what `SaveChanges` is about to do before calling it.
- Judge a query's translation: what crosses the boundary, and what it costs when it does.

## Sections

### 1. What sits between your LINQ and your database

Reach for this model whenever EF Core surprises you. There are four steps and
the surprises live in the gaps between them.

Your `IQueryable` is an **expression tree**, not code that has run. Calling
`Where` builds a description of a filter; nothing happens. When you enumerate --
`ToList`, `First`, `foreach`, `Count` -- EF **translates** that tree into SQL for
your provider's dialect, **executes** it, and then **materialises** the rows into
objects, tracking them as it goes.

Every failure in this module is one of those steps doing exactly what it should:

| Step | What it does well | What that costs |
|---|---|---|
| Build | Nothing runs until you enumerate | It is not obvious where enumeration happens |
| Translate | Refuses what it cannot express | It accepts things it *can* express but should not |
| Execute | One statement per enumeration | A loop enumerates once per iteration |
| Materialise | One object per key, always | That object may be older than the row |

Nothing on the right is a bug. They are the price of the thing on the left, and
knowing which one you are paying explains most of what follows.

### 2. Ask what it will send

This is the habit the module is really trying to install, and it costs nothing:

```csharp
var sql = db.Orders.Where(o => o.Total > 100).ToQueryString();
```

No database is contacted. The provider supplies the dialect, not a connection,
so this works in a unit test, in LINQPad, or in a debugger watch window. Every
question of the form "what does this LINQ actually do" has a mechanical answer
available at the moment you are writing it, rather than a guess confirmed in
production three months later.

Exercise 1 builds the small helper the rest of the module uses.

```bash
dotnet run --project modules/11-ef-core-internals/examples/WhatEfSends
```

One caution worth meeting now, because it will bite you the first time you paste
generated SQL into a console: EF prefixes a parameterised query with a
declaration comment, `-- @reference='ORD-1'`, on its own line. Collapse the
newlines and that comment swallows the entire statement.

### 3. Lazy, eager, explicit, projected

Four ways to get related data. The choice is about round trips, and it is not
close.

**Projection** should be your default. `Select` into a type carrying exactly the
fields the screen needs: one query, only the columns you named, and nothing
tracked. That is the right answer for every read-only screen, and most screens
are read-only.

**Eager loading** with `Include` is for when you genuinely need the entities --
you are going to change them and save. One query, whole rows, tracked.

**Explicit loading** (`Entry(order).Collection(o => o.Lines).Load()`) is for the
occasional case where you decide after the fact. It is at least honest: the load
is a statement you can see.

**Lazy loading** is the one to avoid, and it is the module's real-world case. It
turns a property access into a database query, which means the cost is invisible
at the call site by design. Section 4 is what that does.

### 4. The N+1, and why nobody sees it

Reach for this section whenever a page is slow and the database says it is fine.

The shape: fetch a list, then touch a navigation property on each item. One
query for the list, then one per item. Two hundred orders is two hundred and one
round trips.

```bash
dotnet run --project modules/11-ef-core-internals/examples/TwoHundredQueries
```

What makes it survive review is that **every part of it looks correct**. The
query is a query. The loop is a loop. `order.Lines` is a property access and
reads like one. Nothing in the C# suggests I/O.

What makes it survive monitoring is more interesting. Each of those two hundred
queries is individually fast -- an indexed lookup returning three rows -- so
nothing appears in a slow-query log, and the database's own metrics are
unremarkable. The cost is not in any one statement; it is in the count. Section
7's detector exists because two hundred near-identical statements is not
something you notice by scrolling.

And it scales with the wrong thing. Not with data volume, but with **page size**
-- the number of rows the screen happens to show. A page that was fine at twenty
rows is ten times worse at two hundred, and nobody changed a query to make it so.

### 5. One query or several

`Include` two collections and you have not asked for orders plus lines plus
payments. You have asked for their **product**: a join of two independent
one-to-many relationships pairs every line with every payment, for every order.

| Shape | Single query | Split query |
|---|---|---|
| 100 orders, 20 lines, 10 payments | 20,000 rows | 3,100 rows |
| 100 orders, 40 lines, 10 payments | 40,000 rows | 5,100 rows |

EF discards the duplicates while materialising, so the object graph you get is
correct. Only the wire, the memory and the clock pay -- which is why this is
discovered as an out-of-memory error or a mysteriously slow endpoint rather than
as wrong data.

`AsSplitQuery()` sends one statement per collection instead. Note what you give
up, because it is real and often glossed over: the collections are no longer
read in a single consistent snapshot, so a concurrent write between the
statements can produce an object graph that never existed in the database. That
is a trade, not a free improvement. Module 10 is the vocabulary for deciding it.

Exercise 3 is both halves. Reach for split queries when a single query's row
count is genuinely a problem; reach for a projection when you did not need the
entities at all, which is more often.

### 6. The change tracker and the identity map

Every entity a tracking query returns goes into the context's change tracker,
along with the values it had on arrival. That bookkeeping is how `SaveChanges`
knows what to write, and exercise 5 and exercise 6 are about reading it -- what
is pending, and which columns are actually dirty -- before it runs.

The part that surprises people is the **identity map**. Within one context,
each primary key maps to exactly one object, forever. Query for the same row
twice and you get the same instance both times.

That guarantee is not incidental; it is what makes change tracking coherent. If
two objects represented the same row, `SaveChanges` would have to pick one.

Here is the consequence, and it is the module's second failure:

```text
this context loaded ORD-1                      total = 100.0
another context set it to 250 and committed
re-queried in the first context                total = 100.0
  same object as before?                       True
  SQL commands that re-query actually sent:    1
```

Read the last line twice. This is **not** a cache avoiding a round trip. The
query ran, the database returned 250, and EF Core discarded that row in favour
of the instance it was already tracking. You paid for the query and got the old
answer.

```bash
dotnet run --project modules/11-ef-core-internals/examples/StaleAfterSave
```

`AsNoTracking()` sees the current row, because nothing is tracked to conflict
with. `Entry(order).Reload()` refreshes the instance you are holding. Both are
fixes for the symptom; section 8 is the fix for the cause.

Worth knowing: `AsNoTracking` changes the SQL not at all. Tracking is entirely a
client-side decision, which is why "the database looks fine and the app is slow"
is such a common shape on read-heavy screens.

### 7. The translation boundary

Two things happen at the boundary, and only one of them is famous.

The famous one: some expressions cannot be translated, and EF Core **refuses
them** rather than silently fetching the table and filtering in memory. That
refusal is a feature -- it is module 04's client-side filter bug caught at the
boundary. `string.Equals(a, b, StringComparison.OrdinalIgnoreCase)` is a good
example, and the exercises assert that it throws.

The one that costs money: an expression can translate **perfectly** and still be
a performance bug.

```csharp
db.Orders.Where(o => o.Reference.ToUpper() == reference)
// WHERE upper(o."Reference") = @reference
```

That is exactly the predicate module 09 was about. The index on `Reference`
holds reference values; `upper(Reference)` is not among them, so the planner
reads the table. The integration tier asserts this against a live server: the
`ToUpper` form produces a sequential scan and the direct comparison uses the
index, on the same data, returning the same row.

Nothing warns you. The LINQ is idiomatic, the SQL is correct, and the query
degrades as the table grows. If you need case-insensitive matching, the answers
are the same as in module 09 -- store a normalised column, or index the
expression -- and neither of them is "write `ToUpper` and hope".

There is a nasty twist here. The CA1862 analyser will tell you to replace
`ToUpper() ==` with `string.Equals(..., StringComparison.OrdinalIgnoreCase)`.
For ordinary in-memory code that is good advice. Inside an expression tree it
replaces a working query with a runtime exception. Analysers do not know they
are looking at a query.

### 8. Context lifetime is the real fix

Almost everything in section 6 stops being a problem when the context is short.

A `DbContext` is a **unit of work**, not a repository and not a service. It is
cheap to create, it is not thread-safe, and its guarantees -- the identity map,
the change tracker -- are scoped to its lifetime. Keep it to one operation and
the identity map cannot go stale, because it never lives long enough to.

`AddDbContext` registers it scoped, which in a web application means one per
request. That default is correct, and module 06 explains what happens when you
overrule it: a singleton holding a scoped `DbContext` captures it for the life
of the process, which gives you one change tracker, growing forever, shared by
every request. That is the same bug as this module's stale read, one level up.

Three rules that follow, in order of how much they buy you:

- **One context per unit of work.** Not per application, not cached on a service, not held across a whole request that both writes and re-reads through another path.
- **`AsNoTracking` for reads you will not save.** It avoids the bookkeeping and the identity map at once. A projection does this for free.
- **`Reload` deliberately** when you are knowingly holding something stale, rather than hoping a re-query will refresh it.

Two more worth knowing once the basics are in place. `DbContextPool` reuses
context instances without reusing their state, for high-throughput cases where
allocation shows up in a profile. And `SaveChanges` runs in an implicit
transaction covering everything the tracker holds -- which is module 10's
subject, and the reason `SaveChanges` is a decision point rather than a
formality.

## Real-world case

**The mechanism.** An order list screen shows the most recent orders and, for
each, how many lines it has:

```csharp
foreach (var order in db.Orders.ToList())
{
    total += order.Lines.Count;
}
```

Lazy loading is on -- switched on years ago, by someone else, to make a
different screen work. `order.Lines` is therefore not a property access. It is a
query, issued on the spot, once per order.

The counted result, with two hundred orders:

| Strategy | SQL commands | Lines totalled |
|---|---|---|
| lazy loading in a loop | 201 | 400 |
| `Include` | 1 | 400 |
| projection | 1 | 400 |

The same screen also lets you edit an order. It saves through a second service
which shares the request's context, then re-reads to show the result -- and
shows the old value, because the context handed back the instance it was already
tracking. Both halves of this case come from the same design decision: a context
that lives longer than the operation, doing work that looks like it is not doing
work.

**The detection gap.** There is no slow query. Each of the two hundred lookups
is an indexed read of three rows, well under a millisecond, so nothing reaches a
slow-query log and the database's own dashboards are unremarkable. There is no
error. The screen is correct, just slow, and the stale read is correct-looking
too -- it shows a value that was true a moment ago.

It is also invisible in review. Every line reads as ordinary C#, and the
expensive one reads as the most ordinary of all.

**What it would cost you.** Every number here is an input, not a finding;
substitute your own. Take a round trip to a database in the same availability
zone at 1.2 ms including the query itself, a page showing 200 orders, 40 page
renders a minute at peak, and a connection pool of 20.

Per render: 201 × 1.2 = **241 ms** of round-trip time, against 1.2 ms for the
`Include` version. That is a screen that takes a quarter of a second longer than
it should for reasons no dashboard reports.

The number that actually breaks something is connection occupancy. Each render
holds a pooled connection for those 241 ms, so 40 renders a minute is 40 × 0.241
= **9.6 seconds of connection time per minute** -- comfortable against a pool of
20. Grow to 400 renders a minute and it is 96 seconds of connection time per
60 seconds of wall clock, which a pool of 20 absorbs only until it does not.
Then the failure arrives all at once, as pool timeouts on unrelated endpoints,
and it points at the database.

That displacement is the real cost. The symptom is a timeout somewhere else, the
database looks healthy because it is, and the cause is a `foreach` that nobody
has any reason to suspect.

```bash
dotnet run --project modules/11-ef-core-internals/examples/TwoHundredQueries
```

The repair is exercise 2 and it is one word: `Include`, or better, a projection.

## Exercises

All eight live in `modules/11-ef-core-internals`. Run the whole module with
`./run.sh test 11` -- all 49 tests fail until you implement the stubs in
`src/Exercises`. Work one class at a time with `--filter-class`.

**Core**

1. **`GeneratedSql`** -- read the SQL a query would send, without sending it.
   `dotnet test --project modules/11-ef-core-internals/tests/UnitTests --filter-class "*GeneratedSqlTests"`
2. **`LoadingStrategies`** -- Include, projection, and neither.
   `dotnet test --project modules/11-ef-core-internals/tests/UnitTests --filter-class "*LoadingStrategiesTests"`
3. **`SplitQueries`** -- when two Includes multiply instead of adding.
   `dotnet test --project modules/11-ef-core-internals/tests/UnitTests --filter-class "*SplitQueriesTests"`
4. **`TranslationBoundary`** -- what crosses, and what it costs when it does.
   `dotnet test --project modules/11-ef-core-internals/tests/UnitTests --filter-class "*TranslationBoundaryTests"`
5. **`TrackingBehaviour`** -- what the change tracker is holding, and why.
   `dotnet test --project modules/11-ef-core-internals/tests/UnitTests --filter-class "*TrackingBehaviourTests"`

**Challenge**

6. **`ChangeTrackerAudit`** -- ask the context what SaveChanges is about to do.
   `dotnet test --project modules/11-ef-core-internals/tests/UnitTests --filter-class "*ChangeTrackerAuditTests"`
7. **`NPlusOneDetector`** -- find the shape in a command log.
   `dotnet test --project modules/11-ef-core-internals/tests/UnitTests --filter-class "*NPlusOneDetectorTests"`
8. **`QueryReview`** -- the capstone: review a query the way you would in a pull request.
   `dotnet test --project modules/11-ef-core-internals/tests/UnitTests --filter-class "*QueryReviewTests"`

There is also an integration tier: 14 tests that start a real PostgreSQL 18 in a
container and count what actually happens -- the round trips, the stale read,
and the plan the generated SQL produces. It needs Docker.

```bash
dotnet test --project modules/11-ef-core-internals/tests/IntegrationTests
```

Reference solutions are in `src/Solutions`. Read them after you have a passing
test of your own, not before.

## Summary

What should have changed is that you no longer read LINQ as code. You read it as
a description of a statement, and you ask what statement -- because the answer
is one method call away and every failure in this module comes from not asking.

Four specifics worth keeping. A navigation property under lazy loading is a
query, so a loop over N entities is N+1 round trips, none of them individually
slow enough to notice. Two collection `Include` calls in one statement return
the product of the collections, not their sum. Within a context, a key maps to
one object forever, so a re-query can execute, return the current row, and hand
you the old one anyway. And an expression that translates perfectly can still be
a performance bug -- `ToUpper()` becomes `upper(column)`, which is module 09's
unsargable predicate with a friendlier syntax.

One habit: before merging a query you have not seen the SQL for, print the SQL.
It takes one line, needs no database, and answers in seconds the question that
otherwise gets answered in production.

## Review questions

Answer before expanding. If you reach for the compiler, that is the answer.

**1. What does `ToQueryString()` need in order to work?**

<details><summary>Answer</summary>
A provider, for the dialect. Not a connection, not a server, and nothing is
executed -- which is why it belongs in unit tests.
</details>

**2. A loop touches `order.Lines` on 200 orders with lazy loading on. How many round trips?**

<details><summary>Answer</summary>
201. One for the orders, one per order. Every one of them is individually fast,
which is why nothing appears in a slow-query log.
</details>

**3. You `Include` two collections in one query. What comes back?**

<details><summary>Answer</summary>
Their product -- every row of one paired with every row of the other, per
parent. EF discards the duplicates on materialisation, so only the wire, the
memory and the clock pay.
</details>

**4. What do you give up by adding `AsSplitQuery()`?**

<details><summary>Answer</summary>
A single consistent read. The collections are now fetched by separate
statements, so a concurrent write between them can produce a graph that never
existed in the database.
</details>

**5. Another context updated a row and committed. You re-query in your context. What do you get?**

<details><summary>Answer</summary>
The instance you already had, with the old values. The query does execute and
the current row does come back -- EF discards it, because within a context a key
maps to one object.
</details>

**6. Two ways to see the current value, and which one is the real fix?**

<details><summary>Answer</summary>
`AsNoTracking()` for reads you will not save, and `Entry(x).Reload()` for an
entity you are holding. Neither is the real fix: shortening the context's
lifetime is.
</details>

**7. Does `AsNoTracking()` change the SQL?**

<details><summary>Answer</summary>
No. Tracking is entirely client-side; the statement on the wire is identical.
What changes is what the context does with the rows afterwards.
</details>

**8. `Where(o => o.Reference.ToUpper() == x)` translates cleanly. Why is it still a problem?**

<details><summary>Answer</summary>
It generates `upper("Reference") = @x`, and an index on `Reference` does not
contain upper-cased references. Module 09's unsargable predicate, reached from
idiomatic LINQ.
</details>

If any answer above surprised you, this module is not finished with you yet.
Module 12 puts a cache in front of these reads and asks what happens when it
expires, and module 15 revisits aggregates once you can see what loading one
actually costs.

## Resources

- [Efficient Querying](https://learn.microsoft.com/en-us/ef/core/performance/efficient-querying) -- projections, tracking and the N+1, from the team that wrote it.
- [Single vs. Split Queries](https://learn.microsoft.com/en-us/ef/core/querying/single-split-queries) -- the cartesian explosion and the consistency trade, in detail.
- [Change Tracking](https://learn.microsoft.com/en-us/ef/core/change-tracking/) -- the identity map, entity states, and what `SaveChanges` derives from them.
- [Tracking vs. No-Tracking Queries](https://learn.microsoft.com/en-us/ef/core/querying/tracking) -- including identity resolution, which is section 6 in one page.
- [DbContext Lifetime, Configuration, and Initialization](https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/) -- why scoped is the default and what pooling changes.
- [Client vs. Server Evaluation](https://learn.microsoft.com/en-us/ef/core/querying/client-eval) -- what EF refuses to translate, and why refusing is the right behaviour.
- [Logging, Events, and Diagnostics](https://learn.microsoft.com/en-us/ef/core/logging-events-diagnostics/) -- how to see the commands your application actually sends.
