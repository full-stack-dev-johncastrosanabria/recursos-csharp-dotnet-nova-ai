# Module 05 · Host configuration and options

## Before you start

You need module 02 for object lifetime — options are an exercise in lifetime
wearing different vocabulary, and the change-token subscription in exercise 6 is
module 02's event-handler leak in new clothes. Module 06 covers dependency
injection properly; this module uses the container but does not explain it, so
if `AddSingleton` and `AddScoped` are unfamiliar, read them as "one per process"
and "one per request" and carry on.

Budget three to four hours. Nothing here is algorithmically hard. The difficulty
is that every wrong choice still compiles, still starts, and still returns a
plausible value — which is the entire subject.

If you can already say which of `IOptions`, `IOptionsSnapshot` and
`IOptionsMonitor` a singleton may inject and why the container enforces it, skip
to `Challenge/` and start with `ConfigurationWatcher`. Run
`dotnet test --project modules/05-host-configuration-and-options/tests/UnitTests --filter-class "*ConfigurationWatcherTests"`.
If the "re-registering after each change" case does not immediately make sense,
read section 6 first.

## Objectives

By the end of this module you can:

- Predict which layer supplies a configuration value, and prove it.
- Bind a section onto a typed object, including arrays and dictionaries.
- Validate options so a bad configuration stops a deploy instead of a customer.
- Choose correctly between the three options interfaces, and say why.
- Configure several instances of one options type by name.
- Subscribe to configuration changes without leaking the subscriber.

## Sections

### 1. Configuration is layers, and the last one wins

Reach for this model whenever a value is not what you expected. Configuration is
not a file. It is an ordered list of providers — JSON files, environment
variables, command line arguments, key vaults, in-memory collections — flattened
into one case-insensitive key/value space. A key set by more than one provider
takes the value from the last provider registered.

Everything follows from that ordering. The default host registers
`appsettings.json`, then `appsettings.{Environment}.json`, then user secrets in
development, then environment variables, then command line arguments — which is
why an environment variable overrides a file, and why `--Payments:ApiKey=x`
overrides everything.

The flattening is worth internalising too. Nesting is expressed with `:` in a
key, so `Payments:ApiKey` is one key, not a lookup into an object. Environment
variables cannot contain `:` on all platforms, so they use `__` instead:
`Payments__ApiKey`. Arrays are keys with numeric segments — `Carriers:0:Name`.

When a value surprises you, ask the runtime rather than reasoning about it.
`IConfigurationRoot.GetDebugView()` prints every key with the provider that won
it. `examples/LayerPrecedence` shows both that and the per-provider view. Use it
in a debugger — never in a log, for reasons in section 7.

### 2. Binding a section onto a type

Bind whenever more than one value belongs together. Reading
`configuration["Payments:TimeoutSeconds"]` at the point of use scatters string
keys and parsing through your code, and every one of those is a rename waiting
to fail silently.

Binding maps a section onto a plain class by matching property names to keys,
case-insensitively, recursively. Arrays bind from index keys, dictionaries bind
from the child key names. Exercise 5 does all four shapes.

Two behaviours are worth knowing before you rely on them.

**An absent section binds successfully.** It does not throw and does not return
null; you get a fully constructed object with defaults and empty collections. A
typo in a section name therefore produces a service that starts, reports
healthy, and does nothing — the quietest failure in this module. Section 3 is
the answer.

**Array indexes are positions, not identities.** The binder appends children in
key order, so a gap collapses rather than truncating, and overriding
`Carriers:2` from the environment when the file defines two carriers appends a
third instead of replacing one. Configuration arrays are a poor place to keep
things you intend to address individually; a dictionary keyed by name is better
precisely because the key means something.

Keep the section name on the options type as a `const`, so it is written once.

### 3. Validate, and validate at startup

Validate every options type that can be misconfigured, which is all of them.
Then make the validation run at startup. These are two separate decisions and
the second one is the one people miss.

`ValidateDataAnnotations()` turns `[Required]` and `[Range]` into checks.
`IValidateOptions<T>` handles rules that span properties — "the ceiling must not
be below the floor" has nowhere to live in an attribute, because each attribute
sees one property. Exercise 8 is that case, and it also asks you to report every
failure at once: a validator that stops at the first turns a three-mistake
configuration into three deploys.

But options are validated **lazily by default** — on first access to `.Value`.
So a broken configuration is accepted at boot, the instance reports healthy and
starts taking traffic, and the failure arrives on the first request that happens
to need that value. That may be minutes later or hours. It affects only the
instances that have taken such a request. And the stack trace describes a
checkout, not a deploy.

`.ValidateOnStart()` moves it to startup. The instance never becomes healthy,
the rolling deploy halts on the first one, and the message names the setting.
`examples/FailFastOrFailLater` runs both, and the contrast is the whole
argument. It is one method call, and it is the highest-value line in this module.

### 4. The three options interfaces

This is the module's core decision, and the real-world case is what happens when
it goes wrong.

**`IOptions<T>`** is a singleton. Its `Value` is computed once, on first access,
and cached for the life of the process. Use it when the value genuinely cannot
change after startup: a listening port, a connection string.

**`IOptionsSnapshot<T>`** is scoped. It is recomputed per scope — per request in
a web application — and is stable within that scope. Use it when the value may
change but must not change *during* an operation. A flag flipping halfway
through a request would mean one operation taking both branches.

**`IOptionsMonitor<T>`** is a singleton that reads through to the current value
every time, and can notify you on change. Use it in a singleton, or wherever you
genuinely want the newest value now.

The failure this module is named for is injecting `IOptions<T>` where the value
can change, then capturing it. `examples/StaleFlag` reads one flag all three
ways across a configuration change: the `IOptions` column never moves, for the
rest of the process. Restarting appears to fix it, which is how it gets filed as
a deployment quirk rather than a bug.

One more piece of the same machinery, because it explains a class of confusing
behaviour: `Configure` calls accumulate. Registering `Configure<T>` twice does
not replace the first registration — both run, in registration order, against
the same instance. That is what lets a library supply defaults and an
application override them. `PostConfigure<T>` runs after every `Configure`, which
is where a value derived from other values belongs. It also means a stray second
`Configure` call somewhere in your startup silently wins over the one you were
looking at.

The container already enforces half of this for you. `IOptionsSnapshot<T>` is
scoped, so a singleton cannot inject one — the container will refuse. When it
does, it is answering your question: a singleton that wants live values wants
the monitor.

### 5. Named options

Use named options when you need several configured instances of one options
type: two payment gateways, three HTTP clients, a primary and a fallback. Bind
each child of a section under its own name and resolve with
`IOptionsMonitor<T>.Get(name)` or `IOptionsSnapshot<T>.Get(name)`.

Note what is missing from that list. Plain `IOptions<T>` has no `Get` — it only
ever sees the unnamed instance. Injecting it into a service that was meant to
use a named configuration silently hands you defaults, and defaults look like a
working object.

The other sharp edge is that **an unconfigured name is not an error**. Ask for a
name that was never registered and the factory cheerfully builds one from
defaults, so a typo becomes a client pointed at an empty endpoint rather than a
startup failure. Exercise 4 asserts that behaviour so you have met it once
deliberately. If your names come from configuration, validate the set at startup.

### 6. Reacting to change

Reach for this when something must act on a change rather than merely read the
new value: refresh a cache, reopen a connection, re-evaluate a policy.
`IOptionsMonitor<T>.OnChange` and `IChangeToken` both do it.

The mechanic that catches people is that **a change token is single-use**. It
fires once and is then spent. Subscribe by hand with
`configuration.GetReloadToken().RegisterChangeCallback(...)` and you are notified
of exactly one change and never again — which is a nasty bug precisely because
the first change *does* arrive, so it demonstrably works when you test it.
`ChangeToken.OnChange` exists to re-register on every fire; exercise 6 asserts
five consecutive changes for that reason.

The other half is disposal, and it is module 02 again. The registration roots
your callback, which roots the object that owns it, which roots everything that
object holds. A subscription you never dispose is a leak whose reachability path
runs through the configuration system, which is not where anyone looks.

One caution on file reload: `reloadOnChange: true` watches the file, and editors
that write by truncate-then-write can produce a transient read of an empty or
partial file. Validate on change too, not just at startup.

### 7. Secrets

Keep secrets out of `appsettings.json`, which is in source control and stays in
history after you delete it. In development use user secrets; in production use
environment variables or a managed secret store. Both arrive as ordinary
configuration providers, so the code that reads them does not change.

The leak this module can actually teach you to prevent is the startup dump.
Logging effective configuration at boot is genuinely useful — it answers "what
did *this* instance actually read", which is otherwise surprisingly hard. It is
also the single most common way a production secret reaches a log aggregator, a
support ticket and a screenshot in a chat channel, because the dump was written
before anyone thought about which keys it would contain.

Exercise 7 is a redacting dump. It matches on the **key**, not the value, and
that is the important design decision: you cannot recognise a secret by looking
at it, since an API key and an account id are both opaque strings — but whoever
named the setting already told you what it holds. Note the direction it fails
in: a secret stored under an innocuous name still leaks. It is one layer, not
permission to log everything.

### 8. What the host actually gives you

`Host.CreateApplicationBuilder` is not magic, and knowing what it wires up makes
it much easier to work out where a value came from. It configures the layered
providers from section 1, reads `DOTNET_ENVIRONMENT`/`ASPNETCORE_ENVIRONMENT` to
pick the environment-specific file, sets up logging, registers `IConfiguration`
in the container, and builds an `IServiceProvider`.

`Host.CreateEmptyApplicationBuilder` — which the tests in this module use —
gives you the same shape with none of the defaults, which makes it the right
tool for tests: nothing is inherited from the machine, so a test cannot pass or
fail because of a stray environment variable.

Two host behaviours matter for what you have just learned.
`ValidateOnStart` runs during `StartAsync`, which is what makes a bad
configuration halt a rollout. And `IHostedService.StartAsync` runs before the
application accepts traffic, so validating there is the same guarantee for
things options cannot express — a reachable database, a resolvable endpoint.

Startup order is worth knowing precisely, because people put checks in the wrong
place. Hosted services start in registration order, and each one's `StartAsync`
must complete before the next begins — so a slow check blocks everything behind
it, and a hosted service that never returns will hang the host rather than fail
it. Options validation registered by `ValidateOnStart` runs before any of them.

Environment names are worth one warning: they are ordinary strings compared
case-insensitively, and `builder.Environment.IsDevelopment()` is just a
comparison against `"Development"`. A misspelled environment variable does not
error; it silently selects production behaviour.

## Real-world case

**The mechanism.** A checkout service reads a feature flag through a small
wrapper:

```csharp
public sealed class FeatureGate(IOptions<FeatureOptions> options)
{
    public bool CheckoutV2Enabled { get; } = options.Value.CheckoutV2Enabled;
}
```

It is registered as a singleton, which is correct — it holds no state and there
is no reason to build one per request. `IOptions<T>` is the interface most
examples show. The value is captured in the constructor because reading it once
looks like the efficient thing to do.

Everything about that is defensible in isolation, and together it means the flag
can never change again for the life of the process. `IOptions<T>` is a singleton
whose `Value` is computed once and cached; the gate then copies it into a
readonly property. Two layers of caching, neither of them wrong on its own.

**The detection gap.** The flag flip appears to work. Configuration reloads, the
new value is genuinely in `IConfiguration`, and anything reading
`IOptionsSnapshot` or `IOptionsMonitor` sees it immediately — so a dashboard, an
admin endpoint or a health check that reads config will confirm the new value
while checkout continues on the old one. `examples/StaleFlag` shows exactly that
split: two columns move and one does not.

Then someone restarts the service and the flag takes effect. That is the worst
possible outcome, because it teaches the team that flag changes "need a restart"
— a rule that is false, gets written down, and hides the bug for years. The next
incident is a flag flipped to disable a broken feature, which does not take
effect, during an incident, while everyone believes it has.

**What it would cost you.** Every number here is an input, not a finding;
substitute your own. Take a flag flipped to disable a checkout path that is
failing, 300 checkouts an hour, a 30% failure rate on the broken path, and 40
minutes between flipping the flag and someone realising it did nothing and
ordering a restart.

That is 300 × (40 ÷ 60) × 0.3 = 60 failed checkouts that the flag was supposed
to prevent. At an average basket of 85 and 70% of customers retrying later, the
unrecovered value is 60 × 85 × 0.3 ≈ 1,500. Small. The number that is not small
is the 40 minutes, because during them the team believed the mitigation was
applied and stopped looking for one.

The cost worth counting here is not the money. It is that a control you rely on
during an incident does not work, and that you find out during the incident.

Run it rather than believing it:

```bash
dotnet run --project modules/05-host-configuration-and-options/examples/StaleFlag
```

The repair is exercise 3, and it is a one-word change: inject
`IOptionsMonitor<T>` and read `CurrentValue` at the point of use rather than
capturing it in a constructor.

## Exercises

All eight live in `modules/05-host-configuration-and-options`. Run the whole
module with `./run.sh test 05` — all 48 tests fail until you implement the stubs
in `src/Exercises`. Work one class at a time with `--filter-class`.

**Core**

1. **`ConfigurationLayering`** — layer order, and which one wins.
   `dotnet test --project modules/05-host-configuration-and-options/tests/UnitTests --filter-class "*ConfigurationLayeringTests"`
2. **`PaymentOptionsSetup`** — bind, validate, and fail at startup rather than at first use.
   `dotnet test --project modules/05-host-configuration-and-options/tests/UnitTests --filter-class "*PaymentOptionsSetupTests"`
3. **`OptionsLifetimes`** — the real-world case: three interfaces, three behaviours.
   `dotnet test --project modules/05-host-configuration-and-options/tests/UnitTests --filter-class "*OptionsLifetimesTests"`
4. **`NamedGatewayOptions`** — many instances of one options type.
   `dotnet test --project modules/05-host-configuration-and-options/tests/UnitTests --filter-class "*NamedGatewayOptionsTests"`
5. **`RoutingTableBinder`** — arrays, nested objects and dictionaries.
   `dotnet test --project modules/05-host-configuration-and-options/tests/UnitTests --filter-class "*RoutingTableBinderTests"`

**Challenge**

6. **`ConfigurationWatcher`** — change tokens, re-registration, and disposal.
   `dotnet test --project modules/05-host-configuration-and-options/tests/UnitTests --filter-class "*ConfigurationWatcherTests"`
7. **`SecretRedaction`** — a configuration dump that is safe to log.
   `dotnet test --project modules/05-host-configuration-and-options/tests/UnitTests --filter-class "*SecretRedactionTests"`
8. **`CrossFieldValidation`** — rules DataAnnotations cannot express.
   `dotnet test --project modules/05-host-configuration-and-options/tests/UnitTests --filter-class "*CrossFieldValidationTests"`

Reference solutions are in `src/Solutions`. Read them after you have a passing
test of your own, not before.

## Summary

What should have changed is where you expect configuration bugs to appear. Not
at the point of the mistake — at the point where something finally reads the
value, which may be a different class, a different request, or a different day.
Every failure in this module is displaced in time from its cause.

Three specifics worth keeping. Binding a section that does not exist succeeds
and gives you defaults, so a typo produces a healthy service that does nothing;
validation at startup is what converts that into a stopped deploy. The choice
between the three options interfaces is a statement about whether a value can
change, and capturing `IOptions<T>` in a constructor makes that statement
permanent. And a change token fires once, so a subscription that does not
re-register works exactly one time — the most convincing kind of broken.

One habit: when a configuration value is not what you expect, call
`GetDebugView()` before reading any code. It names the provider that won, and it
answers in seconds a question that otherwise takes an afternoon.

## Review questions

Answer before expanding. If you reach for the compiler, that is the answer.

**1. Two providers set the same key. Which value do you get?**

<details><summary>Answer</summary>
The one from the provider registered last. Precedence is registration order,
which is why environment variables beat files under the default host.
</details>

**2. You bind a section whose name you misspelled. What happens?**

<details><summary>Answer</summary>
Binding succeeds and gives you an object full of defaults with empty
collections. Nothing throws. The service starts healthy and does nothing.
</details>

**3. Without `ValidateOnStart`, when is a bad options value discovered?**

<details><summary>Answer</summary>
On first access to `.Value` — the first request that happens to need it, on
whichever instances have taken such a request, with a stack trace about that
request rather than the deploy.
</details>

**4. A singleton needs a value that can change at runtime. Which interface?**

<details><summary>Answer</summary>
`IOptionsMonitor<T>`, read via `CurrentValue` at the point of use.
`IOptionsSnapshot<T>` is scoped and the container will refuse to inject it.
</details>

**5. Why is capturing `options.Value` in a constructor worse than injecting `IOptions<T>`?**

<details><summary>Answer</summary>
It adds a second cache on top of one that already never refreshes, and it hides
the decision. Either way a singleton reading `IOptions<T>` is frozen at startup.
</details>

**6. You ask a named options monitor for a name that was never configured.**

<details><summary>Answer</summary>
You get a valid object built from defaults. No exception. A typo becomes a
client pointed at nothing.
</details>

**7. You register a change callback on `GetReloadToken()`. How many changes do you see?**

<details><summary>Answer</summary>
One. Tokens are single-use. Re-register inside the callback, or use
`ChangeToken.OnChange`, which does it for you.
</details>

**8. Which rule cannot be expressed with DataAnnotations, and what handles it?**

<details><summary>Answer</summary>
Anything relating two properties, such as a maximum that must not be below a
minimum. `IValidateOptions<T>` sees the whole object.
</details>

If any answer above surprised you, this module is not finished with you yet.
Module 06 builds directly on the lifetime distinctions here, and module 19
assumes you can change behaviour at runtime without restarting.

## Resources

- [Configuration in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration) — the provider model and the default ordering from section 1.
- [Options pattern in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/options) — the three interfaces, and the lifetime rules the container enforces.
- [Options validation](https://learn.microsoft.com/en-us/dotnet/core/extensions/options-library-authors#options-validation) — `ValidateDataAnnotations`, `IValidateOptions<T>` and `ValidateOnStart`.
- [Configuration providers](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration-providers) — including the `__` convention for environment variables.
- [Safe storage of app secrets in development](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) — user secrets, and why `appsettings.json` is the wrong place.
- [`IChangeToken`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.primitives.ichangetoken) — read the Remarks on single-use semantics; that is exercise 6 in one paragraph.
- [.NET Generic Host](https://learn.microsoft.com/en-us/dotnet/core/extensions/generic-host) — what the builder wires up, and where `StartAsync` sits in the sequence.
