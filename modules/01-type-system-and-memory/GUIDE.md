# Module 01 · Type system and memory model

## Before you start

You need C# and .NET 10 installed, and you should be comfortable writing a class,
a method and a unit test. You do not need to have thought about memory before.

Budget three to four hours: roughly one to read, two to work through the eight
exercises, and a little to run the three examples and argue with them.

If you can already explain why `record struct` equality differs from `class`
equality, and name three places boxing hides without looking them up, skip
straight to the `Challenge/` exercises and start with `SkuList`. Run
`dotnet test --project modules/01-type-system-and-memory/tests/UnitTests --filter-class "*SkuListTests"`.
If that one takes you more than twenty minutes, come back and read section 4
properly — the rest of this guide will save you time rather than cost it.

## Objectives

By the end of this module you can:

- Choose between a `class`, a `struct` and a `record` for a specific type, and defend the choice.
- Name where a value lives and when that actually affects a decision you make.
- Spot boxing in code that contains no cast and no `object` keyword.
- Implement `Equals`, `GetHashCode` and `IEquatable<T>` so a dictionary behaves.
- Predict what compiler-generated record equality does with a collection member.
- Explain what `string.Intern` costs and why it is rarely the right tool.

## Sections

### 1. Values and references — what the variable holds

Reach for a `struct` when the thing is small, genuinely a value, and you will
have a great many of them: a point, a money amount, a date range. Reach for a
`class` when the thing has identity, when it is large, or when you are not sure
— `class` is the right default, and a `struct` chosen for vague performance
reasons usually costs more than it saves.

The distinction is not where the data lives. It is what the variable holds. A
variable of class type holds a reference: a small handle pointing somewhere
else. A variable of struct type holds the value itself, inline, wherever that
variable happens to live.

Every consequence follows from that one fact. Assigning a class variable copies
the handle, so two names refer to one object and a change through either is
visible through both. Assigning a struct variable copies the whole value, so
two names refer to two independent things and a change through one is invisible
to the other. Passing to a method is the same operation with a different name.

This is where the "structs are faster" folklore goes wrong. A struct avoids one
allocation. It also gets copied at every assignment, every method call, every
time it is stored or retrieved. A 200-byte struct passed through five call
frames copies 1,000 bytes to avoid a 24-byte allocation. The usual guidance —
keep structs under 16 bytes or so — is not arbitrary; it is the point where
copying stops being cheaper than pointing.

`Money` in this module is a good struct: two fields, immutable, obviously a
value. `CustomerReference` is deliberately a class, and it makes you write the
equality that `Money` gets for free. Neither choice is about speed.

### 2. Stack and heap, and why it is the wrong model to lean on

Use this mental model when you are reasoning about allocation *rate*, about
what the garbage collector will have to do, or about lifetime. Do not use it to
answer "is this a struct or a class", because it will mislead you.

The rule you were probably taught is that value types go on the stack and
reference types go on the heap. It is close enough to be useful and wrong often
enough to hurt. A struct that is a field of a class lives on the heap, inside
that object. A struct captured by a lambda lives on the heap, inside the
closure. A struct in an array lives in the array, on the heap. A struct that is
boxed lives on the heap by definition. The language specification does not
promise a stack at all; it talks about lifetime, and the runtime picks storage.

Three places the distinction genuinely matters. First, allocation rate: heap
allocations are individually cheap but collectively expensive, because they are
what eventually makes the collector run. Reducing allocations in a hot loop is
a real technique, and the `BoxingCosts` example measures it. Second, lifetime:
anything on the heap is kept alive by references to it, which is how a cache
that never evicts becomes a leak. Third, copying: a large struct is expensive
to pass around precisely because it is stored inline.

So the useful question is almost never "stack or heap". It is "how many of
these will exist, how long will they live, and how often will they be copied".
Those you can answer, and they lead to the same decisions with fewer wrong
turns.

### 3. `struct`, `readonly struct`, `ref struct`, and the copy you did not ask for

Use `readonly struct` for essentially every struct you write. Use a plain
`struct` when you genuinely need to mutate in place, which is rarer than it
sounds. Use `ref struct` only when the type must never reach the heap —
`Span<T>` is the reason it exists — and accept that it cannot be a field of a
class, boxed, or captured by a lambda.

`readonly` on a struct is a promise to the compiler that no member changes the
instance. That promise buys something concrete. When you pass a struct by `in`,
the compiler must guarantee the callee does not modify your copy. If the type
is not `readonly`, it cannot know that, so it makes a defensive copy at every
call — and `in`, added to avoid copying, silently starts copying. Mark the
struct `readonly` and the copy disappears.

The same trap appears with `readonly` fields. A `readonly` field of struct type
is copied every time you touch a member on it, for the same reason. Code that
looks like it mutates the field compiles, runs, and does nothing at all,
because it mutated a temporary copy that was then discarded.

This is the failure `ReservationWindow` is built to make impossible to write.
It is a `readonly struct`, `ExtendBy` returns a new window instead of changing
this one, and `Overlaps` takes its argument by `in`. Written that way there is
no copy to be surprised by and no mutation to lose.

If you take one habit from this section: make every struct `readonly` and every
struct member that does not need to mutate `readonly` too. The compiler will
tell you when you are wrong, and the cost of being right is one keyword.

### 4. The six places boxing hides

Boxing matters in loops that run often, in allocation-sensitive paths, and in
library code you do not control the call rate of. It does not matter in
start-up code or in a request handler that already talks to a database. Learn
to see it, then decide whether it is worth removing — in that order.

Boxing wraps a value in a heap object so it can be treated as a reference. The
cast is usually invisible. Six places it hides:

1. **Interface dispatch on a struct.** Calling a struct's interface method
   through the interface boxes it, unless a generic constraint lets the JIT
   specialise the call.
2. **Assignment to `object`.** The obvious one, and the rarest in real code.
3. **Non-generic collections.** `ArrayList`, `Hashtable`, and the older APIs
   that take `object`. One box per element.
4. **`params object[]`.** Every value-type argument boxed, plus the array.
   String formatting and logging APIs are full of these.
5. **`IComparable` without the generic constraint.** `IComparable` boxes;
   `IComparable<T>` as a constraint does not. One character of difference.
6. **LINQ over value types.** Anything that reaches `IEnumerable` rather than a
   concrete type, and anything that goes through a non-generic comparer.

Do not take this list on faith. Run
`dotnet run --project modules/01-type-system-and-memory/examples/BoxingCosts`
and read the bytes. It measures all six, then the same work written not to box.
The example also shows something worth knowing: writing site 1 as a local
variable makes the CA1859 analyser reject the build outright, while passing the
struct to a method taking the interface is the same boxing and no analyser
objects. The boxing that survives code review is the boxing one call frame
away from where you can see it.

### 5. `==`, `Equals`, `GetHashCode`, `IEquatable<T>`

Write these when your type is a value, when it will be a dictionary or hash-set
key, or when two instances holding the same data should be interchangeable. Do
not write them for a type with identity — an entity with an ID, a service, a
connection — where two instances holding the same data are still two things.

There are four members and they must agree. `Equals(object?)` is the base
contract. `IEquatable<T>.Equals(T?)` is the strongly-typed version that avoids
boxing when the type is a struct. `GetHashCode` decides which bucket the value
lands in. `==` is an operator, resolved at compile time, and it is not the same
question as `Equals`.

The rule that matters: whatever comparison `Equals` uses, `GetHashCode` must
use the same one. Equal objects must produce equal hash codes. Unequal objects
may collide — that is merely slow, not wrong. Get this backwards and a
dictionary silently misses: the value is in there, the lookup goes to the wrong
bucket, and nothing throws.

`CustomerReference` is this section as an exercise. Region is compared
case-insensitively, because the upstream system is inconsistent about casing.
That single fact is what makes `GetHashCode` interesting: it must hash
case-insensitively too, or `"eu"` and `"EU"` are equal but land in different
buckets. Satisfy `Equals` and not `GetHashCode` and the two dictionary tests
tell you immediately.

Run `dotnet run --project modules/01-type-system-and-memory/examples/EqualitySurprises`
before writing any of it. It shows the same pair of values compared four ways,
disagreeing. The last row is the one to sit with: two equal struct values,
boxed, where `==` says false and `Equals` says true.

### 6. Records, and the three times value equality is wrong

Reach for a record when the type is defined entirely by its data: a DTO, a
message, a key, a result. Do not reach for one when the type has identity,
behaviour that matters more than its data, or — and this is the one that
bites — a member that value equality cannot compare correctly.

A record gives you `Equals`, `GetHashCode`, `ToString`, `==` and a copy
constructor, generated correctly, for free. When it fits it is the best tool in
the language. Three cases where the generated version is wrong:

**A mutable member.** A record whose property can change is a key whose hash
can change. Put it in a dictionary, mutate it, and it is unreachable — still
there, still consuming memory, in a bucket that no longer matches its hash.

**A collection member.** This is the expensive one. The generated `Equals`
compares each member with the default equality comparer. For `List<T>` or an
array that is reference equality, because those types do not override `Equals`.
Two records holding structurally identical lists are therefore unequal, and
they hash differently. The code reads as value equality and behaves as
reference equality.

**An inherited record.** Record equality includes the runtime type, so a
derived instance never equals a base instance even when every field matches.
That is usually right and occasionally surprising.

`BasketKey` is case two, and it is this module's real-world case. Below is what
it costs.

### 7. Strings and interning

Reach for interning when you have a small, bounded set of symbols appearing
enormously often — currency codes, tenant identifiers, column names off a wire
protocol — and you want reference comparison rather than character comparison.
Do not reach for `string.Intern` to do it.

Strings in .NET are immutable reference types with value equality: `==` is
overloaded to compare contents, which is why the string row in
`EqualitySurprises` looks unlike the class row. Literals in your source are
interned automatically by the runtime, so two identical literals really are the
same instance. A string built at run time — parsed, concatenated, read from a
socket — is not.

`string.Intern` puts a string into a runtime-wide table, and that table lives
for the life of the process. Nothing is ever removed. Interning strings from
untrusted or unbounded input is therefore a memory leak you cannot clean up,
and it is worse than an ordinary leak because no profiler will point at your
code — the references belong to the runtime.

`SymbolTable` is the honest version: the same canonicalisation, in a dictionary
you own, whose entries die when it does. Bounded, collectable, and testable.
The exercise is small; the judgement it teaches is the point.

### 8. Mutability, and `readonly` fields that are not

Default to immutable. Make the exceptions deliberate. Most of the equality and
copying problems in this module disappear entirely when values cannot change
after construction, and the ones that remain become easy to reason about.

`readonly` on a field means the reference cannot be reassigned. It says nothing
about what it points at. A `readonly List<string>` cannot be pointed at a
different list and can be cleared, added to and emptied all day. This is the
single most common misreading of the keyword.

For a struct field, `readonly` has the copying consequence from section 3: each
member access on it works against a temporary copy. For a reference field, it
is a promise about the variable and not about the object.

Real immutability needs the type to cooperate: get-only properties, no setters,
collections exposed as `IReadOnlyList<T>` over a copy rather than the live list
— and note that `IReadOnlyList<T>` is a read-only *view*, not a guarantee the
underlying list is not being mutated by whoever handed it to you. `init`
accessors and `readonly record struct` make this cheap to express.

The connection back to section 6 is direct: `BasketKey` holds an
`IReadOnlyList<LineItem>`. That interface prevents *you* from adding to it. It
does not make the list a value, it does not make equality structural, and it
does not stop the caller who still holds the concrete list from changing it
under you.

## Real-world case

**The mechanism.** A checkout service caches charges under an idempotency key
so that a retried request is recognised rather than charged again. The key is a
record: `BasketKey(string CustomerId, IReadOnlyList<LineItem> Lines)`. A record
gives value equality, which is exactly what a cache key needs, so the type
looks correct on review and every unit test written against a single instance
passes.

The generated `Equals` compares each member with the default equality comparer.
For `CustomerId`, a string, that is value comparison. For `Lines`, a
`List<LineItem>` behind an interface, it is reference comparison — `List<T>`
does not override `Equals`. The retry arrives over a new HTTP request, is
deserialised into a new list, and produces a key that is structurally identical
and referentially different. It compares unequal. It hashes differently. The
cache misses. A second charge is issued.

**The detection gap.** Nothing throws. No error is logged. The cache reports a
miss, which is an ordinary event it is designed to handle, so no metric moves.
Charge success rate stays flat, latency stays flat, error rate stays at zero.
The system is successfully doing the wrong thing, which is the category of bug
dashboards are worst at. It surfaces through customer complaints and card
disputes, days later, one at a time — and each one is investigated as an
individual incident rather than as a pattern, because that is what the shape of
the evidence suggests.

**What it costs.** These are named assumptions, not measurements; substitute
your own and the arithmetic still works. Take 4,000 orders a day and a 2% retry
rate — retries come from flaky mobile connections, impatient double-taps and
gateway timeouts, and 2% is not a pessimistic figure. That is 80 duplicate
charges a day. At an average basket of 85 and a chargeback fee of 15 per
dispute, each duplicate costs about 100 once refunded and disputed: roughly
8,000 a day, or 240,000 over a thirty-day window, before anyone counts support
time or the customers who simply leave.

Run it rather than believing it:

```bash
dotnet run --project modules/01-type-system-and-memory/examples/BasketBug
```

The example builds two structurally identical baskets, shows the plain record
missing the cache, then shows the same data with `Equals` and `GetHashCode`
written to agree hitting it. The fix is roughly fifteen lines: compare `Lines`
with `SequenceEqual` and build the hash from the same elements. That is the
whole repair, and it is exercise 4.

One decision inside that repair is worth making deliberately rather than
inheriting: `SequenceEqual` makes line *order* part of the key, so the same two
products added in the opposite order produce a different key and a retry in
that shape would not be recognised. For a retried HTTP request, which replays
an identical payload, that is the right call and the cheapest correct one.
Treating the basket as a set instead is defensible for a client that reorders
lines, but it costs an order-independent hash — sum or XOR the element hashes
rather than combining them in sequence — and it makes two genuinely different
baskets collide more often. `BasketKeyTests.Line_order_is_part_of_the_key`
pins whichever choice you make, which is the actual point: the failure here was
never the ordering, it was that nobody chose.

## Exercises

All eight live in `modules/01-type-system-and-memory`. Run the whole module
with `./run.sh test 01` — all 42 tests fail until you implement the stubs in
`src/Exercises`. Work one class at a time with `--filter-class`.

**Core**

1. **`Money`** — value equality on a `readonly record struct`, and refusing to
   add across currencies.
   `dotnet test --project modules/01-type-system-and-memory/tests/UnitTests --filter-class "*MoneyTests"`
2. **`CustomerReference`** — the full equality contract by hand, with a
   case-insensitive region so `GetHashCode` must match `Equals`.
   `dotnet test --project modules/01-type-system-and-memory/tests/UnitTests --filter-class "*CustomerReferenceTests"`
3. **`OrderTotals`** — make the sum allocate nothing. The test measures it.
   `dotnet test --project modules/01-type-system-and-memory/tests/UnitTests --filter-class "*OrderTotalsTests"`
4. **`BasketKey`** — the real-world case above, as a repair.
   `dotnet test --project modules/01-type-system-and-memory/tests/UnitTests --filter-class "*BasketKeyTests"`
5. **`ReservationWindow`** — a `readonly struct` that returns new windows
   instead of mutating in place.
   `dotnet test --project modules/01-type-system-and-memory/tests/UnitTests --filter-class "*ReservationWindowTests"`

**Challenge**

6. **`Comparisons`** — the same comparison with and without a generic
   constraint, one of which boxes.
   `dotnet test --project modules/01-type-system-and-memory/tests/UnitTests --filter-class "*ComparisonsTests"`
7. **`SymbolTable`** — interning without the leak.
   `dotnet test --project modules/01-type-system-and-memory/tests/UnitTests --filter-class "*SymbolTableTests"`
8. **`SkuList`** — a struct enumerator that `foreach` binds to by pattern.
   `dotnet test --project modules/01-type-system-and-memory/tests/UnitTests --filter-class "*SkuListTests"`

Reference solutions are in `src/Solutions`. Read them after you have a passing
test of your own, not before.

## Summary

What should have changed is the question you ask. "Is this a struct or a class"
is a question about storage and it leads to folklore. "What does the variable
hold, how many will exist, how long will they live, and how often will they be
copied" is a question about consequences, and it answers the first one on the
way past.

Three specifics worth keeping. Equality is a contract between four members, not
a method: satisfy one and not the others and a dictionary misses silently,
which is the worst way for anything to fail. Compiler-generated equality is
correct exactly as far as its members are values, and a collection member is
where that stops. Boxing is invisible by design, so you find it by measuring
rather than by reading.

And one habit: when a type is going to be a key, write a test that puts two
equal-but-distinct instances in a dictionary and looks one up with the other.
It is three lines and it catches the entire class of bug this module is about.

## Review questions

Answer before expanding. If you reach for the compiler, that is the answer.

**1. Where does a struct that is a field of a class live?**

<details><summary>Answer</summary>
On the heap, inside the object. "Value types go on the stack" is a rule of
thumb about local variables, not a property of value types.
</details>

**2. You mark a method's parameter `in` to avoid copying a struct. When does it copy anyway?**

<details><summary>Answer</summary>
When the struct is not declared `readonly`. The compiler cannot prove the
callee leaves it alone, so it defends with a copy at every call.
</details>

**3. Two records hold structurally identical `List<int>` members. Are they equal?**

<details><summary>Answer</summary>
No. Generated equality uses the default comparer per member, and for `List<T>`
that is reference equality. This is the module's real-world case.
</details>

**4. `Equals` is correct and `GetHashCode` always returns 1. What breaks?**

<details><summary>Answer</summary>
Nothing, correctness-wise — every lookup still finds its value. The dictionary
degrades to a linear scan, so it is a performance bug, not a wrong answer. The
reverse mistake, a hash that disagrees with `Equals`, gives wrong answers.
</details>

**5. Name a boxing site with no cast and no `object` in the source.**

<details><summary>Answer</summary>
Calling an interface method on a struct through the interface; `params
object[]`; a non-generic `IComparable` parameter; `ArrayList.Add`.
</details>

**6. Why is `string.Intern` on parsed input dangerous?**

<details><summary>Answer</summary>
The intern pool is runtime-wide and never collected. Unbounded input means an
unbounded, uncollectable table — and the references belong to the runtime, so
a profiler will not point at your code.
</details>

**7. A field is `readonly List<string>`. What can still change?**

<details><summary>Answer</summary>
Everything about the list's contents. `readonly` protects the reference, not
what it points at.
</details>

**8. When is `class` the right choice for a small immutable value?**

<details><summary>Answer</summary>
When it has identity, when it is polymorphic, or when it is passed through many
frames often enough that copying beats the one allocation. "Small and
immutable" argues for a struct; it does not settle it.
</details>

If any answer above surprised you, this module is not finished with you yet.
Go back to the section it came from before starting the next one — every later
module assumes this one, and the assumptions are silent.

## Resources

- [Choosing between class and struct](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/choosing-between-class-and-struct) — the official guidance, including the size threshold; short enough to read in full.
- [The truth about value types](https://ericlippert.com/2010/09/30/the-truth-about-value-types/) — Eric Lippert on why "stack and heap" is the wrong model, from someone who worked on the compiler.
- [Boxing and unboxing](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/types/boxing-and-unboxing) — the mechanics, with the IL, if you want to see the `box` instruction.
- [`Object.GetHashCode`](https://learn.microsoft.com/en-us/dotnet/api/system.object.gethashcode) — read the Remarks specifically; the contract is stated there and almost nowhere else.
- [Records](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record) — what the compiler generates, which is what you need to know to predict when it is wrong.
- [`ref struct`](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/ref-struct) — the restrictions and why they exist; relevant the moment you meet `Span<T>`.
- [`String.Intern`](https://learn.microsoft.com/en-us/dotnet/api/system.string.intern) — the Remarks say the pool is never collected; worth reading before using it.
