---
name: dotnet-tdd
description: >
  Test-driven development for .NET/C# projects using xUnit. Use this skill
  whenever the user asks to implement something with TDD, asks to "/tdd" a
  feature, or says things like "red-green-refactor", "write it test-first",
  or "add tests first". Also use it when the user asks to add a class or method
  and the project already has an xUnit test project. This skill changes how
  dotnet test is run (always with --filter + grep), enforces explicit
  RED→GREEN decision points, and requires a cycle summary line and a
  "Refactor or next cycle?" checkpoint after every GREEN.
---

# .NET TDD — Red → Green → Refactor

## Before writing any code: plan and confirm

List the cycles you intend to run — one row per behaviour — and stop. Do
not write any test or production code yet. Example:

```
Planned cycles:
1. Returns number as string (plain case)
2. Multiples of 3 → "Fizz"
3. Multiples of 5 → "Buzz"
4. Multiples of 15 → "FizzBuzz"
```

Adjust based on the task, then proceed cycle by cycle.

---

## The only command for running tests

Every time you run tests — for RED, for GREEN, for REFACTOR — use this form:

```bash
dotnet test [--project <path>] --filter "FullyQualifiedName~<TestMethodName>" --nologo -v q 2>&1 | grep -E "^\s*(Failed|Passed|Error|error CS)"
```

- `--filter` scopes the run to the test you just wrote. Fill in the method name.
- The `grep` strips MSBuild noise so only pass/fail lines appear.
- `--project <path>` when the user has given you a test project path or there are multiple test projects.
- Only run bare `dotnet test` (no filter) for the very first compile check, and even then pipe through grep.

**Why this matters:** running the full suite on every cycle defeats the purpose of
cycling — it couples unrelated tests, hides the signal in noise, and slows feedback.

### Reading the output

- A compile error line (`error CS…`) is a valid RED — write that test, see the compile error, then add production code.
- `Failed!` with the test name is a runtime RED.
- `Passed!` is GREEN. Stop, write the summary line, ask about refactoring.

---

## Each cycle follows this exact sequence

### 1. RED — write the failing test

Write one test. Run it with `--filter`. Confirm you see a failure (compile error or assertion failure) before touching production code. Show the failure output.

### 2. GREEN — minimum production code

Add only enough code to make *this* test pass. Run again with `--filter`. Confirm `Passed!`.

Then immediately output this summary line:

```
Cycle N ✓ <TestMethodName> (total passing: X)
```

Fill in the cycle number, the test method name, and the running total of passing tests.

### 3. REFACTOR checkpoint — ask before proceeding

After every GREEN, stop and ask:

> Refactor or next cycle?

Wait for the answer before writing any more code. If refactoring: make the change, run **all** tests (`dotnet test --nologo -v q 2>&1 | grep -E "Failed|Passed|error CS"`), confirm everything still passes, then ask again. If next cycle: start the next row from your plan.

---

## [Theory] / [InlineData]

When a test checks the same logic across 3 or more different inputs, use
`[Theory]` with `[InlineData]` instead of separate `[Fact]` methods.

```csharp
[Theory]
[InlineData(3,  "Fizz")]
[InlineData(6,  "Fizz")]
[InlineData(9,  "Fizz")]
public void Returns_Fizz_for_multiples_of_3(int n, string expected)
{
    Assert.Equal(expected, FizzBuzz.Evaluate(n));
}
```

Run with `--filter "FullyQualifiedName~Returns_Fizz"` — xUnit runs all InlineData
cases that match, so you still get focused output.

---

## Working directory

Run all commands from the solution root (where the `.sln` file lives), not
from inside a project directory.

---

## Naming conventions

Test methods: `<Behaviour>_<condition>` in PascalCase snake-hybrid, e.g.
`Returns_Fizz_for_multiples_of_3`. This gives readable `--filter` targets.
