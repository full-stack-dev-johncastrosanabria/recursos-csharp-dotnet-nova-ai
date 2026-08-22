# Start here

## 1. Check your environment

```bash
dotnet --version
```

This must report a `10.0.2xx` SDK — for example `10.0.201`. The repository
pins its SDK band in `global.json` with `rollForward: latestFeature`, so any
`10.0.2xx` or later patch within that band works; an older `10.0.1xx` SDK
will not satisfy it. If you don't have .NET 10 installed, get it from
[dotnet.microsoft.com/download](https://dotnet.microsoft.com/download).

Docker is optional for most of the path — see the Docker split in
[README.md](README.md) — but install it now if you want the integration
tier available from day one.

## 2. Clone the repository

```bash
git clone https://github.com/full-stack-dev-johncastrosanabria/recursos-csharp-dotnet-nova-ai.git
cd recursos-csharp-dotnet-nova-ai
```

## 3. Run the tests. Watch them fail.

```bash
./run.sh test
```

This runs every module's unit tier — no argument means all modules, not
just one. On a fresh clone, this **fails**. The failure looks like a test
throwing `NotImplementedException` from inside an exercise stub, not a build
error or a missing dependency: the code compiles, the test runs, and it hits
a method body that is nothing but `throw new NotImplementedException();`.

## 4. What red means

That failure is the whole point. Every exercise in every module starts as a
stub: a method signature with a body that throws. The test file next to it
already defines the contract — what the method should return, how it should
behave at the edges — and your job is to make that test pass by implementing
the method. Green is not the starting state; green is what you're working
towards, one exercise at a time.

If a module's tests are all green without you having written anything, that
is not success — it means you're looking at `src/Solutions` instead of
`src/Exercises`, or something is wrong. The two projects have an identical
public API on purpose (checked by `Training.Audit` in CI), specifically so
you can't tell them apart except by opening the file and reading the body.

Once you've implemented an exercise, rerun `./run.sh test NN` (with the
module's two-digit number) to check it. Use `./run.sh status` to see, across
every module, how many exercises you've solved. If you want to discard your
attempt at a module and start it again, `./run.sh reset NN` restores that
module's stubs from `HEAD` after asking you to confirm.

## 5. The toolchain — and why Shouldly, not FluentAssertions

Tests in this repo use `xunit.v3` on Microsoft Testing Platform, `Shouldly`
for assertions, and `NSubstitute` where a test needs a fake. If you've
written .NET tests before, you were probably expecting FluentAssertions
instead of Shouldly. Here is why it isn't used here, stated plainly.

FluentAssertions' current licence is the **Xceed Community License**, which
permits **non-commercial use only** and explicitly excludes use "by or for"
an organisation that charges fees or earns revenue. Starting with version 8,
commercial use requires a paid, per-developer licence. A training repository
whose default `dotnet test` command would put a team's employer on the hook
for a commercial licence is not something this repo will ship — the
obvious, familiar default was not chosen because it silently changes what
your company owes.

Shouldly is licensed **BSD-3-Clause**, unrestricted for commercial use, and
that's the reason it's the assertion library throughout this repo. The API
reads similarly — `result.ShouldBe(expected)` instead of
`result.Should().Be(expected)` — so the switch costs you a few minutes of
muscle memory, not a redesign of how you write a test.
