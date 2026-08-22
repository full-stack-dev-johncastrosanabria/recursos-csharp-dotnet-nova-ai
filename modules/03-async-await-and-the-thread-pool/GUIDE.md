# Module 03 · Async, await and the thread pool

## Before you start

You need module 02, or at least the habit of asking what holds a reference to
what. Tasks and continuations are references, and a continuation that never runs
keeps everything it captured alive.

Budget four to five hours. This is an anchor module: modules 08, 12, 17, 19 and
22 all assume it, and the failures it prevents are the ones that only appear
under load, which means they appear in production first.

If you can already explain why `.Result` is worse than a slow call, and say
without looking what `TaskCreationOptions.RunContinuationsAsynchronously`
changes, skip to `Challenge/` and start with `CallbackBridge`. Run
`dotnet test --project modules/03-async-await-and-the-thread-pool/tests/UnitTests --filter-class "*CallbackBridgeTests"`.
If the "does not run the awaiter on the completing thread" case does not fall
out of your first attempt, read section 7 before continuing.

## Objectives

By the end of this module you can:

- Say what `await` does to a method, and what it does not do to a thread.
- Explain thread pool starvation and recognise it from its symptoms.
- Compose concurrent work without letting it become unbounded.
- Treat cancellation as a contract rather than a parameter you pass along.
- Bridge a callback API to a task without deadlocking its caller.
- Say where an async failure goes when nobody awaits it.

## Sections

### 1. What `await` actually does

Reach for this model whenever async code surprises you. Almost every surprise
comes from the same wrong assumption: that `await` starts a thread, or that it
waits.

`await` does neither. The compiler rewrites your method into a state machine.
When you await something incomplete, the method *returns* — right there, to its
caller — after registering the rest of itself as a continuation. When the
awaited operation completes, that continuation is scheduled, and the method
resumes from where it stopped, possibly on a different thread.

Two consequences follow, and they explain most of this module.

The first: an async method that never awaits anything incomplete runs entirely
synchronously. `await` on an already-completed task does not yield. This is why
a bug can hide until the day the cache misses and the call becomes slow.

The second: `async` is not `parallel`. Awaiting an I/O operation occupies no
thread at all while it is in flight — there is nothing to occupy, the work is
happening in the kernel or on another machine. That is what makes async worth
having on a server: not speed, but not holding a thread while waiting.

### 2. The thread pool, and why blocking is different from slow

Care about the pool whenever you write server code. The pool is a fixed-ish set
of worker threads shared by everything in the process. Requests, continuations,
timers and `Task.Run` bodies all draw from it.

It grows, but deliberately slowly. A pool that expanded instantly on demand
would thrash, so when it detects that no progress is being made it injects
threads gradually. That policy is correct for the case it was designed for — a
temporary burst — and pathological for the case in section 3.

Here is the distinction that matters. A **slow** operation that awaits holds no
thread; the pool is free to run other work, and throughput is unaffected. A
**blocking** operation holds a thread for its entire duration and the pool
cannot use it for anything. One slow call costs latency. One blocking call costs
a thread — and threads are the scarce resource.

Run `dotnet run --project modules/03-async-await-and-the-thread-pool/examples/StarvedPool`
now. Both runs do identical work; one needs roughly twenty extra operating
system threads to do it, and takes several times as long.

### 3. Async all the way, and the cost of `.Result`

Never call `.Result`, `.Wait()` or `.GetAwaiter().GetResult()` on a task in a
request path. There is no threshold below which it is fine, and the ways it
fails do not show up in testing.

The mechanism is a cycle. The blocking call occupies a pool thread and waits for
a task to complete. Completing that task requires running a continuation. The
continuation needs a pool thread. Enough concurrent blocking calls and every
thread is parked waiting for continuations that are queued behind the very
threads waiting for them. Progress stops until the pool injects more threads,
one slow batch at a time.

What makes this so expensive to diagnose is that the symptom is not local to
the cause. Requests that never touch the blocking path time out too, because
they are queued behind it, so the traces blame whichever endpoint happened to
be next in line. It is load-dependent, so it passes every test and every staging
run. And CPU sits near idle throughout, which rules out the first thing anyone
checks.

"Async all the way" is not stylistic advice. Awaiting is only cheap if everyone
in the chain awaits; one `.Result` anywhere in the path reintroduces the whole
problem. If a synchronous API is genuinely forced on you — an interface you do
not own, a constructor — the fix is to move the boundary, not to block at it.

The exception, and it is a narrow one: blocking in `Main`, in a console tool, or
in a test is harmless, because there is no pool of request threads to starve.

### 4. Composing work: `WhenAll`, and what it changes

Use `Task.WhenAll` when several operations are independent and you need all the
results. Use `Task.WhenAny` when you need the first. Use a plain loop with an
`await` inside it when each step genuinely depends on the previous one — and
only then, because it is the slowest thing you can write by accident.

The difference is where the `await` goes. Awaiting inside the loop means each
call finishes before the next starts, so the total is the sum of the latencies.
Starting every call and awaiting once means the total is the slowest of them.
`examples/WhenAllVersusSequential` measures it: 935 ms against 111 ms for
identical results.

`WhenAll` returns results in the order the tasks were supplied, not the order
they completed, so no bookkeeping is needed to keep them aligned. If any task
faults it waits for all of them to finish and then throws the first exception.
That "waits for all" detail matters more than it looks: it is what makes it safe
to dispose things the tasks were using once `WhenAll` has returned or thrown.

There is also a behavioural difference, not only a speed one. A sequential loop
stops at the first failure, so the remaining calls never happen. `WhenAll` has
already started them. When those calls have side effects, the two versions do
genuinely different things when something goes wrong — which is why exercise 1
asserts that every call was started even when an early one fails.

### 5. Cancellation is a contract

Accept a `CancellationToken` on anything that can take a meaningful amount of
time, and pass it to everything you call. Do not accept one and ignore it: a
token that stops at the first method is worse than no token, because the caller
now believes cancellation works.

Check it before starting, not only during. Work begun on an already-cancelled
token still takes a connection, a thread and a retry budget before its result is
thrown away. `CancellationRelay` is exercise 2 and that check is most of it.

Cancellation surfaces as `OperationCanceledException`, and it means *the caller
asked to stop* — it is not a failure of the operation. Two rules follow.
Do not catch it and convert it into a generic error, or a cancelled request
becomes a logged exception and a false alert. And do not retry it: exercise 3's
retry policy excludes it from its `when` clause deliberately, because retrying a
cancellation means ignoring the caller who just told you to stop.

The most common real bug here is a retry loop that ignores its token. It keeps
hammering a dependency that is already known to be failing, long after every
caller has given up, and it does the most damage exactly when the system is
least able to absorb it.

### 6. Concurrency has to be bounded

Bound anything that fans out over a collection whose size you do not control.
`Task.WhenAll` over eight items is fine. Over eight thousand it starts eight
thousand operations at once, and the connection pool that was protecting your
database becomes the thing that exhausts it.

The failure looks like the database being slow. It is not: it is your service
asking for more concurrency than anything downstream agreed to provide. Adding
retries at that point makes it strictly worse, and adding more consumers makes
it worse again — which is why this shows up in module 22 as a system-level
failure rather than a coding mistake.

`SemaphoreSlim` is the tool, and `BoundedFanOut` is exercise 5. Start every task
immediately but make each one wait on the semaphore before doing anything, so
the ceiling applies to work in flight rather than to tasks created. Writing
results into a pre-sized array by index preserves input order without sorting.

The number you choose is a real decision, not a default. It should come from
what the downstream can take — pool size, rate limit, partition count — not
from `Environment.ProcessorCount`, which describes your machine and says
nothing about theirs.

### 7. Bridging callbacks, and the two defaults that bite

Use `TaskCompletionSource<T>` when you need to hand out a task that something
else completes: a callback API, a device event, a message correlated by id.
Do not use it to wrap work you could simply await, and never use `Task.Run` to
make a synchronous API "async" on a server — that consumes a pool thread to
avoid consuming a pool thread.

Two defaults are wrong for this job and both are one token to fix.

`SetResult` throws if the task is already completed. Callback APIs fire twice
more often than their documentation admits — a timeout races a response, a
retry arrives late — and that exception is raised on the callback's thread,
where nobody is catching it, which takes the process down. `TrySetResult` makes
a second callback a non-event.

Continuations run **inline on the completing thread** unless you ask otherwise.
So whoever calls `Complete` also runs every awaiter's continuation, on their
thread — a device callback thread, a socket IO thread, a driver's notification
thread — and your awaiter's work now blocks it.
`TaskCreationOptions.RunContinuationsAsynchronously` is the fix, and exercise 6
tests it by completing from a dedicated thread and asserting the continuation
did not run there.

### 8. Where async failures go

Look here whenever an operation "succeeded" and nothing happened. An async
failure does not announce itself; it waits inside a task for somebody to look.

A faulted task holds its exception until awaited. If nobody awaits it, nobody
ever sees it — no crash, no log, no failed request. The only trace is
`TaskScheduler.UnobservedTaskException`, which almost nobody registers, and
which fires whenever the task is eventually garbage collected, minutes later,
with no request context attached. `examples/LostException` shows exactly one
exception reaching it out of three failures.

`async void` is the same bug with the safety off. There is no task, so there is
nothing to await and nothing to observe: the exception goes straight to the
runtime as unhandled and takes the process down. Use it only for event handlers,
where the signature leaves no choice, and catch everything inside.

Fire-and-forget is not automatically wrong, but it has to be deliberate. If you
want to start work and not wait for it, hold the task and await it somewhere
that can report what happened. Exercise 7 is that shape: a dispatcher that keeps
its handler's failure and raises it at disposal rather than dropping it, and
that drains its backlog on the way out instead of discarding it on every deploy.

One last trap in the same family: `await` inside a `foreach` over an
`IAsyncEnumerable` is lazy by design, and an implementation that buffers the
whole stream to process it has thrown away the reason the stream was async.
That is exercise 8.

## Real-world case

**The mechanism.** A checkout endpoint needs a fraud score. The fraud client is
async, the method that needs it is not — it is called from a constructor, or an
interface nobody owns, or code that predates async — so somebody writes
`var score = _fraud.GetScoreAsync(order).Result;`. It works. It is reviewed, it
is tested, it ships, and for weeks it is the least interesting line in the file.

Under load it becomes the only line that matters. Each concurrent checkout parks
a thread pool thread inside `.Result`, waiting for a continuation that itself
needs a pool thread to run. Past a threshold, every worker is parked waiting for
work that is queued behind the parked workers. The pool responds the only way it
can, by injecting threads, deliberately slowly. `examples/StarvedPool` measures
the same effect in miniature: identical work, twenty extra operating system
threads, several times the wall clock.

**The detection gap.** Every instinct points away from the cause.

CPU is near idle, so the first dashboard anyone opens says the service is
healthy. Memory is flat. The database is fine — its own metrics show short
queries and a quiet connection pool, because the requests are not reaching it.
The timeouts appear on endpoints that never call the fraud service at all, since
they are queued behind the ones that do, so the traces blame whichever handler
happened to be next. And it is load-dependent, so it reproduces in production
and nowhere else; below the threshold there is always a spare thread and the
blocking call is invisible.

The tell is thread count. A worker thread count that climbs while CPU stays low
means threads are being created to replace threads that are blocked, and almost
nothing else produces that shape. Most teams find it by taking a dump and seeing
dozens of stacks parked in the same `.Result`.

**What it would cost you.** Every number here is an input, not a finding;
substitute your own. Take a checkout that handles 300 requests a minute at peak,
a two-hour degradation before someone correlates thread count with the deploy,
and a 40% failure rate while it lasts. That is 300 × 120 × 0.4 ≈ 14,400 failed
checkouts. At an average basket of 85 and a recovery rate of 70% — most people
retry, some do not — the unrecovered value is 14,400 × 85 × 0.3 ≈ 367,000.

Whether that number is right for you depends entirely on your volume and your
recovery rate, and both are things you can measure rather than guess. The shape
is what transfers: a single-line change, invisible in review, that converts a
dependency's latency into your service's availability.

Run it rather than believing it:

```bash
dotnet run --project modules/03-async-await-and-the-thread-pool/examples/StarvedPool
```

The repair is exercise 2 and the discipline of section 3. There is no clever
fix, and there is no pool size that solves it.

## Exercises

All eight live in `modules/03-async-await-and-the-thread-pool`. Run the whole
module with `./run.sh test 03` — all 49 tests fail until you implement the stubs
in `src/Exercises`. Work one class at a time with `--filter-class`.

**Core**

1. **`OrderPipeline`** — concurrent composition, asserted by overlap rather than by results.
   `dotnet test --project modules/03-async-await-and-the-thread-pool/tests/UnitTests --filter-class "*OrderPipelineTests"`
2. **`CancellationRelay`** — passing the token down, and refusing to start when it is already cancelled.
   `dotnet test --project modules/03-async-await-and-the-thread-pool/tests/UnitTests --filter-class "*CancellationRelayTests"`
3. **`AsyncRetryPolicy`** — bounded retries that stop when the caller gives up.
   `dotnet test --project modules/03-async-await-and-the-thread-pool/tests/UnitTests --filter-class "*AsyncRetryPolicyTests"`
4. **`SingleFlightCache`** — collapsing a stampede into one call, without caching the failure.
   `dotnet test --project modules/03-async-await-and-the-thread-pool/tests/UnitTests --filter-class "*SingleFlightCacheTests"`
5. **`BoundedFanOut`** — concurrency with a ceiling, results still in order.
   `dotnet test --project modules/03-async-await-and-the-thread-pool/tests/UnitTests --filter-class "*BoundedFanOutTests"`

**Challenge**

6. **`CallbackBridge`** — `TaskCompletionSource`, and the two defaults that bite.
   `dotnet test --project modules/03-async-await-and-the-thread-pool/tests/UnitTests --filter-class "*CallbackBridgeTests"`
7. **`NotificationDispatcher`** — a channel that drains on shutdown and keeps its failures.
   `dotnet test --project modules/03-async-await-and-the-thread-pool/tests/UnitTests --filter-class "*NotificationDispatcherTests"`
8. **`AsyncBatcher`** — batching an async stream without draining it.
   `dotnet test --project modules/03-async-await-and-the-thread-pool/tests/UnitTests --filter-class "*AsyncBatcherTests"`

Exercise 4 has a trap worth warning about, because it is the same one the
reference solution hit first. An async method runs synchronously until its
first *incomplete* await, so a factory that returns an already-faulted task
runs your cleanup path before the calling method has finished. Get the ordering
wrong and the failed call stays cached — which is exactly what the cleanup was
there to prevent.

Reference solutions are in `src/Solutions`. Read them after you have a passing
test of your own, not before.

## Summary

What should have changed is your sense of what is scarce. Latency is not the
scarce resource on a server; threads are. Awaiting a slow call costs nothing but
time, and blocking on a fast one costs a thread — which is why `.Result` in a
request path is not a small inefficiency but a different category of mistake.

Three specifics worth keeping. `await` returns from your method rather than
waiting, so an async method that never awaits anything incomplete is just a
synchronous method with extra ceremony — and a bug that hides until the cache
misses. Concurrency without a ceiling is a load generator pointed at your own
dependencies. And a task nobody awaits is a failure nobody hears about, which is
the quietest way a system can be broken.

One habit: when a service is slow and CPU is idle, look at thread count before
anything else. Rising threads with flat CPU is close to a signature, and it
points at the blocking call rather than at the endpoint that timed out.

## Review questions

Answer before expanding. If you reach for the compiler, that is the answer.

**1. Does `await` start a thread?**

<details><summary>Answer</summary>
No. It registers the rest of the method as a continuation and returns to the
caller. Awaiting I/O occupies no thread at all while the operation is in flight.
</details>

**2. A slow call and a blocking call both take 500 ms. Why is only one a problem?**

<details><summary>Answer</summary>
The awaited one holds no thread while it waits; the blocking one holds a pool
thread for the whole 500 ms. Latency costs time, blocking costs the scarce
resource.
</details>

**3. Service is slow, CPU near idle, database quiet. What do you look at?**

<details><summary>Answer</summary>
Thread count. Rising worker threads with flat CPU means threads are being
injected to replace blocked ones — the signature of starvation.
</details>

**4. Why does moving `await` inside a `foreach` change behaviour and not just speed?**

<details><summary>Answer</summary>
It stops at the first failure, so later calls never start. `WhenAll` has already
started them all. With side effects, the two do genuinely different things.
</details>

**5. Your retry policy catches every exception and retries. What did you break?**

<details><summary>Answer</summary>
Cancellation. `OperationCanceledException` means the caller asked to stop;
retrying it ignores them and keeps load on a failing dependency.
</details>

**6. Twenty requests miss the same cache key at once. What reaches the database?**

<details><summary>Answer</summary>
Twenty identical queries, unless something collapses them. Caching the task
rather than the value makes latecomers await the call already in flight.
</details>

**7. What does `RunContinuationsAsynchronously` prevent?**

<details><summary>Answer</summary>
Awaiters' continuations running inline on whichever thread completed the task —
typically a callback or IO thread that your work then blocks.
</details>

**8. You start a task and never await it. It throws. What happens?**

<details><summary>Answer</summary>
Nothing visible. The exception sits in the task until it is collected, then
raises `UnobservedTaskException`, which almost nobody handles. The caller was
told the operation succeeded.
</details>

If any answer above surprised you, this module is not finished with you yet.
It is an anchor module — five later ones assume it — and the assumptions are
silent.

## Resources

- [Asynchronous programming with async and await](https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/) — the model itself, including what the compiler actually generates.
- [The Task asynchronous programming model](https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/task-asynchronous-programming-model) — where the state machine and continuation vocabulary comes from.
- [`SemaphoreSlim`](https://learn.microsoft.com/en-us/dotnet/api/system.threading.semaphoreslim) — the bounding primitive from exercise 5; note `WaitAsync` rather than `Wait`.
- [`TaskCompletionSource<T>`](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.taskcompletionsource-1) — read the Remarks on continuation scheduling, which is the part exercise 6 turns on.
- [System.Threading.Channels](https://learn.microsoft.com/en-us/dotnet/core/extensions/channels) — the producer/consumer primitive behind exercise 7, including completion and draining.
- [Recommended patterns for CancellationToken](https://devblogs.microsoft.com/premier-developer/recommended-patterns-for-cancellationtoken/) — the contract in section 5, written out properly.
- [ThreadPool starvation](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/debug-threadpool-starvation) — the official diagnostic walkthrough for the real-world case, using dumps and counters.
