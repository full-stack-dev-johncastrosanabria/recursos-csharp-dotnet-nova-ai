# Module 09 · SQL fluency

## Before you start

No prior module is required, which makes this a reasonable place to start if
you arrived for the database material. It helps to have module 04 for LINQ,
because the mistake this module is about has an exact LINQ twin — a predicate
the provider cannot translate, evaluated client-side — and module 11 will join
the two properly.

You need Docker only for the integration tier. Everything else runs against
plans captured from a real PostgreSQL 18 and committed to
`modules/09-sql-fluency/plans`, so the reasoning is available to everyone and
the verification is available to whoever can run a container. Be clear about
which is which: reading a captured plan teaches you to read plans, and only the
integration tier proves the rules hold against a live planner.

Budget four hours, and expect the second half to be the slow part. Reading a
plan is mechanical once somebody shows you the three numbers that matter.
Believing what it says about your own query, when the query looks obviously
fine, takes longer.

If you can already say why `WHERE lower(email) = ?` cannot use an index on
`email`, and what `loops=200` does to the row count printed next to it, skip to
`Challenge/` and start with `PlanReport`. Run
`dotnet test --project modules/09-sql-fluency/tests/UnitTests --filter-class "*PlanReportTests"`.
If the test asserting that a healthy plan produces no findings at all does not
strike you as the important one, read section 8 first.

## Objectives

By the end of this module you can:

- Say what a B-tree index physically is, and derive from that what it can and cannot answer.
- Look at a predicate and tell whether an index on that column could serve it.
- Read `EXPLAIN (ANALYZE, FORMAT JSON)` output as a tree and find the expensive node.
- Multiply by the loop count, and recognise an N+1 query in a plan.
- Use the planner's row estimates to explain a plan that otherwise makes no sense.
- Choose between rewriting a query, adding an index, and doing nothing.
- Run `EXPLAIN ANALYZE` safely, including against statements that write.

## Sections

### 1. An index is an ordered copy of a column

Start here, because almost everything else in this module is a consequence.

A B-tree index on `orders (customer_email)` is a sorted structure containing
**the values of that column**, each with a pointer back to its row. That is the
whole idea. Searching it is a descent through sorted values, which is why a
lookup is logarithmic rather than linear, and why the index can also satisfy
range queries and `ORDER BY` on the same column.

Two consequences fall straight out. The index can answer questions about
`customer_email`, because that is what it contains. And it cannot answer
questions about `lower(customer_email)`, because that value is nowhere in it —
there is no sorted list of lowercased emails to descend. PostgreSQL is not
ignoring your index or making a bad choice. It has been asked about a value the
index does not hold.

The same reasoning covers the costs people forget. An index is a second copy of
that column, maintained on every insert, update and delete that touches it. It
occupies disk and cache that the table would otherwise use. "Add an index" is
never free, which is why section 7 treats it as one option among several rather
than as the answer.

### 2. Sargability: ask about the column itself

Reach for this check before you reach for EXPLAIN. It costs a glance and it
explains most slow queries you will meet.

A predicate is **sargable** — Search ARGument ABLE — when it compares the bare
column to something. Wrap the column in anything and it stops being one:

| Predicate | Sargable | Why not |
|---|---|---|
| `customer_email = $1` | yes | asks about the column |
| `placed_at >= $1 AND placed_at < $2` | yes | a range over the column |
| `lower(customer_email) = $1` | no | a function's output is not in the index |
| `placed_at::date = $1` | no | a cast is a function |
| `total_cents % 4 = 1` | no | arithmetic is a function |
| `customer_email LIKE '%.com'` | no | no prefix to descend to |
| `status <> 'paid'` | no | an index locates values, not their absence |

Exercise 5 encodes that table as a rule, and the integration tier asserts every
row of it against a live server.

The last two deserve a sentence each. A leading wildcard has no prefix, so
there is no point in the sorted structure to start from; `LIKE 'User1%'` is
fine, because it does. And a negation matches most of the table, which brings
us to section 6.

### 3. Reading a plan

`EXPLAIN` shows the plan the planner chose. `EXPLAIN (ANALYZE)` runs the query
and adds what actually happened, which is the only version worth reading, with
the caution in section 8. `FORMAT JSON` makes it a tree you can parse, which is
exercise 1.

A plan is a tree executed from the leaves up: children produce rows, parents
consume them. Read it inside out. Three numbers on each node carry almost all
of the diagnostic value:

- **`Actual Rows`** — what the node produced.
- **`Rows Removed by Filter`** — what it looked at and threw away. This is the number nobody looks at, and it is where the truth lives. A scan returning 2 rows having removed 99,998 read the whole table.
- **`Actual Loops`** — how many times the node ran, which section 4 is about.

Scan types are the vocabulary. `Seq Scan` reads the whole relation. `Index Scan`
walks the index and fetches matching rows. `Index Only Scan` answers entirely
from the index without touching the table, which is the fastest thing here.
`Bitmap Heap Scan` — with a `Bitmap Index Scan` child — collects matches from
the index first and then visits the heap in physical order, which is what
PostgreSQL chooses when there are too many matches for one-by-one fetches but
not enough to justify reading everything.

```bash
dotnet run --project modules/09-sql-fluency/examples/WholeTableForFourRows
```

### 4. Per loop, not total

This one costs people whole afternoons, so it gets its own section.

`Actual Rows` and `Actual Total Time` are reported **per loop, averaged across
loops**. A node showing `rows=4 loops=200` produced 800 rows, and its time is
what one of those two hundred passes cost. Multiply before you conclude
anything.

The shape this matters most for has a name outside SQL: the N+1 query. In a
plan it looks like a small, fast, perfectly indexed node with a large loop
count — and the node is genuinely fine. It is just happening once per outer row.

```bash
dotnet run --project modules/09-sql-fluency/examples/PerLoopNumbers
```

Note what the fix is not. Making that inner lookup faster achieves nothing
worth having, because it is already fast; the cost is the repetition. The fix is
to stop doing it per row — a join, or one query with an `IN` list. Exercise 4 is
the multiplication, and it also covers parallel workers, which are reported as
loops for the same reason.

### 5. Estimates are the explanation

When a plan looks stupid, the plan is usually not the problem.

The planner chooses between a scan and an index, or between a hash join and a
nested loop, by **predicting how many rows each step will produce**. Those
predictions come from statistics kept on columns and refreshed by `ANALYZE`.
Given good numbers it makes good choices. Given a number that is wrong by two
orders of magnitude it makes a perfectly reasonable choice for a query that does
not exist.

So when a plan makes no sense, compare `Plan Rows` with `Actual Rows` before you
argue with the planner. Exercise 3 is that comparison.

And note where the bad estimate comes from in this module's own example:
`total_cents % 4 = 1` is estimated at 249 rows and returns 50,000. PostgreSQL
keeps statistics on **columns**, not on expressions over them, so wrapping a
column takes away the index and the statistics in one move — the same root
cause, twice.

```bash
dotnet run --project modules/09-sql-fluency/examples/PlannerBlindSpot
```

The reason this matters more than one slow node: an estimate is an input to
every decision above it. A step believed to return 249 rows is a fine candidate
for a nested loop on the other side of a join; at 50,000 it is a disaster, and
the node that looks wrong in the plan is not the one that lied.

### 6. Sargable does not mean the index will be used

Here is the half that most treatments leave out, and the exercises assert both
directions of it deliberately.

Sargability is **necessary, not sufficient**. It says the index *could* apply.
Whether the planner uses it is a separate question with a different answer, and
that answer is about **selectivity**: what fraction of the table matches.

Reading rows through an index means fetching them from the heap in index order,
which is effectively random access. Do that for 40,000 rows and you pay 40,000
scattered reads; reading the same rows sequentially is dramatically cheaper per
row. So there is a crossover, and past it a sequential scan is the *right*
choice. `placed_at >= '2025-01-01'` against a table that starts in 2025 is
perfectly sargable and will be scanned, correctly.

That is why "it's not using my index" is so often not a bug. Before you fight
it, ask what fraction of the table the predicate matches. If it is most of them,
the planner is right and the query is the thing to reconsider.

The direction that *is* absolute is the other one: an unsargable predicate can
never use a plain index on that column, whatever the selectivity, because the
value it asks about is not in there.

### 7. Choosing the remedy

Four options, and reaching for the same one every time is the mistake.

**Rewrite the predicate.** Always consider this first, because it costs nothing
to maintain. `placed_at::date = '2025-06-15'` becomes `placed_at >= '2025-06-15'
AND placed_at < '2025-06-16'`, which uses the index you already have. A date
cast is almost always a query bug rather than a missing index.

**Add a plain index.** When the predicate is already sargable and selective and
no index covers the column. Remember the write cost.

**Add an expression index.** `CREATE INDEX ON orders (lower(customer_email))`
makes the original query fast without changing it. Right when you cannot change
the query, or when the expression is genuinely how the data is addressed. It
costs the same as any index, on every write, forever.

**Do nothing.** When the table is small, when the query is rare, or when the
predicate matches most of the table anyway. An index that is never chosen is
pure cost.

Exercise 7 encodes those choices. Two things it deliberately does not cover:
partial indexes (`WHERE status = 'active'`), which are excellent when your
queries always carry the same filter, and multi-column indexes, where column
order decides which queries can use them — both worth reading about once you are
comfortable here.

### 8. What EXPLAIN costs, and what it does not tell you

One warning first, and it is the kind that only needs to bite once.
**`EXPLAIN ANALYZE` actually runs the statement.** On a `SELECT` that is merely
slow. On an `UPDATE`, a `DELETE` or an `INSERT` it does the thing. Wrap it:

```sql
BEGIN;
EXPLAIN (ANALYZE) DELETE FROM orders WHERE placed_at < '2020-01-01';
ROLLBACK;
```

Then the limits. `Execution Time` is one run on one machine with whatever
happened to be in cache — useful for orders of magnitude and worthless for
small differences, which is why every exercise here asserts on **rows** rather
than milliseconds. `Planning Time` is separate and matters for cheap queries
executed constantly. And a plan describes one execution with one set of
parameter values; a different value can produce a different plan, which is how a
query that is fine for every customer you tested is catastrophic for your
largest one.

`BUFFERS` is the option worth adding once you are comfortable: it reports blocks
read and whether they came from cache, which separates "this query is slow" from
"this query is slow when it misses cache". `VERBOSE` adds output columns and
schema-qualified names.

## Real-world case

**The mechanism.** A support tool looks up a customer's orders. Emails are
stored as the customer typed them and compared case-insensitively, which is
correct:

```sql
SELECT id, total_cents FROM orders WHERE lower(customer_email) = $1;
```

There is an index on `orders (customer_email)`. It cannot serve this query. The
index contains email addresses as stored; the predicate asks about their
lowercased forms, which are nowhere in it. PostgreSQL computes `lower()` for
every row in the table and compares.

The captured plan says exactly that: **200,000 rows examined to return 4**, and
the sargable form of the same lookup examines 4.

**The detection gap.** Nothing about this is visible in the code. The query is
correct, readable, and returns the right answer every time. It is fast in
development, where the table has a few thousand rows. It is fast at launch. It
has no error, no warning, and no log line, and — this is the part that hides it
— it never changes, so it is never a suspect. What changes is the table, one
insert at a time.

A sequential scan costs time proportional to the table. The indexed form costs
time proportional to the logarithm of it. So the two are indistinguishable at
launch and diverge forever afterwards, and the day someone finally profiles it,
the query looks exactly as it did on the day it was written.

**What it would cost you.** Every number here is an input; substitute your own.
Start from the measured figure: the captured plan takes **31 ms** at 200,000
rows. Take a table that grows to 10 million rows, which is 50 times larger, and
a support tool that runs this lookup 40 times a minute.

A sequential scan is linear, so 31 ms becomes roughly 31 × 50 ≈ **1.55 seconds**
— and that is optimistic, because at 10 million rows the table no longer fits in
cache and the reads stop being free. At 40 calls a minute, 40 × 1.55 = 62
seconds of work per 60 seconds of wall clock: one core saturated permanently by
a query that returns four rows. The indexed form is still reading four.

The cost worth counting is not the core. It is that this degrades smoothly, so
there is no incident to investigate — just a support tool that everyone agrees
has "got slower", a database that looks busy for no clear reason, and a
year-long argument about needing a bigger instance.

The repair is exercise 5's rule and exercise 7's decision, and there are two of
them: rewrite the query to compare the column, or index the expression.

```bash
dotnet run --project modules/09-sql-fluency/examples/WholeTableForFourRows
```

## Exercises

All eight live in `modules/09-sql-fluency`. Run the whole module with
`./run.sh test 09` — all 58 tests fail until you implement the stubs in
`src/Exercises`. Work one class at a time with `--filter-class`.

**Core**

1. **`PlanTree`** — parse `EXPLAIN (FORMAT JSON)` into a tree and walk it.
   `dotnet test --project modules/09-sql-fluency/tests/UnitTests --filter-class "*PlanTreeTests"`
2. **`ScanStrategy`** — how each relation was reached, and how many rows that cost.
   `dotnet test --project modules/09-sql-fluency/tests/UnitTests --filter-class "*ScanStrategyTests"`
3. **`EstimateAccuracy`** — find where the planner was wrong, which is usually the answer.
   `dotnet test --project modules/09-sql-fluency/tests/UnitTests --filter-class "*EstimateAccuracyTests"`
4. **`LoopAmplification`** — multiply by the loop count; find the N+1.
   `dotnet test --project modules/09-sql-fluency/tests/UnitTests --filter-class "*LoopAmplificationTests"`
5. **`Sargability`** — the real-world case as a rule you can apply by eye.
   `dotnet test --project modules/09-sql-fluency/tests/UnitTests --filter-class "*SargabilityTests"`

**Challenge**

6. **`PlanDiff`** — prove a change helped, by rows read rather than by the clock.
   `dotnet test --project modules/09-sql-fluency/tests/UnitTests --filter-class "*PlanDiffTests"`
7. **`IndexAdvice`** — turn a verdict into a decision, including "do nothing".
   `dotnet test --project modules/09-sql-fluency/tests/UnitTests --filter-class "*IndexAdviceTests"`
8. **`PlanReport`** — the capstone: read a plan the way you would in review.
   `dotnet test --project modules/09-sql-fluency/tests/UnitTests --filter-class "*PlanReportTests"`

There is also an integration tier: 13 tests that start a real PostgreSQL 18 in a
container, generate plans against it, and run your analyser over output it has
never seen. It needs Docker, and it is where the rules stop being claims.

```bash
dotnet test --project modules/09-sql-fluency/tests/IntegrationTests
```

Reference solutions are in `src/Solutions`. Read them after you have a passing
test of your own, not before.

## Summary

What should have changed is what you look at first when a query is slow. Not the
query's shape, not the server, and not the row count it returns — the predicate,
and whether it asks about a column or about something computed from one.

Four specifics worth keeping. An index is an ordered copy of a column, so it can
only answer questions about that column; wrapping it in a function, a cast or
arithmetic makes the index inapplicable rather than merely unused. The number
that tells you this happened is rows removed by filter, not rows returned.
`EXPLAIN` reports rows and time per loop, so a node with a large loop count is
doing far more than it appears. And when a plan makes no sense, the planner's
row estimate is usually the thing that is wrong — the same expression that cost
you the index also cost you the statistics.

One habit: when you write a `WHERE` clause, read the left-hand side of every
comparison and ask whether it is a bare column. That question takes a second and
catches the most expensive bug in this module before it exists.

## Review questions

Answer before expanding. If you reach for the compiler, that is the answer.

**1. Why can an index on `email` not serve `WHERE lower(email) = $1`?**

<details><summary>Answer</summary>
The index contains the values of `email`. The lowercased forms are not in it, so
there is nothing to search. The index is inapplicable, not ignored.
</details>

**2. A scan returned 2 rows. How much did it read?**

<details><summary>Answer</summary>
Unanswerable without `Rows Removed by Filter`, and with loops, the total is
(returned + removed) × loops. Two rows returned can mean two hundred thousand
read.
</details>

**3. A node reads `rows=4 loops=200`. How many rows did it produce?**

<details><summary>Answer</summary>
800. Rows and times are per loop, averaged. This is the N+1 query as it appears
in a plan: a small, fast, correctly indexed node happening far too often.
</details>

**4. The plan looks absurd. Where do you look before blaming the planner?**

<details><summary>Answer</summary>
At `Plan Rows` against `Actual Rows`. A choice that is wrong for the real data
was usually reasonable for the estimated data.
</details>

**5. Your predicate is sargable and PostgreSQL still scans. Is that a bug?**

<details><summary>Answer</summary>
Probably not. If the predicate matches a large fraction of the table, reading it
sequentially is cheaper than thousands of random heap fetches. Sargability is
necessary, not sufficient.
</details>

**6. When is an expression index the wrong fix for an unsargable predicate?**

<details><summary>Answer</summary>
When the predicate can simply be rewritten — a date cast becomes a half-open
range and uses the existing index. An expression index costs write time forever
to support a query that was avoidable.
</details>

**7. What is dangerous about `EXPLAIN ANALYZE`?**

<details><summary>Answer</summary>
It executes the statement. Against an `UPDATE` or `DELETE` it does the thing.
Wrap it in `BEGIN` … `ROLLBACK`.
</details>

**8. Why do these exercises assert on rows rather than milliseconds?**

<details><summary>Answer</summary>
Timings move with cache warmth, load and hardware. Rows read is the quantity
that scales with the table, and it is what will still be true next year.
</details>

If any answer above surprised you, this module is not finished with you yet.
Module 10 takes the same table into concurrent transactions, and module 11 puts
EF Core on top and asks which of these plans your LINQ actually produced.

## Resources

- [Using EXPLAIN](https://www.postgresql.org/docs/18/using-explain.html) — the official walkthrough, including how to read costs and why ANALYZE executes.
- [`EXPLAIN` reference](https://www.postgresql.org/docs/18/sql-explain.html) — every option, including `BUFFERS` and `FORMAT JSON`.
- [Row Estimation Examples](https://www.postgresql.org/docs/18/row-estimation-examples.html) — how the numbers in section 5 are actually derived.
- [Indexes on Expressions](https://www.postgresql.org/docs/18/indexes-expressional.html) — the exact fix for a function-wrapped predicate, and its cost.
- [Partial Indexes](https://www.postgresql.org/docs/18/indexes-partial.html) — the option section 7 deliberately left out; read it next.
- [Multicolumn Indexes](https://www.postgresql.org/docs/18/indexes-multicolumn.html) — why column order decides which queries an index can serve.
- [Statistics Used by the Planner](https://www.postgresql.org/docs/18/planner-stats.html) — what `ANALYZE` collects, and what it cannot collect about expressions.
