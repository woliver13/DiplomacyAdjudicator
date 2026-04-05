# DiplomacyAdjudicator

A stateless REST API and .NET class library for adjudicating the board game [Diplomacy](https://en.wikipedia.org/wiki/Diplomacy_(game)) (Avalon Hill). Resolves movement, retreat, and build phases with full compliance to the [Diplomacy Adjudicator Test Cases (DATC)](http://web.inter.nl.net/users/L.B.Kruijswijk/).

## Features

- Full DATC compliance (~170 test cases)
- Implements Kruijswijk's algorithm for resolving order dependency cycles
- Stateless HTTP API — callers own board state, the adjudicator resolves it
- Core logic independently usable as a NuGet library
- Standard ruleset (Avalon Hill 2000 edition), with historical editions planned

## Project Structure

```
src/
  DiplomacyAdjudicator.Core/    # Adjudication engine and domain models (NuGet library)
  DiplomacyAdjudicator.Api/     # ASP.NET Core Web API wrapper
tests/
  DiplomacyAdjudicator.Tests/   # xUnit DATC compliance test suite
```

## API

Base URL: `/api/v1/`

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/adjudicate/movement` | Resolve a full movement phase |
| POST | `/adjudicate/retreat` | Resolve a retreat phase |
| POST | `/adjudicate/build` | Resolve a build/disband phase |
| POST | `/validate/orders` | Validate orders without adjudicating |

### Example: Movement Phase

```http
POST /api/v1/adjudicate/movement
```

```json
{
  "ruleset": "standard_2000",
  "units": [
    { "type": "army", "power": "england", "province": "lon" },
    { "type": "fleet", "power": "england", "province": "nth" }
  ],
  "orders": [
    { "power": "england", "unit": "army", "province": "lon", "order": "move yor" },
    { "power": "england", "unit": "fleet", "province": "nth", "order": "support army lon move yor" }
  ],
  "supply_centers": {
    "england": ["lon", "lvp", "edi"]
  }
}
```

```json
{
  "orders": [
    { "power": "england", "unit": "army", "province": "lon", "order": "move yor", "result": "success", "reason": null }
  ],
  "dislodged_units": [],
  "next_phase": "fall_movement"
}
```

Every submitted order appears in the response. Orders are never silently dropped.

## Core Library Usage

```csharp
var adjudicator = new MovementAdjudicator();

var lon = new Province("lon");
var yor = new Province("yor");
var army = new Unit(UnitType.Army, Power.England, lon);
var move = new MoveOrder(army, yor);

var request = new MovementAdjudicationRequest(
    units: [army],
    orders: [move],
    supplyCenters: new Dictionary<Power, IReadOnlyList<Province>>
    {
        [Power.England] = [lon, new Province("lvp"), new Province("edi")]
    }
);

var result = adjudicator.Adjudicate(request);
// result.OrderResults[0].Outcome == OrderOutcome.Success
```

## Building

Requires [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0).

```bash
dotnet build
dotnet test
```

## Milestones

| # | Deliverable | Status |
|---|-------------|--------|
| M1 | Solution scaffold, domain models, map data | Done |
| M2 | Order parsing and validation | Pending |
| M3 | Movement adjudication (Kruijswijk's algorithm) | Pending |
| M4 | Retreat and build adjudication | Pending |
| M5 | Full DATC test suite passing | Pending |
| M6 | ASP.NET Core API endpoints | Pending |
| M7 | Historical ruleset version support | Pending |
| M8 | NuGet package publication | Pending |

## License

MIT
