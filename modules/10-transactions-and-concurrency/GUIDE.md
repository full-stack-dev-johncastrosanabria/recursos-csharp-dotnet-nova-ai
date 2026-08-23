# Module 10 · Transactions and concurrency

## Before you start

You need module 09 for the vocabulary — tables, indexes, and the habit of
asking the database what it actually did rather than assuming. Module 03 helps
because everything here races, and module 08's section on retries is the
outbound twin of this module's section 6.

Docker is needed for the integration tier and for one of the two examples. That
is not a gap in the SDK-only material, it is the honest shape of the subject:
module 09 could ship captured query plans because a plan is a document, and
there is no way to save a race. So the SDK-only exercises cover the machinery
you write — the retry loop, the version check, the transaction wrapper, the
lock ordering — and every claim about what a database actually does under
contention is asserted in the integration tier, live.

Budget four hours. The concepts are few and the vocabulary is standard. What
takes the time is that the broken version of every pattern here works perfectly
on your machine, in your tests, and in staging, and only fails when two people
do the same thing at the same moment.

If you can already say why READ COMMITTED does not prevent a lost update, and
what an `UPDATE` affecting zero rows means, skip to `Challenge/` and start with
`ReservationService`. Run
`dotnet test --project modules/10-transactions-and-concurrency/tests/UnitTests --filter-class "*ReservationServiceTests"`.
If the test asserting that an empty shelf is answered immediately rather than
retried does not strike you as obviously right, read section 6 first.

## Objectives

By the end of this module you can:

- Say what each isolation level promises, and what it conspicuously does not.
- Reproduce a lost update, and explain why nothing failed while it happened.
- Choose between an atomic statement, a lock, and a version, and justify the choice.
- Detect a concurrency conflict from a row count rather than an exception.
- Write a retry loop that retries the right errors and backs off with jitter.
- Prevent deadlocks by ordering rather than surviving them by retrying.
- Wrap work in a transaction that commits, rolls back and disposes correctly.
- Make a retry safe for an operation that is not naturally idempotent.

## Sections

### 1. What a transaction actually promises

Reach for a transaction whenever two writes must both happen or neither. Do not
reach for one as a general-purpose wrapper around unrelated work: a transaction
holds resources for its whole life, and section 8 is about what that costs.

ACID is four letters and, in practice, one of them does the work. Atomicity,
consistency and durability are mostly the database's problem and mostly
invisible: your writes land together or not at all, constraints hold, and a
commit survives a crash. **Isolation** is the one you have to make decisions
about, because it is the only one with a dial on it — and the default position
of that dial permits more than most people think.

The useful mental model for PostgreSQL is that isolation is implemented with
**snapshots**, not with read locks. A transaction sees a consistent picture of
the database as of some moment, and readers never block writers. That is why
PostgreSQL cannot do a dirty read even when you ask for one, and it is why the
anomalies you do get are about *time* rather than about seeing garbage.

### 2. The isolation levels, and what they do not promise

The standard defines four levels by what they permit:

| Level | Dirty read | Non-repeatable read | Phantom |
|---|---|---|---|
| READ UNCOMMITTED | yes | yes | yes |
| READ COMMITTED | no | yes | yes |
| REPEATABLE READ | no | no | yes |
| SERIALIZABLE | no | no | no |

Exercise 3 encodes that table, and two things about it matter more than
memorising it.

The first is that **the level you ask for is not always the level you get**.
PostgreSQL implements three distinct levels, not four: ask for READ UNCOMMITTED
and you get READ COMMITTED behaviour, because a snapshot cannot contain
uncommitted data. It does not reject the request, and — the part worth knowing —
`SHOW transaction_isolation` cheerfully reports "read uncommitted" back at you.
You cannot detect this by asking. Only behaviour reveals it, which is what the
integration tier asserts.

The second is what is missing from that table. It says nothing about the
**lost update**, which is the anomaly this module is named for, and READ
COMMITTED permits it enthusiastically. Read the table as a list of things the
level protects you from, not as a description of everything that can go wrong.

### 3. The lost update

Here is the whole bug:

```sql
SELECT quantity FROM stock WHERE sku = 'W';   -- 10
-- ... your code decides 10 > 0, so the sale can go ahead
UPDATE stock SET quantity = 9 WHERE sku = 'W';
```

Two transactions do that at once. Both read 10. Both write 9. Two units were
sold and one unit was deducted. Nothing failed, nothing was rolled back, and
READ COMMITTED did exactly what it promises — it promises you will not read
uncommitted data, and it says nothing at all about the row being unchanged
since you read it.

Run it:

```bash
dotnet run --project modules/10-transactions-and-concurrency/examples/OversoldStock
```

Ten buyers against ten units gives **ten sales and nine units left**: nine
units sold that never existed. The gap between the `SELECT` and the `UPDATE` is
the whole vulnerability, and every real handler has one — it is where you
validate, price, check a limit, or call a payment gateway.

### 4. Four ways to prevent it

In the order to reach for them.

**Make it one statement.** `UPDATE stock SET quantity = quantity - 1 WHERE sku =
$1 AND quantity > 0` has no window, because the read and the write are the same
operation and the row is locked for its duration. The row count tells you
whether it applied. This is the best answer whenever the decision can be
expressed as a predicate, and it is under-used because it looks too simple.

**Lock the row.** `SELECT ... FOR UPDATE` takes a write lock at read time and
holds it until you commit, so the read-then-write shape stays and the window
closes. Correct, and it serialises every buyer behind one lock — which is fine
for a rare row and a bottleneck for a hot one.

**Raise the isolation level.** REPEATABLE READ makes PostgreSQL detect the
conflict and abort the loser with SQLSTATE 40001. Nothing is oversold. Note
what the example shows, though: ten buyers become one sale and nine aborted
transactions. That is only useful with the retry loop from section 6 around it,
and without one you have converted silent corruption into loud failure — better,
but not yet good.

**Carry a version.** Covered next, and the right answer when the window is long
or spans a user's thinking time, where holding a lock is not an option.

### 5. Optimistic concurrency: detect rather than prevent

Reach for a version when the gap between read and write is long — a user editing
a form, a workflow spanning requests — or when contention is genuinely rare and
you do not want to pay for locks that are almost never contended.

The mechanism is to put the version in the `WHERE` clause:

```sql
UPDATE stock SET quantity = $1, version = version + 1
 WHERE sku = $2 AND version = $3
```

If somebody committed since you read, the version has moved, the predicate
matches nothing, and **zero rows are affected**. Exercise 4 is that check, and
the thing to internalise is that this raises no exception. A conflict is a
number, not an error, so code that ignores the row count from an `UPDATE` has an
optimistic concurrency scheme that detects precisely nothing. That is the single
most common way this is got wrong, and it fails silently by design.

Detection alone is not a strategy. One attempt against ten buyers reserves one
unit and tells nine people they lost a race they did not know they had entered.
Exercise 8 puts the retry loop around it.

And know where this stops working. The integration tier races ten buyers at one
row and shows optimistic concurrency **starving**: nothing is oversold, and most
buyers exhaust their attempts and give up. On a row that hot, versions are the
wrong tool and the atomic statement from section 4 is the right one. Optimistic
concurrency assumes conflicts are the exception; when they are the rule, it
degrades into a lottery.

### 6. Retrying, and why jitter is not decoration

A database that raises 40001 is not misbehaving. It is refusing to give you a
wrong answer and expecting you to come back, and that bargain only works if you
actually come back. Every write path above READ COMMITTED needs a retry loop.

**Retry the right things.** PostgreSQL's SQLSTATEs sort themselves neatly by
class: 40 is transaction rollback and means try again (40001 serialization
failure, 40P01 deadlock); 23 is an integrity constraint and means the data
really is like that (23505 duplicate key); everything else is a bug in the
statement or the schema. Exercise 1 is that classification, and the integration
tier provokes all three live to prove the codes are what this claims.

Retrying a 23505 gives you four identical errors and a slower failure.
Retrying something you do not recognise is how one problem becomes a thousand,
so unknown codes are treated as fatal on purpose.

**Back off with jitter.** Transactions that collided are by definition doing the
same thing at the same moment. Back them all off by the same amount and they
wake at the same moment and collide again — the retry loop is not recovering
from the pile-up, it is rebuilding it on a timer.

```bash
dotnet run --project modules/10-transactions-and-concurrency/examples/BackoffJitter
```

Twenty-four clients with a fixed backoff produce five distinct wake-up instants
and a worst-case pile-up of all twenty-four. The same clients with jitter
between half and one and a half times the delay produce over a hundred instants
and a worst case of three.

**Know when not to retry at all.** Exercise 8 answers an out-of-stock shelf
immediately: waiting will not refill it, and retrying turns a fast "no" into a
slow one. Retry contention, not answers you dislike.

### 7. Deadlocks

A deadlock needs a cycle: one transaction holds A and wants B while another
holds B and wants A. PostgreSQL detects that after a timeout and kills one of
them with 40P01, so with section 6's retry loop a deadlock is survivable.

It is still worth preventing, for two reasons. Detection is not instant — the
default is a second of waiting first — and the victim is chosen for the
database's convenience rather than yours, so the transaction that dies may be
the expensive one.

The cure is free: **take locks in a consistent order**. A cycle cannot form if
everyone sorts the rows they are about to touch before touching them. Exercise 5
is that ordering, and one of its tests is the important one — ordering only
works if *everyone* does it, including the batch job nobody remembered. A single
writer taking locks in arrival order reintroduces the cycle for everybody.

### 8. What an open transaction costs

A transaction is not free while it is open, and the costs are the ones that turn
a correctness fix into an availability problem.

It **holds its locks** until it ends, so anything conflicting waits. It **holds
a connection** from a pool that has a fixed size, which is how one slow
transaction becomes a queue of unrelated requests. And in PostgreSQL it **holds
back cleanup**: rows updated or deleted since the oldest running transaction
started cannot be vacuumed away, so one long-running transaction bloats tables
it never touched.

Two consequences worth stating plainly. Never do I/O inside a transaction that
you can do outside it — a call to a payment gateway inside an open transaction
holds row locks for the duration of somebody else's outage. And never let a
transaction span a user's thinking time; that is exactly what section 5's
version column exists for.

Exercise 6 is the wrapper that makes the mechanics hard to get wrong: commit on
success, roll back and rethrow on failure, dispose either way, and never let a
failing rollback replace the exception that explains why you were rolling back.

## Real-world case

**The mechanism.** A checkout handler reserves stock:

```csharp
var quantity = await ReadQuantityAsync(sku);
if (quantity <= 0) return SoldOut();
await SetQuantityAsync(sku, quantity - 1);
```

It is correct-looking, readable, and wrong. Between the read and the write there
is a window — validation, pricing, a call to the payment gateway — and every
other buyer in that window reads the same quantity and writes back the same
decrement. The last write wins and the others vanish.

Nothing is misconfigured. The default isolation level is READ COMMITTED, which
guarantees you will not read uncommitted data and guarantees nothing whatever
about the row being unchanged since you read it.

**The detection gap.** Every transaction commits successfully. There is no
error, no rollback, no log line, and no failed test — because a test that
exercises this needs two concurrent callers, and almost nobody writes one. The
stock count is not obviously wrong either; it goes down, just not enough.

The failure surfaces at **fulfilment**, days later and in another team's
system, as physical stock that does not match the ledger. By then the customer
has been charged and told their order is confirmed, which converts a database
bug into a refund, an apology, and a support conversation.

**What it would cost you.** Every number here is an input; substitute your own.
Take a 20-minute flash sale of 5,000 orders on one SKU — about 4.2 orders a
second — and a read-to-write window of 40 ms.

The expected number of other buyers arriving inside any one buyer's window is
4.2 × 0.04 ≈ 0.17, so roughly one order in six collides with another, and each
collision loses exactly one decrement. Over 5,000 orders that is about **830
units sold that do not exist**. At £9 per cancellation — payment reversal plus
one support contact — that is around £7,500, before anything is said about the
830 customers.

The number that is not in pounds is the delay. The bug is invisible in the
service that caused it and visible only in a warehouse days later, so the
feedback loop that would normally catch it is broken by design.

The repair is exercise 4 for the general case and one line of SQL for this one:
`UPDATE stock SET quantity = quantity - 1 WHERE sku = $1 AND quantity > 0`.

## Exercises

All eight live in `modules/10-transactions-and-concurrency`. Run the whole
module with `./run.sh test 10` — all 61 tests fail until you implement the stubs
in `src/Exercises`. Work one class at a time with `--filter-class`.

**Core**

1. **`RetryableErrors`** — tell "try again" apart from "this will never work".
   `dotnet test --project modules/10-transactions-and-concurrency/tests/UnitTests --filter-class "*RetryableErrorsTests"`
2. **`RetryPolicy`** — bounded retries, exponential backoff, and the jitter.
   `dotnet test --project modules/10-transactions-and-concurrency/tests/UnitTests --filter-class "*RetryPolicyTests"`
3. **`IsolationLevels`** — what each level permits, and what PostgreSQL really does.
   `dotnet test --project modules/10-transactions-and-concurrency/tests/UnitTests --filter-class "*IsolationLevelsTests"`
4. **`OptimisticConcurrency`** — a conflict is a row count, not an exception.
   `dotnet test --project modules/10-transactions-and-concurrency/tests/UnitTests --filter-class "*OptimisticConcurrencyTests"`
5. **`LockOrdering`** — make the cycle impossible instead of surviving it.
   `dotnet test --project modules/10-transactions-and-concurrency/tests/UnitTests --filter-class "*LockOrderingTests"`

**Challenge**

6. **`UnitOfWork`** — commit, roll back, dispose, and do not hide the real error.
   `dotnet test --project modules/10-transactions-and-concurrency/tests/UnitTests --filter-class "*UnitOfWorkTests"`
7. **`IdempotentOperations`** — make a retry safe for an operation that is not.
   `dotnet test --project modules/10-transactions-and-concurrency/tests/UnitTests --filter-class "*IdempotentOperationsTests"`
8. **`ReservationService`** — the capstone: detection plus retry, and when to stop.
   `dotnet test --project modules/10-transactions-and-concurrency/tests/UnitTests --filter-class "*ReservationServiceTests"`

There is also an integration tier: 15 tests that start a real PostgreSQL 18 and
race against it. It provokes 40001 and 40P01 for real, proves the isolation
claim in section 2 behaviourally, reproduces the oversell, and shows optimistic
concurrency starving on a hot row.

```bash
dotnet test --project modules/10-transactions-and-concurrency/tests/IntegrationTests
```

Reference solutions are in `src/Solutions`. Read them after you have a passing
test of your own, not before.

## Summary

What should have changed is what you picture when you read a `SELECT` followed
by an `UPDATE`. Not two steps, but a gap — and the gap is where another copy of
your own code is running.

Four specifics worth keeping. READ COMMITTED promises you will not read
uncommitted data and promises nothing about the row still being what you read,
so a read-modify-write under it loses updates silently and successfully. The
cheapest fix is usually to stop reading first: one statement with the decision
in its `WHERE` clause has no window. An optimistic version check reports a
conflict as zero rows affected, which is not an exception and is therefore easy
to ignore into uselessness. And a retry loop needs to know which errors deserve
one — class 40 yes, class 23 never — and needs jitter, because the transactions
that collided are the most perfectly synchronised group you will ever retry.

One habit: whenever you write a `SELECT` whose result decides a subsequent
`UPDATE`, ask what happens if that `UPDATE` runs twice at once. If the answer is
"the second one wins", you have found a lost update before it shipped.

## Review questions

Answer before expanding. If you reach for the compiler, that is the answer.

**1. Two transactions read 10 and both write 9. What went wrong, and what failed?**

<details><summary>Answer</summary>
A lost update. Nothing failed — both transactions committed successfully. READ
COMMITTED never promised the row would be unchanged between your read and your
write.
</details>

**2. You ask PostgreSQL for READ UNCOMMITTED. What do you get?**

<details><summary>Answer</summary>
READ COMMITTED behaviour, with no error and no correction — `SHOW
transaction_isolation` still reports "read uncommitted". A snapshot cannot
contain uncommitted data, so a dirty read is not something it can do.
</details>

**3. Your version-checked `UPDATE` affected zero rows. What happened?**

<details><summary>Answer</summary>
Somebody committed between your read and your write, so the version no longer
matched. Nothing threw. Ignoring the row count means detecting nothing.
</details>

**4. Which of 40001, 23505 and 42P01 should a retry loop retry?**

<details><summary>Answer</summary>
Only 40001. Class 40 is the server asking you to try again; 23505 is a real
duplicate that will duplicate again; 42P01 is a missing table, which retrying
will not create.
</details>

**5. Why does exponential backoff need jitter here in particular?**

<details><summary>Answer</summary>
Because a serialization failure means those transactions wanted the same row at
the same instant. Identical backoff wakes them together and reproduces the
collision — the retry loop rebuilds the pile-up on a timer.
</details>

**6. A deadlock is retryable. Why bother preventing it?**

<details><summary>Answer</summary>
Detection costs a wait — a second by default — and the victim is chosen for the
database's convenience, so the transaction that dies may be the expensive one.
Consistent lock ordering costs nothing.
</details>

**7. When is optimistic concurrency the wrong choice?**

<details><summary>Answer</summary>
On a hot row. It assumes conflicts are rare; when they are the norm, most
callers exhaust their retries and are turned away. Nothing is oversold and
almost nobody is served — use one atomic statement instead.
</details>

**8. Why should a payment gateway call not happen inside an open transaction?**

<details><summary>Answer</summary>
It holds row locks and a pooled connection for the duration of somebody else's
network call, and in PostgreSQL it holds back vacuuming too. Their outage
becomes your contention.
</details>

If any answer above surprised you, this module is not finished with you yet.
Module 11 puts EF Core over these same tables and asks which of these
guarantees survive an ORM, and module 17 asks the same questions of a message
broker, where the retry is somebody else's.

## Resources

- [Transaction Isolation](https://www.postgresql.org/docs/18/transaction-iso.html) — the levels as PostgreSQL implements them, including why it has three and not four.
- [Explicit Locking](https://www.postgresql.org/docs/18/explicit-locking.html) — `FOR UPDATE`, lock modes, and the deadlock section behind exercise 5.
- [PostgreSQL Error Codes](https://www.postgresql.org/docs/18/errcodes-appendix.html) — the full SQLSTATE table; class 40 and class 23 are the two to know.
- [Routine Vacuuming](https://www.postgresql.org/docs/18/routine-vacuuming.html) — why one long transaction bloats tables it never touched.
- [`System.Data.IsolationLevel`](https://learn.microsoft.com/en-us/dotnet/api/system.data.isolationlevel) — what the .NET enum means, and that asking is not the same as getting.
- [`NpgsqlTransaction`](https://www.npgsql.org/doc/api/Npgsql.NpgsqlTransaction.html) — how the driver maps .NET transactions onto PostgreSQL's, including which isolation levels it will actually send.
- [Testcontainers for .NET](https://dotnet.testcontainers.org/) — how the integration tier starts a real database per run.
