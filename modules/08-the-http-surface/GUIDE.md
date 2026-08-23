# Module 08 · The HTTP surface

## Before you start

You need module 06, because the whole of this module is a lifetime argument:
`HttpClient` is cheap and `HttpMessageHandler` is expensive, and every mistake
here comes from treating them as one thing. Module 07 helps too — a
`DelegatingHandler` chain is that module's pipeline pointing at the network
instead of at your endpoints, and the ordering rules are identical.

Module 03 is the one to revisit if async still feels shaky. Every call in this
module is an awaited network operation, and the difference between a timeout
and a cancellation — section 5 — is a question about tokens.

Budget three to four hours. The API is small and you will have used most of it
before. What takes the time is that the two most expensive mistakes in it are
each other's supposed fix: one is corrected by sharing a client, and sharing a
client badly causes the other.

If you can already say why `PooledConnectionLifetime` exists and what
`IHttpClientFactory` does that a `static readonly HttpClient` does not, skip to
`Challenge/` and start with `HeaderScopes`. Run
`dotnet test --project modules/08-the-http-surface/tests/UnitTests --filter-class "*HeaderScopesTests"`.
If the test in which the second caller sends no credential and gets somebody
else's does not immediately look familiar, read section 8 first.

## Objectives

By the end of this module you can:

- Explain what an `HttpClient` owns and what its handler owns, and dispose accordingly.
- Reproduce socket exhaustion and DNS pinning, and say why each is the other's cure.
- Configure `IHttpClientFactory` clients and say what handler rotation buys you.
- Write a `DelegatingHandler` and place it correctly relative to the others.
- Tell a dependency timeout apart from a caller cancellation, in code.
- Decide when a non-2xx should become an exception, and keep the diagnostic when it does.
- Retry safely: the right methods, the right statuses, and a fresh request each attempt.
- Keep per-request data off a shared client.

## Sections

### 1. `HttpClient` is a facade; the handler owns the connections

Reach for `HttpClient` for any outbound HTTP call — there is no reason to use
anything lower. What matters is not the type you call but the object underneath
it.

An `HttpClient` is a thin, thread-safe wrapper holding a base address, default
headers and a timeout. Underneath sits an `HttpMessageHandler`, and for the
default `SocketsHttpHandler` that handler owns the **connection pool**: the live
TCP connections, their DNS resolutions, and the limits on both. Constructing an
`HttpClient` with no arguments constructs a fresh handler with it, and therefore
a fresh, empty pool.

Two rules follow. Disposing an `HttpClient` disposes its handler by default,
destroying the pool — which is why `using var client = new HttpClient()` is
the most expensive idiomatic-looking line in .NET. And many clients can share
one handler safely, which is exactly what you want: cheap facades over one
long-lived pool. Exercise 2 is both halves of that.

Sharing is safe because `HttpClient` is thread-safe for concurrent requests.
What is not safe to share is the mutable state on it, which is section 8.

### 2. Two failure modes, and each is the other's cure

This is the module's real-world case, and it is worth seeing as one shape rather
than two bugs.

**Per-request clients exhaust sockets.** A new client per call means a new pool
per call, so no connection is ever reused. Worse, closing a TCP connection does
not release its local port: the port sits in `TIME_WAIT` for around a minute so
that stray packets cannot be misdelivered to a new connection. Ports accumulate
at the rate you open connections, and the process runs out of ports while
leaking no memory at all.

The arithmetic is worth doing once, because it tells you where your own ceiling
is. A typical Linux ephemeral range is 32768–60999, which is 28,232 ports, and
`TIME_WAIT` is 60 seconds. So the sustainable rate of *new* connections is about
28,232 ÷ 60 ≈ 470 per second, and every one of them is unavailable for a minute
after use. With connection reuse the number of new connections is approximately
zero, whatever your request rate.

**Immortal shared clients pin DNS.** So you make the client static, and now a
connection is opened once and reused forever. A pooled connection is bound to
the IP address it was opened to and never consults DNS again. When the far side
fails over to a new address, every healthy pooled connection keeps talking to
the old one — and a healthy connection is never closed, so this does not
resolve itself.

`SocketsHttpHandler.PooledConnectionLifetime` is the answer. It retires
connections on a clock whether or not they are healthy, so DNS gets consulted
again. Two minutes is the usual value. Run both halves:

```bash
dotnet run --project modules/08-the-http-surface/examples/SocketChurn
```

### 3. `IHttpClientFactory`, and what it actually solves

Reach for the factory by default in any application that already has a
container. Reach for a hand-managed static handler only when you have no
container at all, and then set `PooledConnectionLifetime` yourself.

The factory solves both of section 2's problems at once. It **pools handlers**,
so asking for a client does not create a connection pool; and it **rotates**
each handler on a lifetime (two minutes by default), so no connection can
outlive a DNS change. The `HttpClient` it hands back is a disposable facade —
create one per call if you like, that is the intended usage.

`AddHttpClient("gateway", …)` registers a named client with a base address,
default headers and a handler chain. `AddHttpClient<TClient>()` registers a
typed client: a class of yours that takes an `HttpClient` in its constructor,
which is usually the better shape because the configuration lives next to the
code that depends on it.

One trap, and module 05 readers will recognise it exactly. **An unknown client
name is not an error.** `CreateClient("gatway")` returns a real, working
`HttpClient` with no base address and none of your headers. A typo becomes a
client that sends unauthenticated requests to a relative URL, and the failure
appears wherever that lands rather than at the typo. Exercise 5 asserts it so
you meet it once deliberately.

### 4. `DelegatingHandler` is outbound middleware

Reach for a handler when a concern applies to every call a client makes:
authentication, correlation ids, logging, retries. Do not reach for one for
something specific to a single endpoint — that belongs in the method that calls
it.

The mechanism is module 07's, mirrored. Each handler does something, calls the
inner handler, and does something with the response. The chain is built by
setting `InnerHandler`, backwards, for the same reason that pipeline's fold runs
backwards. Exercise 1 builds it by hand; `AddHttpMessageHandler` does it for you
in factory registrations, in the order you add them.

Ordering decides meaning here just as sharply as it does inbound, and the
canonical example is retry against auth. Put the retry handler **outside** the
auth handler and every attempt re-enters auth, so an expired token is refreshed
and the second attempt succeeds. Put it **inside** and auth runs once on the way
in, so the retry handler replays the same rejected token as fast as it can —
three times the load on a dependency that is already refusing you, with no
possible outcome but failure.

```bash
dotnet run --project modules/08-the-http-surface/examples/HandlerChain
```

Neither order is wrong in general. That one is wrong for those two handlers,
which is the point: you cannot review a handler in isolation.

### 5. Timeouts and cancellations are different events

`HttpClient.Timeout` applies to the **whole operation** — connect, send, and
read the response body — not to any single step. A generous body still counts
against it, which is why streaming a large download from a client with a 30
second timeout fails at 30 seconds regardless of throughput.

The part that costs people real time is telling the two failures apart. A
timeout and a caller cancellation both arrive as `TaskCanceledException`, so
code that catches the type and logs "operation cancelled" reports a dependency
outage as routine client churn.

The obvious discriminator does not work either. `HttpClient` implements its
timeout by cancelling an internal token, so the exception's own
`CancellationToken` reports cancelled in both cases. Two things do work:

| Event | Exception | InnerException | Caller's token |
|---|---|---|---|
| `Timeout` elapsed | `TaskCanceledException` | `TimeoutException` | untouched |
| Caller cancelled | `TaskCanceledException` | `TaskCanceledException` | cancelled |

So the honest catch is `when (error.InnerException is TimeoutException)` for the
first, and `when (callerToken.IsCancellationRequested)` for the second.
Exercise 3 is exactly that, and the example prints the table above from a live
run:

```bash
dotnet run --project modules/08-the-http-surface/examples/TimeoutOrCancel
```

For a per-call timeout that differs from the client's, link your own
`CancellationTokenSource` with the caller's token rather than mutating
`client.Timeout`, which is shared state on a shared object.

### 6. What comes back: a non-2xx is an answer

`HttpClient` does not throw on 404 or 503. Those are answers — the request
reached the server and the server replied. Only a transport failure throws.
This surprises people arriving from clients that throw on everything, and it is
why so much code checks nothing and parses an error page as data.

`IsSuccessStatusCode` is the check. `EnsureSuccessStatusCode()` is the
convenient version that throws for you, and it has a cost worth knowing:
**it discards the body**. The response body of a failed call is usually the only
thing that says *why* — a validation message, an upstream error id — and
`EnsureSuccessStatusCode` throws before you have read it. Exercise 4 puts the
two side by side: same failure, same exception type, and one of them has the
sentence you needed. Read the body first, then throw, and put it in the message.
`HttpRequestException.StatusCode` carries the status so callers can branch.

The other half of "what comes back" is **how much of it you hold at once**. By
default `HttpClient` does not return until the entire body is buffered in
memory, which is right for a small JSON payload and wrong for anything large:
peak memory is the response size, per concurrent request.
`HttpCompletionOption.ResponseHeadersRead` returns as soon as status and headers
have arrived, leaving the body to be read as a stream in fixed-size chunks.
Exercise 7 measures the difference — at the moment the send returns, the
buffered form has read the whole body and the streamed form has read none of it.

### 7. Retries, and what may be retried

Reach for retries only for **transient** failures: 5xx, 408, and transport
errors. A 400 or a 404 will say the same thing however many times you ask, and
retrying it just multiplies the load.

Restrict them by method. `GET` and `HEAD` are safe — repeating one cannot change
anything at the far end. `PUT` and `DELETE` are idempotent in principle, but
only if the server honours that. `POST` is neither, and a `POST` that timed out
may well have been applied already: retrying is how one order becomes two. If
you need to retry a `POST`, the far side needs an idempotency key, which is a
protocol decision, not a client one.

Then there is the trap the framework sets. **An `HttpRequestMessage` can only be
sent once.** Send the same instance again and you get
`InvalidOperationException`, "The request message was already sent". So a retry
is not a loop around `base.SendAsync` — each attempt needs a fresh message
carrying the same method, URI and headers, and a request with a body needs that
body buffered so it can be replayed. Exercise 6 is the whole rule set.

Two cautions before you write your own. Retries without jitter synchronise
across every client at once and turn a degraded dependency into an outage —
module 19 covers that properly, along with circuit breakers, and in production
you should be using a resilience library rather than a hand-rolled handler.
And every retry multiplies your effective timeout: three attempts against a 30
second client timeout is a caller waiting a minute and a half.

### 8. What a shared client shares

The whole argument of this module is that one client is shared by everything.
That makes any mutable state on it shared too, and `DefaultRequestHeaders` is
mutable state on it.

Setting a header there sets it for **every caller** until somebody changes it.
For a correlation id that is confusing. For an `Authorization` header it is a
security incident: caller A sets their token, caller B makes a request without
one, and B's request goes out carrying A's credential. Exercise 8 demonstrates
it with two sequential requests — it does not need a race, though under
concurrency it becomes one and stops being reproducible.

This is module 07's per-request-state bug on the outbound side, and it survives
review for the same reason: it looks correct whenever every caller supplies a
value. Only the caller who omits one sees somebody else's.

`DefaultRequestHeaders` is for what is genuinely true of every request from this
client — a user agent, an API version, a tenant that the client was built for.
Anything belonging to one request goes on the `HttpRequestMessage`, which exists
for exactly that request. The rule is the same one as `HttpContext.Items`
inbound: match the lifetime of the data to the lifetime of the thing you put it
on.

## Real-world case

**The mechanism.** A checkout service calls a payment gateway. The call is
wrapped in the obvious way:

```csharp
using var client = new HttpClient();
var response = await client.PostAsync(GatewayUrl, content);
```

`using` on an `IDisposable` is correct in every other context, which is why this
passes review. But the client builds a `SocketsHttpHandler` with it, and that
handler owns a connection pool, so every checkout opens a brand-new TCP
connection and then closes it. Closed connections hold their local port in
`TIME_WAIT` for about a minute.

At 200 checkouts a second the service holds roughly 200 × 60 = 12,000 ports at
any moment, against an ephemeral range of about 28,232. That is 42% — healthy,
until a sale doubles traffic and connections start failing with "address already
in use" while every dashboard shows a service that is not busy.

**The detection gap, and the fix that becomes the next incident.** The symptom
does not name its cause. It appears as sporadic connection failures under load,
resolves when traffic drops, and cannot be reproduced in staging because staging
never sustains the rate. When somebody does identify it, the fix everybody
reaches for is one `static readonly HttpClient`, and it works: connection count
drops to approximately one.

Six weeks later the gateway fails over to a new IP address. The pooled
connection is bound to the old one and never consults DNS again, and it is
perfectly healthy, so nothing closes it. Every checkout goes to an address that
is gone. The gateway's own status page is green, DNS resolves correctly from the
box, `curl` from the same host succeeds — and the service keeps failing, because
the only thing still using the old address is a connection inside your process.
The fix is a restart, which is why this gets recorded as "it needed a restart"
rather than as a missing `PooledConnectionLifetime`.

**What it would cost you.** Every number here is an input; substitute your own.
Take 200 checkouts a second, an average basket of 85, a 25 minute window between
the failover and someone deciding to restart, and 30% of customers who never
come back to complete the purchase.

That is 200 × 60 × 25 = 300,000 checkouts attempted during the window. Even at
a 10% failure share while retries and other paths absorb the rest, that is
30,000 failed checkouts, and 30,000 × 85 × 0.3 ≈ 765,000 of basket value that
does not arrive. Scale it down as hard as you like — a tenth of that is still
the most expensive line in this module.

The cost worth counting is the second one, though. The team now believes the
gateway is unreliable and that restarts fix it, and that belief is wrong, cheap
to act on, and will be applied to the next three incidents.

The repairs are exercises 2 and 5.

## Exercises

All eight live in `modules/08-the-http-surface`. Run the whole module with
`./run.sh test 08` — all 40 tests fail until you implement the stubs in
`src/Exercises`. Work one class at a time with `--filter-class`.

**Core**

1. **`MessageHandlers`** — build the outbound chain by hand; module 07's onion, mirrored.
   `dotnet test --project modules/08-the-http-surface/tests/UnitTests --filter-class "*MessageHandlersTests"`
2. **`ClientLifetime`** — what the client owns, what the handler owns, and who disposes what.
   `dotnet test --project modules/08-the-http-surface/tests/UnitTests --filter-class "*ClientLifetimeTests"`
3. **`TimeoutsAndCancellation`** — two different events wearing the same exception.
   `dotnet test --project modules/08-the-http-surface/tests/UnitTests --filter-class "*TimeoutsAndCancellationTests"`
4. **`ResponseHandling`** — a non-2xx is an answer, and the body is the diagnostic.
   `dotnet test --project modules/08-the-http-surface/tests/UnitTests --filter-class "*ResponseHandlingTests"`
5. **`NamedClients`** — `IHttpClientFactory`, and the unknown-name trap.
   `dotnet test --project modules/08-the-http-surface/tests/UnitTests --filter-class "*NamedClientsTests"`

**Challenge**

6. **`RetryHandler`** — retry what is safe to retry, with a fresh message each attempt.
   `dotnet test --project modules/08-the-http-surface/tests/UnitTests --filter-class "*RetryHandlerTests"`
7. **`StreamingResponses`** — headers first, body in chunks, bounded memory.
   `dotnet test --project modules/08-the-http-surface/tests/UnitTests --filter-class "*StreamingResponsesTests"`
8. **`HeaderScopes`** — keep one caller's credential off every other caller's request.
   `dotnet test --project modules/08-the-http-surface/tests/UnitTests --filter-class "*HeaderScopesTests"`

Reference solutions are in `src/Solutions`. Read them after you have a passing
test of your own, not before.

## Summary

What should have changed is which object you think about. Not `HttpClient`,
which is a facade with a base address on it, but the handler underneath — where
the connections live, where DNS is remembered, and where every expensive
decision in this module is actually made.

Four specifics worth keeping. A client per request builds a pool per request and
exhausts ports through `TIME_WAIT` rather than through anything that looks like
a leak. An immortal shared client fixes that and pins DNS instead, so
`PooledConnectionLifetime` — or `IHttpClientFactory`, which rotates handlers for
you — is not a tuning knob but the second half of the fix. A timeout and a
cancellation are different events sharing one exception type, and the only
reliable discriminators are the inner exception and the caller's own token. And
anything set on a shared client is set for every caller, which for an
`Authorization` header is somebody else's credential on your request.

One habit: when you write `new HttpClient(`, stop and ask what handler it just
built and how long that handler is going to live. There are only two correct
answers — the factory made it, or you set its pooled connection lifetime
deliberately.

## Review questions

Answer before expanding. If you reach for the compiler, that is the answer.

**1. What does disposing an `HttpClient` actually destroy?**

<details><summary>Answer</summary>
Its handler, by default, and therefore the connection pool — the live
connections and their DNS resolutions. Pass `disposeHandler: false` when the
handler is shared.
</details>

**2. Why does a client per request exhaust ports rather than memory?**

<details><summary>Answer</summary>
Every call opens a new connection, and closing one holds its local port in
`TIME_WAIT` for about a minute. Ports accumulate at the rate you open
connections; nothing is leaked in the managed sense.
</details>

**3. A static `HttpClient` fixed that. What did it introduce?**

<details><summary>Answer</summary>
DNS pinning. A pooled connection is bound to the address it was opened to and
never re-resolves, so a failover is invisible to it — and a healthy connection
is never closed on its own.
</details>

**4. What does `IHttpClientFactory` do that a static client does not?**

<details><summary>Answer</summary>
It rotates handlers on a lifetime, so no connection can outlive a DNS change,
while still pooling them so a client per call is cheap.
</details>

**5. You ask the factory for a client name nobody registered. What happens?**

<details><summary>Answer</summary>
You get a working `HttpClient` with no base address and none of your headers.
No exception — the same shape as module 05's unknown options name.
</details>

**6. A call fails with `TaskCanceledException`. Was it a timeout?**

<details><summary>Answer</summary>
Unanswerable from the type. Check `InnerException is TimeoutException` for a
timeout, or the caller's own token for a cancellation. The exception's own token
reports cancelled either way.
</details>

**7. What does `EnsureSuccessStatusCode` cost you?**

<details><summary>Answer</summary>
The response body, which is usually the only thing explaining the failure. Read
it first, then throw with it in the message.
</details>

**8. Why can a retry handler not just loop around `base.SendAsync`?**

<details><summary>Answer</summary>
An `HttpRequestMessage` can only be sent once; the second send throws "The
request message was already sent". Each attempt needs a fresh message, and a
body that can be replayed.
</details>

If any answer above surprised you, this module is not finished with you yet.
Module 19 takes retries seriously — jitter, budgets and circuit breakers — and
module 20 assumes you know exactly where a credential is attached to a request.

## Resources

- [HttpClient guidelines for .NET](https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines) — the socket exhaustion and DNS sections in the source, stated more briefly than here.
- [`IHttpClientFactory` with .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/httpclient-factory) — named and typed clients, handler lifetime, and the ordering of `AddHttpMessageHandler`.
- [`SocketsHttpHandler`](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.socketshttphandler) — `PooledConnectionLifetime`, `PooledConnectionIdleTimeout` and `MaxConnectionsPerServer` in one place.
- [`DelegatingHandler`](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.delegatinghandler) — the outbound chain from section 4, and what `InnerHandler` means.
- [`HttpCompletionOption`](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpcompletionoption) — two values, and the memory difference between them is exercise 7.
- [`HttpRequestException`](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httprequestexception) — including `StatusCode`, which is what makes a deliberate throw useful to a caller.
- [Make HTTP requests with the HttpClient class](https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient) — the broad tour, worth skimming for the parts this module did not cover.
