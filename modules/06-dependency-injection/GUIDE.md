# Module 06 · Dependency injection

## Before you start

You need module 02. Every failure in this module is an object lifetime failure
wearing new vocabulary — a singleton that outlives what it holds, a disposable
nobody disposes, a reference that keeps a graph alive. If "who owns this and
when does it die" is not yet a reflex, read module 02 first.

Module 05 used the container without explaining it, so some of this will look
familiar. The relationship runs the other way too: the three options interfaces
you chose between there are three lifetimes, and the rule that stopped you
injecting `IOptionsSnapshot<T>` into a singleton is the rule this module is
about.

Budget three to four hours. There is no algorithm here and no syntax worth
memorising. The difficulty is that every mistake in this module compiles,
starts, passes review, and behaves correctly under the load you test it with.

If you can already say why a singleton may not take a scoped dependency, what
the container does with a transient disposable resolved from the root provider,
and what `TryAddSingleton` is for, skip to `Challenge/` and start with
`DecoratedRepository`. Run
`dotnet test --project modules/06-dependency-injection/tests/UnitTests --filter-class "*DecoratedRepositoryTests"`.
If the test asserting that `SqlOrderRepository` and `IOrderRepository` resolve
to the same underlying object does not immediately tell you which registration
shape is required, read section 7 first.

## Objectives

By the end of this module you can:

- Choose a lifetime deliberately, and say what breaks under each wrong choice.
- Recognise a captive dependency in review, and name the flag that refuses to build one.
- Predict exactly what the container disposes, and when it does it.
- Say what happens when a service is registered twice, and choose between `Add` and `TryAdd`.
- Register keyed services and open generics, and resolve every implementation of one interface.
- Decorate a service without accidentally creating two of it.
- Judge when injecting `IServiceProvider` is legitimate and when it is hiding your dependencies.

## Sections

### 1. What the container actually is

Reach for this model whenever the container's behaviour looks like magic. There
are two objects and one idea between them.

`IServiceCollection` is a list of `ServiceDescriptor`, and a descriptor is a
triple: the service type being registered, how to produce it (a concrete type,
a factory delegate, or an existing instance), and a lifetime. Every `AddScoped`,
`AddSingleton` and `AddTransient` call appends one of these. Nothing is resolved
while you register, which is why you can register a consumer before its
dependency and why the order of your `Add` calls does not need to follow the
dependency graph. Order matters only for the rules in section 5.

`BuildServiceProvider` freezes that list into an `IServiceProvider`. Resolution
is then a recursive walk: find the descriptor for the requested type, pick a
constructor, resolve each of its parameters the same way, construct, and cache
the result according to the lifetime. A dependency you never registered is not
a compile error and not a build error — it is an `InvalidOperationException` at
the moment something asks for the thing that needs it, which may be a long way
into a running process.

That last point is the one worth carrying. The container defers almost
everything, so almost every mistake surfaces later than it was made, and often
somewhere else. Sections 3 and 4 are two specific ways that plays out.

### 2. The three lifetimes

**Singleton** is one instance per container, created on first resolve and kept
until the container is disposed. **Scoped** is one instance per scope. **Transient**
is a new instance every single time it is injected or resolved.

Two things about "scoped" cause more confusion than the rest of this module
combined. The first is that a scope is not a request; a scope is a scope. In
ASP.NET Core the framework creates one per request, which is why "scoped" and
"per request" are used interchangeably — but that equivalence is a fact about
the web host, not about the container. In a console application, a background
service, or a message consumer, nobody creates a scope for you. If you want
per-message isolation, you create it yourself.

The second is that transient is the row people underestimate. Two collaborators
that each take the same dependency share it when it is scoped and do not when
it is transient, and that difference is completely invisible until something
carries state. `examples/LifetimeMatrix` prints instance counts per lifetime so
the difference is a number rather than a claim:

```bash
dotnet run --project modules/06-dependency-injection/examples/LifetimeMatrix
```

Rules of thumb worth having. Scoped for anything touching a unit of work — a
`DbContext`, a transaction, a per-request cache. Singleton for stateless things
that are expensive to build, and for genuinely shared state, which you must then
make thread-safe. Transient for small stateless things — never as the "safe"
default, because it multiplies quietly and, if the type is disposable,
accumulates in a way section 4 makes precise.

None of this is visible where the service is used. The class takes an interface.
A registration line in another file decides whether its state is shared with the
rest of the request, the rest of the process, or nobody at all. That is why
lifetime bugs read as impossible: the code at the crime scene is correct.

### 3. Captive dependencies

A captive dependency is a service holding another service that is meant to live
less long than it does. The classic shape is a singleton whose constructor takes
a scoped dependency.

The container does exactly what you asked. It resolves the singleton once, from
the root scope, resolving its scoped dependency from the root scope too — and
the singleton then holds that one instance for the life of the process. The
dependency's lifetime is no longer scoped in any meaningful sense; it has been
captured by its holder. For a `DbContext` that means one connection and one
change tracker shared by every request forever.

Nothing about this is loud. It builds. It starts. It passes every test that runs
one operation at a time. The symptom is a concurrency error — "a second
operation was started on this context before a previous operation completed" —
raised inside whichever request happened to lose the race, so the investigation
begins at a handler that is entirely correct.

There is a flag that refuses to build it:

```csharp
services.BuildServiceProvider(new ServiceProviderOptions
{
    ValidateScopes = true,
    ValidateOnBuild = true,
});
```

`ValidateScopes` makes resolving a scoped service from the root provider an
error, which turns a captive dependency into a startup failure that names both
types. `ValidateOnBuild` walks every registration at build time instead of
waiting for the first resolve, so a missing dependency also fails at startup.
The default host enables both in the Development environment only. That is a
sensible default and a trap: the check is off in the environment where the bug
actually bites.

The repair is not to widen the dependency's lifetime. It is to stop holding it.
A singleton that needs a scoped service injects `IServiceScopeFactory`, creates
a scope per unit of work, resolves inside it, and disposes the scope when the
work ends. Exercise 2 is that repair, and `examples/CaptiveScope` runs both
versions side by side under concurrency.

### 4. Disposal: the container owns what it created

One rule covers all of it: **the container disposes what it created, when the
scope that created it ends.** Everything below follows from that sentence, and
each consequence is asserted by a test in exercise 5.

A scoped disposable is disposed when its scope ends. A singleton disposable is
disposed when the container itself is disposed. A transient disposable is
disposed when the scope that created it ends — which is where it gets
interesting, because "transient" says nothing about how long the object lives.
It says only that you get a new one each time.

So a transient disposable resolved from the root provider is held until the
container shuts down. A singleton that resolves one per operation therefore
accumulates them for the life of the process, and they are not collectable: the
provider's list of things it must dispose is a GC root, so this is a leak that
holds objects deliberately and will not show up as a missing `Dispose` call
anywhere. `examples/TransientLeak` counts them:

```bash
dotnet run --project modules/06-dependency-injection/examples/TransientLeak
```

Two more consequences. **An instance you constructed yourself and handed to the
container is not the container's to dispose** — `services.AddSingleton(myThing)`
passes a reference, not ownership, so whoever created it still has to dispose
it. And **disposal runs in reverse order of creation**, so a service is disposed
before the dependencies it was built from, which is the order you would want if
you had thought about it.

The corollary for everyday code is short: never dispose something you resolved
from the container. Wrapping a resolved service in `using` disposes an object
the container still considers live and will hand to somebody else.

One caution the exercises do not cover. A service that implements only
`IAsyncDisposable` cannot be cleaned up by a synchronous `provider.Dispose()`;
the container throws rather than silently skipping it. Dispose providers you own
with `await provider.DisposeAsync()`.

### 5. Registration rules

Registering the same service type twice does not replace and does not throw.
Both descriptors stay in the collection. A single `GetRequiredService<T>`
returns the **last** one registered, and `GetRequiredService<IEnumerable<T>>`
returns **all** of them in registration order.

That is two useful facts and one hazard. It is how you register a pipeline of
handlers or a set of plugins. It is also how a service gets registered twice by
two different extension methods, with the second quietly winning, while the
first still runs if anything resolves the enumerable.

`TryAddSingleton` and its siblings add a descriptor only if the service type has
no registration at all. This is the library author's tool: offer a default that
an application can overrule simply by registering its own first. Note the
direction — `TryAdd` yields to what is already there, so a library's defaults
must be registered after the application's choices to behave as defaults, which
is what `Add...` extension methods called from `Program.cs` naturally do.

`TryAddEnumerable` is the version for the many-implementations case: it dedupes
on the implementation type, so calling an extension method twice does not give
you the same handler running twice. `Replace` and `RemoveAll` exist for the
cases where you genuinely mean to take something out, and are worth reaching for
instead of relying on last-wins, because they say so.

Finally: `GetService<T>` returns `null` for an unregistered type and
`GetRequiredService<T>` throws. Prefer the second everywhere except the rare
genuinely-optional dependency, so a missing registration fails where it is
missing rather than as a `NullReferenceException` one layer up.

### 6. Constructor selection

When a type has more than one public constructor, the container picks the one
with the most parameters it can satisfy from what is registered. Not the one you
meant, and not the first.

The consequence is action at a distance: registering one extra service changes
which constructor runs, in a file that did not change, with no error anywhere.
Remove a registration and the container silently falls back to a smaller
constructor, so a class quietly starts running without its metrics sink or its
audit log. Exercise 3 makes this happen four different ways.

If the container cannot fully satisfy any constructor, that is an exception at
resolve time. If two constructors are equally applicable and neither's parameter
set contains the other's, that is an ambiguity exception.

The practical advice is to have one public constructor. When a type genuinely
needs a value the container cannot produce — a connection string, an endpoint, a
tenant id — register a factory rather than adding a second constructor:

```csharp
services.AddSingleton(provider => new EndpointClient(endpoint));
```

The factory receives the provider, so it can resolve the dependencies the
container does know about and supply the rest itself.

### 7. Keyed services, open generics and decoration

Three registration shapes that come up constantly once the graph grows past a
dozen services.

**Keyed services** register several implementations of one interface under
distinct keys, and resolve one by key. Declare the key at the injection point
with an attribute on the parameter rather than reaching into the provider:

```csharp
services.AddKeyedSingleton<IPaymentGateway, PrimaryGateway>("primary");

public sealed class PaymentRouter(
    [FromKeyedServices("primary")] IPaymentGateway preferred);
```

Compare this with module 05's named options, which look almost identical and
behave oppositely: an unknown options name silently gives you defaults, while an
unknown service key is an error. The container is stricter than the options
system here, and that is the safer direction.

**Open generics** register one implementation for every closed form of a generic
interface:

```csharp
services.AddSingleton(typeof(IValidator<>), typeof(DefaultValidator<>));
```

One line serves `IValidator<OrderPlaced>`, `IValidator<ShipmentBooked>` and
every other closed type, including ones that do not exist yet. A specific closed
registration for one type wins over the open generic for that type, which is how
you special-case a single message without abandoning the general rule.

The related hazard is resolving handlers by runtime type. A dispatcher that
resolves `IEnumerable<IHandler<T>>` and runs each one reports complete success
when nothing is registered, because an empty sequence is not an error. A message
type nobody handles is processed, acknowledged, and dropped. Exercise 8 asserts
that a dispatch with no handlers returns zero, so you meet it deliberately once
rather than in production.

**Decoration** has no built-in helper in this container. You register the
concrete type, then register the interface with a factory that wraps it:

```csharp
services.AddSingleton<SqlOrderRepository>();
services.AddSingleton<IOrderRepository>(provider =>
    new CachingOrderRepository(provider.GetRequiredService<SqlOrderRepository>(), log));
```

The trap is registering the concrete type and the interface as two independent
registrations. That produces two objects, so the cache the decorator holds is
not the one anything else is using, and the decoration silently does nothing —
no error, no warning, just a cache with a permanent zero hit rate. Exercise 6
tests that both resolutions reach the same underlying instance.

### 8. Injecting the provider itself

Taking `IServiceProvider` as a constructor parameter and resolving from it is
the service locator pattern, and it is worth being precise about why it is
discouraged rather than repeating the label.

A constructor signature is a machine-readable list of what a class needs.
Everything reads it: the container's own `ValidateOnBuild`, your tests, and the
next person deciding whether it is safe to change a registration. A class that
takes the provider has hidden that list. Its dependencies are now discoverable
only by reading every method body, validation cannot check them, and a missing
registration surfaces at the first call that needs it instead of at startup.
The lifetime problem comes along too: the provider a singleton receives is the
root one, so anything it resolves is resolved from the root scope, with both
consequences from sections 3 and 4.

There are legitimate uses. A dispatcher that resolves handlers by a type only
known at runtime cannot express that as constructor parameters — exercise 8 is
exactly that case. Factory abstractions and framework infrastructure are
similar. When what you actually need is a scope, inject `IServiceScopeFactory`
instead of the whole provider: it is narrower, it says what you are doing, and
it makes the per-unit-of-work boundary explicit.

The test to apply in review is whether the resolved type is known when the class
is written. If it is, it belongs in the constructor.

## Real-world case

**The mechanism.** A reporting service needs to run scheduled work against the
database, so it takes the unit of work it needs:

```csharp
public sealed class ReportScheduler(OrderSession session)
{
    public void Run() => session.Query();
}
```

`OrderSession` stands in for a `DbContext` and is registered scoped, which is
correct and is what every guide recommends. `ReportScheduler` is registered as a
singleton, which also looks correct: there is one scheduler, it holds no state,
and building one per request would be waste.

Both decisions are defensible alone. Together they mean the container resolves
one session, from the root scope, at the moment the scheduler is first built,
and the scheduler holds that session for the life of the process. Every request,
every scheduled run, every background operation shares one connection and one
change tracker.

**The detection gap.** The registration builds without complaint, because the
default provider validates nothing. It works perfectly in every test, because
tests run one operation at a time. It works in staging, because staging has no
concurrency worth the name. It reaches production and works there too, until two
requests overlap.

When it finally fails, the exception says a second operation was started on the
context before a previous operation completed, and it is raised inside whichever
request lost the race. That request's handler is correct. So the investigation
starts in the wrong file, and because the failure needs two operations to land
in the same few milliseconds, it is intermittent, unreproducible locally, and
closed as a transient database issue at least once before anyone finds it.

**What it would cost you.** Every number here is an input, not a finding;
substitute your own. Take a service handling 300 requests an hour, a collision
rate of 2% once traffic is concurrent, three days between the first error and a
correct diagnosis, and two engineers spending a day each on it.

That is 300 × 24 × 3 × 0.02 ≈ 430 failed requests, plus two engineer-days spent
in the wrong file. The failed requests are recoverable — most are retries of
something a customer will do again. The two days are not, and neither is the
third cost: a class of error that is intermittent and lands in innocent code
teaches a team to treat real errors as noise.

Run both versions rather than believing either:

```bash
dotnet run --project modules/06-dependency-injection/examples/CaptiveScope
```

The repair is exercise 2. The scheduler injects `IServiceScopeFactory`, opens a
scope per unit of work, and resolves the session inside it — and the same
example shows that `ValidateScopes` would have refused to build the broken
version at all.

## Exercises

All eight live in `modules/06-dependency-injection`. Run the whole module with
`./run.sh test 06` — all 49 tests fail until you implement the stubs in
`src/Exercises`. Work one class at a time with `--filter-class`.

**Core**

1. **`ServiceLifetimes`** — the three lifetimes as instance identity, including the shared-collaborator case.
   `dotnet test --project modules/06-dependency-injection/tests/UnitTests --filter-class "*ServiceLifetimesTests"`
2. **`CaptiveDependency`** — the real-world case, broken and repaired.
   `dotnet test --project modules/06-dependency-injection/tests/UnitTests --filter-class "*CaptiveDependencyTests"`
3. **`ConstructorSelection`** — which constructor runs, and what changes it.
   `dotnet test --project modules/06-dependency-injection/tests/UnitTests --filter-class "*ConstructorSelectionTests"`
4. **`RegistrationRules`** — registering twice, last-wins, and what `TryAdd` is for.
   `dotnet test --project modules/06-dependency-injection/tests/UnitTests --filter-class "*RegistrationRulesTests"`
5. **`ContainerDisposal`** — what the container disposes, when, and what it never disposes.
   `dotnet test --project modules/06-dependency-injection/tests/UnitTests --filter-class "*ContainerDisposalTests"`

**Challenge**

6. **`DecoratedRepository`** — wrapping a service without ending up with two of it.
   `dotnet test --project modules/06-dependency-injection/tests/UnitTests --filter-class "*DecoratedRepositoryTests"`
7. **`KeyedGateways`** — several implementations of one interface, chosen by key.
   `dotnet test --project modules/06-dependency-injection/tests/UnitTests --filter-class "*KeyedGatewaysTests"`
8. **`OpenGenericHandlers`** — open generics, many handlers, and dispatch that does nothing.
   `dotnet test --project modules/06-dependency-injection/tests/UnitTests --filter-class "*OpenGenericHandlersTests"`

Reference solutions are in `src/Solutions`. Read them after you have a passing
test of your own, not before.

## Summary

What should have changed is where you look when a service misbehaves. Not at the
service — at its registration, which is in a different file, was written by
somebody else, and is the only place its lifetime is stated.

Four specifics worth keeping. A lifetime is a statement about sharing, and the
three answers are one per process, one per unit of work, and one per injection
point; a collaborator pair sharing state is the case that makes the difference
visible. A longer-lived service holding a shorter-lived one captures it forever,
and `ValidateScopes` turns that from a production concurrency bug into a startup
error — on in Development only, which is the wrong way round. The container
disposes what it created when the creating scope ends, so a transient disposable
resolved from the root provider is held for the life of the process. And
registering twice keeps both, with the last winning a single resolve, which is
the mechanism behind both plugin lists and duplicate registrations nobody meant.

One habit: when you write `AddSingleton`, read the constructor of the thing you
just registered and check that nothing in it is scoped. That single check, done
at registration time, catches the most expensive bug in this module before it
exists.

## Review questions

Answer before expanding. If you reach for the compiler, that is the answer.

**1. What does a scope correspond to in a console application?**

<details><summary>Answer</summary>
Nothing, until you create one. A scope per request is something the web host
does for you; elsewhere you create scopes yourself, usually one per unit of work
via `IServiceScopeFactory`.
</details>

**2. A singleton takes a scoped dependency. What actually happens?**

<details><summary>Answer</summary>
It resolves once from the root scope and the singleton holds it for the life of
the process. It builds and starts cleanly; the failure is a concurrency error
later, in whichever caller lost the race.
</details>

**3. Which option would have refused to build that, and where is it on by default?**

<details><summary>Answer</summary>
`ValidateScopes`, with `ValidateOnBuild` for missing registrations. The default
host enables them in Development only — off in the environment where the bug
occurs.
</details>

**4. A singleton resolves a transient `IDisposable` per operation from the root provider. When are those disposed?**

<details><summary>Answer</summary>
When the container is disposed, i.e. at process shutdown. They accumulate, they
are rooted by the provider's disposal list, and no `using` statement at the call
site would change it.
</details>

**5. You register `INotifier` twice. What does `GetRequiredService<INotifier>` return, and what about `IEnumerable<INotifier>`?**

<details><summary>Answer</summary>
The last registration wins for the single resolve; the enumerable returns both,
in registration order. Registering twice never replaces and never throws.
</details>

**6. What is `TryAddSingleton` for, and which way does it yield?**

<details><summary>Answer</summary>
Supplying a default that must not overrule an existing registration. It adds
only if the service type has no registration, so it yields to whatever was
registered first.
</details>

**7. A class has two public constructors. Which one does the container use?**

<details><summary>Answer</summary>
The one with the most parameters it can satisfy from the registered services. So
adding or removing an unrelated registration can change which constructor runs,
with no change to the class.
</details>

**8. You register `SqlOrderRepository` and `IOrderRepository` separately, meaning the latter to decorate the former. What goes wrong?**

<details><summary>Answer</summary>
Two independent instances. The decorator wraps its own copy, so any state it
holds is not the state anybody else sees, and the decoration silently does
nothing. Resolve the inner one inside the factory instead.
</details>

If any answer above surprised you, this module is not finished with you yet.
Module 07 assumes you can read a registration and predict the object graph it
produces, and the middleware and pipeline modules later assume scopes are
something you reason about rather than inherit.

## Resources

- [Dependency injection in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection) — the descriptor model and the lifetimes from sections 1 and 2.
- [Dependency injection guidelines](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-guidelines) — captive dependencies, disposal ownership, and the service locator discussion behind section 8.
- [Service lifetimes](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection#service-lifetimes) — the short, precise statement of all three, worth rereading after exercise 1.
- [Keyed services](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection#keyed-services) — `AddKeyedSingleton` and `[FromKeyedServices]`, which is exercise 7.
- [`ServiceCollectionDescriptorExtensions`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.extensions.servicecollectiondescriptorextensions) — `TryAdd`, `TryAddEnumerable`, `Replace` and `RemoveAll` in one place.
- [`ServiceProviderOptions`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.serviceprovideroptions) — the two validation flags from section 3 and exactly what each one checks.
- [Dependency injection in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection) — where request scopes come from, and the framework services registered for you.
