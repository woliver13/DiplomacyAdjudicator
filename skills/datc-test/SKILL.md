---
name: datc-test
description: >
  Write DATC (Diplomacy Adjudicator Test Cases) compliance tests for the
  DiplomacyAdjudicator project. Use this skill when the user asks to implement
  a DATC test case by number (e.g. "add DATC 6.A.1"), to cover a DATC section
  (e.g. "implement section 6.B"), or says "add a compliance test". This skill
  enforces the project's naming convention, exact domain type usage, citation
  comment format, file placement, and assertion style.
---

# DATC Compliance Tests

## Project types — use these exactly

```csharp
// Province: lowercase code string. Bicoastal suffixes: spa_nc, spa_sc, bul_ec, bul_sc, stp_nc, stp_sc
new Province("lon")

// Power: static instances
Power.England  Power.France  Power.Germany
Power.Italy    Power.Austria Power.Russia  Power.Turkey

// Unit
new Unit(UnitType.Army,  Power.England, new Province("lon"))
new Unit(UnitType.Fleet, Power.France,  new Province("bre"))

// Orders
new HoldOrder(unit)
new MoveOrder(unit, new Province("yor"))
new SupportHoldOrder(unit, new Province("yor"))           // support the unit IN yor
new SupportMoveOrder(unit, new Province("lon"), new Province("yor"))  // support lon→yor
new ConvoyOrder(unit, new Province("lon"), new Province("yor"))       // convoy lon→yor

// Request
new MovementAdjudicationRequest(units, orders, supplyCenters)
// units:         IReadOnlyList<Unit>
// orders:        IReadOnlyList<Order>
// supplyCenters: IReadOnlyDictionary<Power, IReadOnlyList<Province>>

// Adjudicator (instantiate per test class, or use a shared field)
private readonly MovementAdjudicator _adjudicator = new();

// Result types
MovementAdjudicationResult result = _adjudicator.Adjudicate(request);
result.OrderResults    // IReadOnlyList<OrderResult>
result.DislodgedUnits  // IReadOnlyList<DislodgedUnit>  — Unit + RetreatOptions
result.NextPhase       // PhaseType enum: Movement | Retreat | Build

// OrderOutcome values
OrderOutcome.Success | Failure | Bounced | Cut | Void | Dislodged | NoOrder
```

---

## Naming convention

**Method name:** `DATC_<section>_<subsection>_<number>_<PascalCaseDescription>`

```csharp
// DATC 6.A.1 — Moving to an area that is not a neighbor
[Fact]
public void DATC_6_A_1_MovingToAnAreaThatIsNotANeighbor() { ... }

// DATC 6.B.3 — A fleet can not cut support of its own country
[Fact]
public void DATC_6_B_3_AFleetCanNotCutSupportOfItsOwnCountry() { ... }
```

**File:** one file per subsection, in `tests/DiplomacyAdjudicator.Tests/Adjudication/`

```
DATC_6_A_Tests.cs   ← all 6.A cases
DATC_6_B_Tests.cs   ← all 6.B cases
DATC_6_C_Tests.cs   ← all 6.C cases
```

---

## Test structure

Every test follows this exact template:

```csharp
[Fact]
public void DATC_<S>_<SS>_<N>_<Description>()
{
    // DATC <S>.<SS>.<N> — <exact title from the DATC document>

    // Arrange
    var <province> = new Province("<code>");
    // ... declare all provinces used

    var <unit> = new Unit(UnitType.<Army|Fleet>, Power.<Power>, <province>);
    // ... declare all units

    var units = new List<Unit> { <unit>, ... };

    var <order> = new <OrderType>(<unit>, ...);
    // ... declare all orders
    var orders = new List<Order> { <order>, ... };

    var supplyCenters = new Dictionary<Power, IReadOnlyList<Province>>
    {
        [Power.<Power>] = new[] { <sc1>, <sc2>, ... },
        // include every power that has a unit on the board
    };

    var request = new MovementAdjudicationRequest(units, orders, supplyCenters);

    // Act
    var result = _adjudicator.Adjudicate(request);

    // Assert
    Assert.Equal(OrderOutcome.<Expected>, result.OrderResults.Single(r => r.Order == <order>).Outcome);
    // one Assert.Equal per order whose outcome the DATC case specifies
}
```

---

## Supply centers

Include realistic supply centers for every power that has a unit in the scenario.
You do not need to list every SC in the game — only the ones relevant to keep
the game state coherent. When in doubt, give each power their standard home SCs.

Standard home supply centers (lowercase province codes):

| Power   | Home SCs |
|---------|----------|
| England | lon, lvp, edi |
| France  | par, bre, mar |
| Germany | ber, mun, kie |
| Italy   | rom, ven, nap |
| Austria | vie, bud, tri |
| Russia  | mos, stp, war, sev |
| Turkey  | con, ank, smy |

---

## Asserting dislodgements

When the DATC case specifies that a unit is dislodged:

```csharp
// Assert the order outcome is Dislodged
Assert.Equal(OrderOutcome.Dislodged, result.OrderResults.Single(r => r.Order == fleetOrder).Outcome);

// Assert the unit appears in DislodgedUnits
var dislodged = result.DislodgedUnits.Single(d => d.Unit == fleet);
// optionally check retreat options
Assert.Contains(new Province("ret"), dislodged.RetreatOptions);
```

---

## Example: DATC 6.A.1

```csharp
[Fact]
public void DATC_6_A_1_MovingToAnAreaThatIsNotANeighbor()
{
    // DATC 6.A.1 — Moving to an area that is not a neighbor

    // Arrange
    var lon = new Province("lon");
    var syr = new Province("syr");   // not adjacent to lon

    var army = new Unit(UnitType.Army, Power.England, lon);
    var units = new List<Unit> { army };

    var move = new MoveOrder(army, syr);
    var orders = new List<Order> { move };

    var supplyCenters = new Dictionary<Power, IReadOnlyList<Province>>
    {
        [Power.England] = new[] { lon, new Province("lvp"), new Province("edi") }
    };

    var request = new MovementAdjudicationRequest(units, orders, supplyCenters);

    // Act
    var result = _adjudicator.Adjudicate(request);

    // Assert
    Assert.Equal(OrderOutcome.Failure, result.OrderResults.Single(r => r.Order == move).Outcome);
}
```

---

## Running the tests

```bash
# Run a single DATC case by method name
dotnet test --project tests/DiplomacyAdjudicator.Tests --filter "FullyQualifiedName~DATC_6_A_1" --nologo -v q 2>&1 | grep -E "^\s*(Failed|Passed|Error|error CS)"

# Run all cases in a subsection
dotnet test --project tests/DiplomacyAdjudicator.Tests --filter "FullyQualifiedName~DATC_6_A" --nologo -v q 2>&1 | grep -E "^\s*(Failed|Passed|Error|error CS)"

# Run all DATC tests
dotnet test --project tests/DiplomacyAdjudicator.Tests --filter "FullyQualifiedName~DATC_" --nologo -v q 2>&1 | grep -E "^\s*(Failed|Passed|Error|error CS)"
```
