# Module 07 · The middleware pipeline

## Before you start

You need module 06. A middleware class is resolved from the container, and the
single most common bug in custom middleware is module 06's captive dependency
appearing in a new place — so if "who builds this, and how often" is not yet a
reflex, go back one module first.

Module 03 helps too. Every middleware is `async` and every one of them awaits
the next, so a blocking call anywhere in the pipeline blocks a thread that is
holding a request open.

Budget three to four hours. The pipeline itself is about forty lines of concept
and you will have it inside an hour. The rest of the time goes on the part that
actually costs people money: that the pipeline is configured by ordering, that
ordering is expressed as the sequence of statements in one file, and that
almost every wrong order still starts, still serves traffic, and still returns
200.

If you can already explain why a middleware placed before `UseRouting` cannot
see endpoint metadata, and what `UseMiddleware<T>` does with a scoped
constructor parameter, skip to `Challenge/` and start with `MiddlewareLifetime`.
Run
`dotnet test --project modules/07-the-middleware-pipeline/tests/UnitTests --filter-class "*MiddlewareLifetimeTests"`.
If the test asserting that two requests observe the same "scoped" marker does
not immediately tell you which of the two classes is wrong, read section 7
first.

## Objectives

By the end of this module you can:

- Describe the pipeline as function composition, and predict the order of any registration list.
- Decide when to short-circuit a request and what a short circuit hides from everything upstream.
- Place an exception handler so that it actually covers what you meant it to cover.
- Explain what `UseRouting` does, and why middleware that reads endpoint metadata must follow it.
- Reproduce, and then fix, a pipeline in which a protected endpoint answers everybody.
- Choose between `Map`, `MapWhen` and `UseWhen`, and say which of them come back.
- Write middleware in each of the three forms and give each one the right lifetime.

## Sections

### 1. The pipeline is a chain of wrappers, not a list of steps

Reach for middleware when something must apply to many requests regardless of
which endpoint handles them: correlation ids, authentication, compression,
exception handling. Do not reach for it when the concern belongs to one
endpoint or one group of them — that is what endpoint filters and the handler
itself are for, and a middleware that begins with `if (path == ...)` is
usually a handler that took a wrong turn.

The mechanism is smaller than its reputation. A middleware component is a
function that takes the next delegate and returns the delegate that replaces
it:

```csharp
Func<RequestDelegate, RequestDelegate>
```

Building the pipeline is folding that list into one `RequestDelegate`, and the
fold runs backwards — a component can only be handed its "next" once that next
already exists. Exercise 1 is that fold, in five lines, and it is genuinely all
`ApplicationBuilder.Build()` does.

Two consequences follow immediately and neither is obvious from reading a
`Program.cs`. Registration order is execution order on the way in. And it is
the reverse on the way out, because each component's code after `await
next(context)` runs as the call stack unwinds. That is the shape people call
the onion, and it decides real questions: a timing middleware measures
everything registered after it, so moving one line changes what the number
means.

```bash
dotnet run --project modules/07-the-middleware-pipeline/examples/PipelineOnion
```

Nothing sorts this list. There is no priority, no attribute, no configuration.
The order of statements in one method is the entire specification of your
request handling, which is why this module is mostly about ordering and hardly
at all about syntax.

### 2. Continue, or stop

Every component makes one decision: call `next`, or don't. Calling it continues
the request into everything registered behind you. Not calling it ends the
request there — that is short-circuiting, and it is what a cache hit, an
authentication failure and a health check all do.

Use `Run` when a component is deliberately terminal; it takes no `next`, which
makes the intent explicit and makes anything registered after it unreachable.
Exercise 2 asserts that unreachability, because "registered but never composed
in" is a genuinely confusing state to debug: the code exists, the registration
exists, and a breakpoint on it is never hit.

The part worth slowing down for is what a short circuit looks like from
outside. A component that called `next` cannot tell whether the request behind
it was served, refused, or abandoned — it just gets control back. So a logging
middleware registered first reports a completed request either way. A pipeline
that quietly rejects a third of its traffic and one that serves all of it look
identical from the first component's point of view.

One more sharp edge: the default status code is 200. A short circuit that
forgets to set a status returns success with an empty body, which downstream
clients will treat as a valid empty result rather than an error.

### 3. Where the exception handler goes, and why it is first

Reach for exception middleware once, at the top, and never scatter try/catch
through the pipeline to compensate for having placed it badly.

Exception middleware is a `try`/`catch` around its call to `next`. That single
sentence answers every question about placement: whatever is registered
**behind** the handler is inside its try block, and whatever is registered in
front of it is not. Depth is irrelevant — a throw four layers down is caught —
but direction is everything.

```bash
dotnet run --project modules/07-the-middleware-pipeline/examples/HandlerReach
```

The failure mode is not "no handler". It is a handler that is right there in
`Program.cs`, plainly visible in review, sitting behind the thing that fails.
The exception leaves the pipeline entirely, and what the client gets is not a
500 page but a dropped connection, because nothing ever wrote a response.

The corollary is the one people miss. Anything registered before the handler is
unprotected by construction. If your header or logging middleware genuinely
needs to run first, accept that it runs outside every safety net you have, and
keep it small enough that it cannot fail.

`UseExceptionHandler` is the production version and `UseDeveloperExceptionPage`
the development one; the second prints stack traces to the response, which is
why it is guarded by an environment check rather than left on. Exercise 4 uses
a hand-written handler so the mechanism stays visible.

### 4. Routing splits the pipeline in two

`UseRouting` and `UseEndpoints` are not one feature with two calls. They are two
halves with a deliberate gap between them, and the gap is the point.

`UseRouting` matches the request against the route table and attaches the
selected `Endpoint` to the `HttpContext`. It does not execute anything.
`UseEndpoints` executes the endpoint that was selected. Everything registered
between them runs **knowing which endpoint will handle the request** — and
therefore knowing its metadata, its route values, and its name.

That is the whole reason the gap exists. Authorization needs it: whether a
request is allowed depends on what the endpoint requires, and `[Authorize]`
lives on the endpoint. CORS needs it. Rate limiting per endpoint needs it. So
does any gate you write yourself.

Before `UseRouting`, `context.GetEndpoint()` returns `null`. Not "throws", not
"empty" — `null`, in the ordinary way that means "no endpoint has been selected
yet", because none has. A middleware that asks about metadata there gets a
truthful answer to a question it should not yet be asking, and the answer is
always the same: this request needs nothing.

Exercise 3 is that failure end to end, and it is the module's real-world case.

### 5. The canonical order, and what each position protects

There is a documented order for the framework's own middleware, and it is worth
knowing as a sequence of reasons rather than as a list to memorise:

| Position | Middleware | Why it is there |
|---|---|---|
| 1 | `UseExceptionHandler` | Wraps everything, so everything is covered |
| 2 | `UseHsts` / `UseHttpsRedirection` | Decide the transport before any content is produced |
| 3 | `UseStaticFiles` | Serve files without paying for routing and auth |
| 4 | `UseRouting` | Select the endpoint |
| 5 | `UseCors` | Needs the endpoint's CORS policy |
| 6 | `UseAuthentication` | Establish who the caller is |
| 7 | `UseAuthorization` | Decide if they may have this endpoint |
| 8 | your middleware | After auth, before execution |
| 9 | `UseEndpoints` | Run the endpoint |

Read positions 4 through 9 as one block. They are all after routing because
they all need to know the endpoint, and they are in that order because each
depends on the one before: you cannot authorize a caller you have not
authenticated, and you cannot authenticate against a policy you have not
selected.

Deviating is legitimate when you have a reason you can state. Static files
before authentication is a deliberate choice that makes `wwwroot` public;
moving it after auth is how you protect it, at the cost of routing every asset
request through the whole pipeline. What is not legitimate is an order that
arrived by accident, which is most of them, because `Program.cs` grows by
people adding a line near a line that looked related.

### 6. Branching

Reach for a branch when a subset of requests needs a genuinely different
pipeline — an admin area, a webhook that must skip your normal authentication,
a legacy path prefix. Reach for `UseWhen` instead when a subset needs one extra
step and then the normal treatment.

The difference that matters is whether the branch comes back:

| Method | Branches on | Rejoins the main pipeline |
|---|---|---|
| `Map` | a path prefix | no |
| `MapWhen` | any predicate over the request | no |
| `UseWhen` | any predicate over the request | yes |

`Map` and `MapWhen` replace the rest of the pipeline for matching requests. A
branch that does not terminate simply produces a 404 rather than falling
through to the main pipeline, which surprises people the first time.

`Map` also rewrites the request: the matched segment moves from `Request.Path`
onto `Request.PathBase`. Inside a `Map("/api")` branch a request for
`/api/orders` has a path of `/orders`. That is what makes a branch composable,
and it breaks any downstream code that reads `Request.Path` expecting the
original. `MapWhen` matched on something other than the path and leaves the
request exactly as it found it — exercise 5 asserts both halves of that
asymmetry.

### 7. Writing middleware: three forms, three lifetimes

There are three ways to write one, and the choice is mostly about lifetime.

**Inline**, with `app.Use(...)`. Right for something small and specific to this
application. No class, no registration, nothing to look up.

**Conventional**, a class with a constructor taking `RequestDelegate` and an
`InvokeAsync` method, registered with `UseMiddleware<T>`. This is the common
form and it has one trap, which exercise 6 is built around: **the class is
constructed once**, when the pipeline is built, from the application's root
provider. A scoped service taken in its constructor is resolved once, from the
root scope, and held for the life of the application. Every request sees the
same one. That is module 06's captive dependency, and here it is easy to walk
into because the class genuinely looks like it is created per request.

The repair is to take scoped services as extra parameters on `InvokeAsync`,
which `UseMiddleware` resolves from the request's own scope every time.
Singletons can stay in the constructor.

**Factory-based**, implementing `IMiddleware`. The container creates one per
request, so constructor injection behaves the way you expected in the first
place. The cost is that the type must be registered explicitly, and it is
usually registered scoped. Reach for it when a middleware has enough
dependencies that the `InvokeAsync` signature is getting silly.

The same reasoning covers state. A conventional middleware is one shared
instance, so a field on it is application state wearing the costume of request
state: one request's data is still sitting there when the next arrives.
Exercise 7 shows that leak with two sequential requests — it does not need a
race, though under concurrency it becomes one. Per-request state belongs in
`HttpContext.Items`, which lives and dies with the request.

### 8. The response is a stream, and it leaves

The last thing to internalise is that a response is not an object you assemble
and then send. It is a stream, and once the first bytes go out you have lost
the ability to change your mind.

Headers and the status code are only editable until the response starts. After
that, `Response.HasStarted` is true and setting a header throws — which is why
a middleware that sets a header on the way out works fine until the day the
response is large enough to flush early. If you need to touch headers at the
last possible moment, register a callback with `Response.OnStarting`, which
runs just before the first byte.

Seeing the body is harder, because the body stream is write-only and cannot be
rewound. The only way is to substitute a stream you control, let everything
downstream write into it, and then deal with the consequences — which is
exercise 8, and the consequences are the exercise. Copy the buffer back into
the original stream or the caller receives nothing. Restore the original stream
afterwards, whatever happened, or every middleware outside yours is writing
into a buffer nobody reads.

Reach for that only when something genuinely needs the body: audit logging, a
checksum, a compatibility rewrite. Buffering defeats streaming, holds the whole
response in memory, and is the usual reason a large file download becomes an
out-of-memory error on a server that was fine yesterday.

## Real-world case

**The mechanism.** A team protects internal endpoints with a small gate of
their own. It reads the endpoint's metadata and requires an API key when it
finds their `RequireApiKey` marker:

```csharp
app.Use(next => context =>
{
    var needsKey = context.GetEndpoint()?.Metadata.GetMetadata<RequireApiKey>() is not null;
    if (needsKey && !context.Request.Headers.ContainsKey("X-Api-Key"))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    }

    return next(context);
});
```

The middleware is correct. It has unit tests. Somebody adding response
compression a month later puts the new `Use` call near the top of `Program.cs`,
where the other cross-cutting things are, and moves the gate up beside it — one
line above `app.UseRouting()`.

Before routing there is no endpoint. `GetEndpoint()` returns `null`,
`needsKey` is `false`, and every protected endpoint in the application now
answers anybody who asks.

**The detection gap.** Nothing raises an error, because nothing is in error: the
gate asked a question and got a truthful answer. There is no log line, because a
request arrived and a 200 went back. The existing tests still pass — they send
a request *with* a valid key and assert it succeeds, which it does in both
orderings. Only a test that sends an unauthenticated request and asserts it is
**rejected** can tell the two configurations apart, and that test is the one
teams most often do not write.

The framework guards its own version of this. Placing `UseAuthorization` before
`UseRouting` fails loudly, with a message naming the endpoint and telling you
where the call belongs. That check exists precisely because this bug shipped
repeatedly when endpoint routing was introduced. But it keys on the framework's
`IAuthorizeData`, so it covers `[Authorize]` and `RequireAuthorization` and
nothing you wrote. A gate keyed on your own metadata gets no tripwire at all —
the example runs both so you can see the loud one next to the silent one:

```bash
dotnet run --project modules/07-the-middleware-pipeline/examples/OpenEndpoint
```

**What it would cost you.** Every number here is an input, not a finding;
substitute your own. Take an internal reporting endpoint returning 50 customer
records per call, a reordering that ships on a Tuesday, and discovery six weeks
later during a routine penetration test.

The exposure ceiling is arithmetic on the endpoint, not a guess: an unattended
scraper at a modest 5 requests per second sustains 18,000 requests an hour, and
18,000 × 50 = 900,000 records an hour. Six weeks is 1,008 hours. The endpoint
could not have served all of that, but nothing in the design stops it, and that
is the number you will be asked for.

The response cost is more concrete. Six weeks of access logs at, say, 30 days
of retention means a third of the window is already unreviewable. Three
engineers spend two days each reconstructing what was accessed — six engineer
days — and the answer at the end is "we cannot rule it out", because a
successful scrape and legitimate internal traffic are both 200s from the same
route.

The cost worth counting is not the engineer days. It is that a control you
believed was enforced was not, you cannot prove for how long or to whom, and
the disclosure decision has to be made on that basis.

The repair is exercise 3, and it is moving one line below another.

## Exercises

All eight live in `modules/07-the-middleware-pipeline`. Run the whole module
with `./run.sh test 07` — all 45 tests fail until you implement the stubs in
`src/Exercises`. Work one class at a time with `--filter-class`.

**Core**

1. **`RequestPipeline`** — compose the chain yourself; the whole mechanism in five lines.
   `dotnet test --project modules/07-the-middleware-pipeline/tests/UnitTests --filter-class "*RequestPipelineTests"`
2. **`ShortCircuiting`** — continuing, stopping, and what is unreachable behind a terminal.
   `dotnet test --project modules/07-the-middleware-pipeline/tests/UnitTests --filter-class "*ShortCircuitingTests"`
3. **`EndpointMetadataGate`** — the real-world case: one line, and the endpoint is open.
   `dotnet test --project modules/07-the-middleware-pipeline/tests/UnitTests --filter-class "*EndpointMetadataGateTests"`
4. **`ExceptionHandlingOrder`** — a handler covers what it wraps, and nothing else.
   `dotnet test --project modules/07-the-middleware-pipeline/tests/UnitTests --filter-class "*ExceptionHandlingOrderTests"`
5. **`PipelineBranching`** — `Map`, `MapWhen`, `UseWhen`, and which of them return.
   `dotnet test --project modules/07-the-middleware-pipeline/tests/UnitTests --filter-class "*PipelineBranchingTests"`

**Challenge**

6. **`MiddlewareLifetime`** — a conventional middleware is built once; act accordingly.
   `dotnet test --project modules/07-the-middleware-pipeline/tests/UnitTests --filter-class "*MiddlewareLifetimeTests"`
7. **`PerRequestState`** — a field on a middleware is not per-request state.
   `dotnet test --project modules/07-the-middleware-pipeline/tests/UnitTests --filter-class "*PerRequestStateTests"`
8. **`ResponseBuffering`** — read the response on the way out without stealing it.
   `dotnet test --project modules/07-the-middleware-pipeline/tests/UnitTests --filter-class "*ResponseBufferingTests"`

Reference solutions are in `src/Solutions`. Read them after you have a passing
test of your own, not before.

## Summary

What should have changed is how you read a `Program.cs`. Not as configuration,
which is how it looks, but as the definition of a nested structure in which the
vertical order of the statements is the whole specification. Moving a line is a
behavioural change, and it is a behavioural change with no type to check it and
no test that fails by default.

Four specifics worth keeping. The pipeline is function composition, so
registration order is execution order in and its reverse out — a component's
second half runs on the way back, or not at all if something ahead of it
short-circuited. An exception handler covers what is registered behind it and
nothing in front, at any depth. `UseRouting` selects the endpoint and
`UseEndpoints` runs it, so anything that needs to know *which* endpoint must sit
between them; before routing, `GetEndpoint()` truthfully returns null. And a
conventional middleware class is constructed once, which makes constructor
injection of a scoped service a captive dependency and an instance field a
cross-request leak.

One habit: whenever you move or add a line in `Program.cs`, ask what is now in
front of it and what is now behind it. Both answers changed, and one of them is
usually the one that mattered.

## Review questions

Answer before expanding. If you reach for the compiler, that is the answer.

**1. In what order do middleware components run on the way out?**

<details><summary>Answer</summary>
The reverse of registration order. Each component's code after `await
next(context)` runs as the stack unwinds, so the last registered finishes first.
</details>

**2. A middleware calls `next` and gets control back. What does it know about what happened?**

<details><summary>Answer</summary>
Nothing. Served, refused and short-circuited are indistinguishable from
upstream, which is why a logging middleware registered first reports every one
of them as a completed request.
</details>

**3. Your exception handler is registered after your logging middleware. What is unprotected?**

<details><summary>Answer</summary>
The logging middleware, and anything else in front of the handler. A failure
there escapes the pipeline entirely — a dropped connection rather than a 500,
because nothing wrote a response.
</details>

**4. What does `context.GetEndpoint()` return before `UseRouting` has run?**

<details><summary>Answer</summary>
`null`. No endpoint has been selected yet, so any middleware asking about
endpoint metadata there finds none and concludes the request needs nothing.
</details>

**5. Why is there a gap between `UseRouting` and `UseEndpoints` at all?**

<details><summary>Answer</summary>
So that middleware can run knowing which endpoint will handle the request.
Authorization, CORS and per-endpoint rate limiting all need the endpoint's
metadata, and it does not exist before routing.
</details>

**6. Which of `Map`, `MapWhen` and `UseWhen` rejoin the main pipeline?**

<details><summary>Answer</summary>
Only `UseWhen`. `Map` and `MapWhen` replace the rest of the pipeline for
matching requests; a branch that does not terminate returns a 404 rather than
falling through.
</details>

**7. A conventional middleware takes a scoped service in its constructor. How many instances does it see?**

<details><summary>Answer</summary>
One, for the life of the application. The class is constructed once from the
root provider. Take scoped services as extra `InvokeAsync` parameters instead,
or implement `IMiddleware`.
</details>

**8. Where does per-request state belong, and what is wrong with a field?**

<details><summary>Answer</summary>
`HttpContext.Items`, which lives and dies with the request. A field lives on the
one shared middleware instance, so one request's data is still there when the
next arrives — a leak that does not need concurrency to happen.
</details>

If any answer above surprised you, this module is not finished with you yet.
Module 08 puts a client at the other end of this pipeline, and module 20 assumes
you can say exactly where in the order an authorization decision is made.

## Resources

- [ASP.NET Core Middleware](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/) — the pipeline, `Use`/`Run`/`Map`, and the canonical order table from section 5.
- [Write custom ASP.NET Core middleware](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/write) — the conventional form, and where its dependencies come from.
- [Factory-based middleware activation](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/extensibility) — `IMiddleware`, per-request activation, and why it needs registering.
- [Routing in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/routing) — what `UseRouting` attaches to the context and what `UseEndpoints` does with it.
- [Handle errors in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling) — `UseExceptionHandler`, and why the developer page is environment-gated.
- [HttpContext in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/use-http-context) — `Items`, `Features`, and the response lifecycle from section 8.
- [Endpoint metadata](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/routing#endpoint-metadata) — what metadata is and how a middleware between routing and execution reads it.
