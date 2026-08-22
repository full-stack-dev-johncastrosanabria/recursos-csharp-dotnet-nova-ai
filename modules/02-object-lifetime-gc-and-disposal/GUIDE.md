# Module 02 · Object lifetime, GC and disposal

## Before you start

You need module 01, or at least a clear head about the difference between a
variable holding a value and a variable holding a reference. Everything here is
about references: which ones exist, what they keep alive, and who is
responsible for letting go.

Budget three to four hours. The exercises are shorter than module 01's but less
forgiving — several of them are about doing the same small thing twice without
it happening twice.

If you can already explain why a forced gen-2 collection can free nothing at
all, and say without looking what `leaveOpen` is for, skip to the `Challenge/`
exercises and start with `BufferPool`. Run
`dotnet test --project modules/02-object-lifetime-gc-and-disposal/tests/UnitTests --filter-class "*BufferPoolTests"`.
If the double-return case does not fall out of your design immediately, read
section 7 before continuing.

## Objectives

By the end of this module you can:

- State what makes an object collectable, and why "unused" is not it.
- Write a `Dispose` that is safe to call twice and refuses use afterwards.
- Choose between `IDisposable`, `IAsyncDisposable` and neither.
- Explain what a finalizer costs and why most types should not have one.
- Find the four reference shapes that leak in managed code.
- Decide who owns a resource, and express that decision in a signature.

## Sections

### 1. Reachability is the only question

Use this model whenever you are asking "why is this still in memory". Do not
reason about "when does the object go out of scope" — scope is a compiler
concept about names, and it is not what keeps an object alive.

An object is collectable when no chain of references reaches it from a root.
Roots are static fields, live local variables and arguments on any thread's
stack, CPU registers, and a few runtime-internal handles. That is the whole
rule. The collector does not know or care whether you still *want* an object,
whether it is "finished", or whether the code that created it returned long ago.

This is why the word "leak" misleads in managed code. A leak in C is memory you
can no longer reach and can no longer free. A leak in .NET is almost always the
opposite: memory you can still reach perfectly well, held by a reference you
forgot about. Nothing is broken. The collector is working exactly as specified
and freeing exactly what it should, which is nothing.

The practical consequence is that leak-hunting is reference-hunting. The
question is never "why was this not collected" — it is "what still points at
it". Modern debuggers answer that directly: take a dump, find the instance, ask
for its path to a root. That path names the bug, and it is very often in code
that has nothing to do with the object that is growing.

Run `dotnet run --project modules/02-object-lifetime-gc-and-disposal/examples/LeakingCache`
now. It fills a static dictionary, forces a full blocking collection, and shows
you the number not moving.

### 2. Generations, and the three numbers worth watching

Care about generations when you are diagnosing pauses or allocation pressure.
Do not tune them. The defaults are the product of far more measurement than you
are going to do, and almost every "GC tuning" problem is an allocation problem
wearing a disguise.

The heap is split by age. New objects start in gen 0, which is collected often
and cheaply. Survivors are promoted to gen 1, then gen 2. Large objects go
straight to a separate heap. The bet behind this is that most objects die young,
and it is usually right — which is precisely why an object that survives is
expensive: it gets promoted into a generation that is collected rarely, and
whatever it holds is promoted with it.

The three numbers that actually help. **Allocation rate**, because it sets how
often gen 0 runs. **Gen-2 collection count**, because gen 2 is the expensive
one, and a rising count under steady load means objects are being promoted that
should have died. **Live heap size after a full collection**, because that is
the only number immune to the collector's timing — it is what is genuinely
still reachable, and it is the number that exposes the leak in section 1.

What does not help is the process's total memory as reported by the operating
system. The runtime keeps memory it is not currently using, returns it lazily,
and the number moves for reasons unrelated to your code. Watching it produces
confident, wrong conclusions.

### 3. `IDisposable`, and the shape of a correct `Dispose`

Implement `IDisposable` when your type holds something that must be released
before the collector gets around to it: a file, a socket, a lock, a
subscription, a pooled buffer. Do not implement it because a type "feels heavy".
A type holding only ordinary managed objects needs nothing — the collector
handles those, and adding disposal to them spreads a burden through every
caller for no benefit.

A correct `Dispose` has three properties. It is **idempotent**: calling it twice
does the work once. It is **final**: every other public member throws
`ObjectDisposedException` afterwards rather than pretending. And it is
**complete**: it releases everything the object owns, including the things it
acquired late.

Idempotence is not a nicety. A caller often cannot tell whether disposal already
happened — a `using` may have run, an owner may have released early, an
exception path may have unwound. `ReservationLease` releases stock, and
releasing it twice oversells it, so the guard is the difference between correct
and not.

Throwing afterwards is the same argument. A disposed object that quietly accepts
work is worse than one that throws, because the caller carries on believing the
work happened. `ObjectDisposedException.ThrowIf(_disposed, this)` is one line
and it converts a silent wrong answer into a stack trace pointing at the caller.

### 4. `using`, and why scope is the right unit

Reach for `using` for anything disposable whose lifetime matches a scope, which
is nearly all of them. Reach for a field and explicit disposal when the resource
outlives any single method. Reach for neither only when something else owns it.

`using` is not sugar for "call Dispose at the end". It compiles to a `try` /
`finally`, so the resource is released whether the block completes, returns
early, or throws. That last case is the one hand-written cleanup gets wrong, and
it gets it wrong exactly when the system is already failing.

The declaration form — `using var lease = ...` — disposes at the end of the
enclosing scope rather than at a brace you choose. It reads better and it is
right most of the time, but when you need release to happen at a specific point,
the block form is not old-fashioned, it is more precise.

Order matters when a scope owns several resources. They are released in reverse
order of acquisition, because a resource taken later may depend on an earlier one
still being open. Nested `using` statements get this for free. A collection of
resources disposed with a naive `foreach` does not, and that is exercise 4:
`ShipmentBatch` must release in reverse, and one child throwing must not strand
the others.

### 5. Finalizers, and what they really cost

Write a finalizer only when your type directly owns an **unmanaged** resource —
a raw handle, native memory — and even then, prefer `SafeHandle`, which already
did this correctly. For a type holding managed resources, a finalizer is not a
safety net; it is a cost with no benefit.

A finalizer does not release anything at a time you can predict. When the
collector finds a finalizable object unreachable, it does not free it. It queues
it for a separate finalizer thread and the object survives that collection.
Only a later collection reclaims it. So every instance of a finalizable type
costs at least two collections instead of one, is promoted to an older
generation on the way, and drags everything it references along.

`examples/DisposeVersusFinalizer` shows the middle of that sequence: a full,
forced, blocking gen-2 collection runs and the object is still not finalized.
That is not a timing accident, it is the design.

There is also no ordering guarantee between finalizers, no guarantee one runs
before the process exits, and the thread it runs on is not yours — an exception
there is not yours to catch. Code that must run should not be in a finalizer.

When a type genuinely needs both, `Dispose` calls `GC.SuppressFinalize(this)` so
an object released properly does not also pay the finalizer's cost.

### 6. `IAsyncDisposable`, and why it is separate

Implement `IAsyncDisposable` when releasing genuinely requires awaiting: a flush
to a network stream, a final database round trip, draining a channel.
Implement plain `IDisposable` when it does not. Implement both only when a
caller may reasonably have either kind of scope.

The reason the interface exists is that there is no safe way to await inside
`Dispose`. Blocking on the task deadlocks under some synchronisation contexts
and starves the thread pool under others, and it does so under load rather than
in your tests. `await using` gives the release path a place to yield.

The subtle failure is not the awaiting, though — it is buffering. Anything that
batches owes its callers a flush on the way out. Every `WriteAsync` in
`LedgerWriter` returns successfully, so the caller believes each entry was
accepted. If disposal drops the buffer, entries below the flush threshold are
lost, no exception is raised anywhere, and the ledger is quietly short. That is
exercise 3, and the "disposing twice does not write them twice" case is the
same idempotence rule from section 3 applied to data instead of handles.

### 7. Ownership, and saying so in the signature

Decide ownership when a resource crosses a boundary, and make the decision
visible. Do not leave it implicit — a `Stream` parameter looks identical whether
the caller is handing the resource over or lending it, and the type system will
not help you.

Getting it wrong is costly in both directions. Dispose something you were only
lent and you break the caller who is still using it — and the exception surfaces
in *their* code, far from yours. Fail to dispose something you were given and it
leaks, silently, for as long as the process lives.

The convention is a `leaveOpen` flag, which is why `StreamWriter` has one. It
turns an assumption into a parameter the caller must answer. `ShipmentLabelWriter`
is exercise 8 and its whole content is respecting that answer.

Pooling is ownership with sharper edges, and `BufferPool` is exercise 7. Renting
a buffer transfers ownership temporarily; returning it transfers ownership back.
Return the same buffer twice and it is handed to two callers who both believe
they own it — the managed equivalent of a double free, and it produces the same
class of impossible bug. A pool must also clear what it takes back, or one
caller's data appears in another's buffer.

### 8. The four shapes that leak

Look for these whenever live heap grows under steady load. Each is a reference
that outlives the thing it points at, and none of them looks like a bug.

**Static collections.** A `static` field is a root, so anything it holds lives
for the process. A cache with no eviction is the canonical case and the one this
module reproduces. The fix is never "call the GC" — it is a capacity and an
eviction rule, which is exercise 2.

**Event handlers.** The publisher holds the handler, the handler holds the
subscriber. When the publisher is long-lived and the subscriber is not, every
missing `-=` is permanent. Worse, the leaking object is the subscriber while the
reference that roots it belongs to the publisher, so reading the subscriber's
code finds nothing. Store the delegate in a field: `+= p => ...` and
`-= p => ...` create two different delegate instances, so the second removes
nothing while reading exactly like it does. `examples/ForgottenSubscription`
shows a dropped subscriber still alive, and exercise 5 is the repair.

**Captured variables.** A lambda captures the whole closure, not just the
variable you used, and a long-lived delegate keeps all of it alive. Registering
a callback on something that outlives you is the same leak as an event.

**Timers and background work.** A running timer, a subscription, an unfinished
task — each is rooted by the runtime, not by your code. Cancel and dispose them
explicitly; nothing else will.

A weak reference helps with none of these directly, but it is the tool when a
lookup must not keep its entries alive. It has a catch, and it is exercise 6:
the entry holding the dead reference does not remove itself, so a weak index
still grows until something prunes it. A weak reference is a smaller leak, not
the absence of one.

## Real-world case

**The mechanism.** A checkout service caches order summaries by id in a static
dictionary, so that a repeat lookup within a request does not hit the database.
It is three lines, it is measurably faster, and it has no eviction. Nobody
decided the cache should be unbounded; nobody decided anything, which is the
same thing. Every distinct order id seen since the process started is still in
there, along with everything each summary references.

The collector cannot help. A `static` field is a root, so every entry is
reachable, and reachable is the only question the collector asks. It runs on
schedule, succeeds, and frees none of it — because none of it is garbage. The
service is doing exactly what it was told.

**The detection gap.** Nothing fails. Latency is flat, error rate is zero, and
the cache's hit ratio looks excellent, because a cache that never evicts has a
wonderful hit ratio. Memory climbs slowly enough that a week of graphs looks
like normal variance. The first real signal is a container hitting its memory
limit and being killed, which the platform reports as a restart rather than as
an error — and the restart *fixes* it, resetting the clock and destroying the
evidence. So it recurs, at a cadence set by traffic volume, and each occurrence
is investigated as an isolated event.

The tell, once you know to look, is the third row of the example: live heap
after a forced full collection, growing monotonically under steady load. That
single number distinguishes "the runtime is holding memory it will reuse" from
"something is accumulating", and almost nothing else does.

**What it would cost you.** Every number here is an input, not a finding;
substitute your own and the arithmetic is unchanged. Take 6 instances of the
service, each provisioned at 2 GB rather than the 512 MB the workload needs,
because raising the limit is what stopped the restarts. At a representative
cloud price of 4 per GB-month that is 6 × 1.5 GB × 4 = 36 a month in memory
nobody is using — real, but not the expensive part.

The expensive part is the restarts. At one OOM kill per instance per 10 days
across 6 instances, that is roughly 18 unplanned restarts a month, each
dropping the requests in flight and each generating an alert somebody answers.
At 30 minutes of engineer time per alert and a loaded cost of 80 an hour, that
is 9 hours and about 720 a month — spent repeatedly, on a problem that is never
diagnosed, because every investigation begins after the evidence has been
destroyed by the restart that ended the incident.

Run it rather than believing it:

```bash
dotnet run --project modules/02-object-lifetime-gc-and-disposal/examples/LeakingCache
```

The repair is exercise 2: a capacity and an eviction rule, roughly twenty lines.
The hard part was never the code.

## Exercises

All eight live in `modules/02-object-lifetime-gc-and-disposal`. Run the whole
module with `./run.sh test 02` — all 46 tests fail until you implement the stubs
in `src/Exercises`. Work one class at a time with `--filter-class`.

**Core**

1. **`ReservationLease`** — disposal that is idempotent and final.
   `dotnet test --project modules/02-object-lifetime-gc-and-disposal/tests/UnitTests --filter-class "*ReservationLeaseTests"`
2. **`BoundedOrderCache`** — the real-world case, as a repair.
   `dotnet test --project modules/02-object-lifetime-gc-and-disposal/tests/UnitTests --filter-class "*BoundedOrderCacheTests"`
3. **`LedgerWriter`** — `IAsyncDisposable`, and the flush a caller was promised.
   `dotnet test --project modules/02-object-lifetime-gc-and-disposal/tests/UnitTests --filter-class "*LedgerWriterTests"`
4. **`ShipmentBatch`** — reverse-order disposal where one failure must not strand the rest.
   `dotnet test --project modules/02-object-lifetime-gc-and-disposal/tests/UnitTests --filter-class "*ShipmentBatchTests"`
5. **`PriceFeedSubscription`** — the event-handler leak, and the `-=` that must match.
   `dotnet test --project modules/02-object-lifetime-gc-and-disposal/tests/UnitTests --filter-class "*PriceFeedSubscriptionTests"`

**Challenge**

6. **`WeakOrderIndex`** — weak references, and the bookkeeping they do not clean up.
   `dotnet test --project modules/02-object-lifetime-gc-and-disposal/tests/UnitTests --filter-class "*WeakOrderIndexTests"`
7. **`BufferPool`** — pooling, double-return, and clearing what comes back.
   `dotnet test --project modules/02-object-lifetime-gc-and-disposal/tests/UnitTests --filter-class "*BufferPoolTests"`
8. **`ShipmentLabelWriter`** — ownership, expressed as `leaveOpen`.
   `dotnet test --project modules/02-object-lifetime-gc-and-disposal/tests/UnitTests --filter-class "*ShipmentLabelWriterTests"`

Reference solutions are in `src/Solutions`. Read them after you have a passing
test of your own, not before.

## Summary

What should have changed is where you look. "Why was this not collected" has no
answer, because the collector always does the same correct thing. "What still
points at it" has exactly one answer, and it is the bug. Every leak in this
module is a reference somebody forgot, not a free somebody missed.

Three specifics worth keeping. Disposal is a contract with two halves — release
once, refuse afterwards — and a type that satisfies only the first turns a
double dispose into a double release, which for anything that reserves stock or
money is a real incident. A finalizer is not a cheaper `Dispose`; it costs every
instance an extra collection and cannot tell you when it will run. And ownership
is a decision somebody has to make explicitly, because a `Stream` parameter
carries no evidence of the answer.

One habit: when live heap after a full collection grows under steady load, stop
reading code and take a dump. The path from the instance to its root names the
bug in seconds, and it is usually somewhere you would not have read.

## Review questions

Answer before expanding. If you reach for the compiler, that is the answer.

**1. A forced gen-2 collection frees nothing. What have you learned?**

<details><summary>Answer</summary>
That everything on the heap is still reachable. The collector is working; some
reference you have not found is holding it all.
</details>

**2. Why is a `static` field special?**

<details><summary>Answer</summary>
It is a root. Anything reachable from it is alive for the life of the process,
regardless of whether anything still uses it.
</details>

**3. `feed.PriceChanged += p => Handle(p);` then `feed.PriceChanged -= p => Handle(p);`. What is unsubscribed?**

<details><summary>Answer</summary>
Nothing. Those are two different delegate instances, so the `-=` removes
nothing at all — and the code reads exactly as though it works. Store the
delegate in a field and use that field both times.
</details>

**4. Your type holds only a `List<T>` and a `string`. Should it implement `IDisposable`?**

<details><summary>Answer</summary>
No. Neither needs releasing; the collector handles them. Implementing it
imposes a burden on every caller and buys nothing.
</details>

**5. Why does a finalizable object cost at least two collections?**

<details><summary>Answer</summary>
When found unreachable it is queued for the finalizer thread rather than freed,
so it survives that collection and is promoted. A later collection reclaims it.
</details>

**6. What does `leaveOpen: true` mean, and who decides it?**

<details><summary>Answer</summary>
That the stream was lent, not given, so this object must not close it. The
caller decides, because only the caller knows whether it will keep using it.
</details>

**7. A weak reference lets the value be collected. What still leaks?**

<details><summary>Answer</summary>
The dictionary entry holding the now-empty weak reference. It does not remove
itself, so the index grows one small entry per key until something prunes it.
</details>

**8. Returning a pooled buffer twice — what actually goes wrong?**

<details><summary>Answer</summary>
It gets handed to two callers who both believe they own it, and they overwrite
each other. It is a double free with the symptoms of data corruption instead of
a crash.
</details>

If any answer above surprised you, this module is not finished with you yet.
Go back to the section it came from before starting the next one — every later
module assumes this one, and the assumptions are silent.

## Resources

- [Fundamentals of garbage collection](https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/fundamentals) — generations, roots and what triggers a collection, from the source.
- [Cleaning up unmanaged resources](https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/unmanaged) — when a finalizer is genuinely warranted, which is less often than you would guess.
- [Implement a Dispose method](https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-dispose) — the canonical shape, including the `SuppressFinalize` call and why it is there.
- [Implement a DisposeAsync method](https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-disposeasync) — the async counterpart and how the two interact.
- [`SafeHandle`](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.safehandle) — read this before writing a finalizer; it has almost certainly already solved your problem.
- [`WeakReference<T>`](https://learn.microsoft.com/en-us/dotnet/api/system.weakreference-1) — the API is small; the Remarks on when the target may vanish are the part that matters.
- [`ArrayPool<T>`](https://learn.microsoft.com/en-us/dotnet/api/system.buffers.arraypool-1) — the production version of exercise 7, with the same ownership rules and the same ways to get them wrong.
