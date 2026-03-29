# Product Requirements Document: DiplomacyAdjudicator

## Overview

DiplomacyAdjudicator is a stateless REST API and companion .NET class library for adjudicating the board game Diplomacy (Avalon Hill). It resolves all phases of the standard ruleset with full compliance to the Diplomacy Adjudicator Test Cases (DATC), and is designed to serve as a reusable backend for game clients, bots, and simulation tools.

## Goals

- Provide a correct, DATC-compliant adjudication engine for the Diplomacy standard ruleset
- Expose adjudication as a stateless HTTP API so any client can integrate without owning resolution logic
- Package the core logic as a NuGet library for direct embedding in .NET applications
- Support multiple versions of the standard ruleset over time
- Be open source (MIT) and suitable for use by the broader Diplomacy software ecosystem

## Non-Goals

- Variant maps (Gunboat, Fleet Rome, Modern Diplomacy, etc.) — out of scope for v1
- Game state persistence — the API is stateless; callers own and store board state
- Player authentication, session management, or matchmaking
- Frontend or game client UI
- Rate limiting or API key enforcement (infrastructure hook only in v1)

---

## Solution Structure

```
DiplomacyAdjudicator.Core       Class library containing all adjudication logic
DiplomacyAdjudicator.Api        ASP.NET Core Web API wrapping the Core library
DiplomacyAdjudicator.Tests      xUnit test project; primary suite is full DATC compliance
```

The `Core` library must be independently usable without the API layer, and must be publishable as a NuGet package.

---

## Rulesets

### Supported at Launch
- **Standard ruleset, current edition** (Avalon Hill 2000 printing)

### Versioning Model
- Additional standard ruleset versions (e.g., 1971, 1976, 1982 editions) will be added over time
- Ruleset version is specified by the caller in the request body via a `"ruleset"` field
- If omitted, the API defaults to the latest standard ruleset
- API versioning (`/api/v1/`) is independent of ruleset versioning and tracks breaking changes to the API contract

---

## API Specification

### Base URL
```
/api/v1/
```

### Endpoints

#### `POST /api/v1/adjudicate/movement`
Resolves a full movement phase given all units and their orders.

**Request body:**
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
    "england": ["lon", "lvp", "edi"],
    "france": ["par", "bre", "mar"]
  }
}
```

**Response body:**
```json
{
  "orders": [
    {
      "power": "england",
      "unit": "army",
      "province": "lon",
      "order": "move yor",
      "result": "success",
      "reason": null
    },
    {
      "power": "france",
      "unit": "army",
      "province": "par",
      "order": "move bur",
      "result": "bounced",
      "reason": "Opposed by army mar with equal support"
    }
  ],
  "dislodged_units": [
    { "type": "army", "power": "france", "province": "bre", "retreat_options": ["pic", "gas"] }
  ],
  "next_phase": "retreat"
}
```

**`next_phase` values:** `"retreat"` | `"fall_movement"` | `"build"`

---

#### `POST /api/v1/adjudicate/retreat`
Resolves a retreat phase given dislodged units and their retreat orders.

**Request body:**
```json
{
  "ruleset": "standard_2000",
  "dislodged_units": [
    {
      "type": "army",
      "power": "france",
      "province": "bre",
      "retreat_options": ["pic", "gas"],
      "order": "retreat pic"
    }
  ]
}
```

**Response body:**
```json
{
  "orders": [
    {
      "power": "france",
      "unit": "army",
      "province": "bre",
      "order": "retreat pic",
      "result": "success",
      "reason": null
    }
  ],
  "disbanded_units": [],
  "next_phase": "fall_movement"
}
```

---

#### `POST /api/v1/adjudicate/build`
Resolves a build/disband phase at year end.

**Request body:**
```json
{
  "ruleset": "standard_2000",
  "units": [
    { "type": "army", "power": "england", "province": "lon" }
  ],
  "supply_centers": {
    "england": ["lon", "lvp", "edi", "yor"]
  },
  "home_centers": {
    "england": ["lon", "lvp", "edi"]
  },
  "orders": [
    { "power": "england", "order": "build army lvp" }
  ]
}
```

**Response body:**
```json
{
  "orders": [
    {
      "power": "england",
      "order": "build army lvp",
      "result": "success",
      "reason": null
    }
  ],
  "next_phase": "spring_movement"
}
```

---

#### `POST /api/v1/validate/orders`
Validates a set of orders against the current board state without adjudicating. Accepts partial order sets (e.g., a single order being composed in a UI).

**Request body:**
```json
{
  "ruleset": "standard_2000",
  "units": [...],
  "orders": [
    { "power": "england", "unit": "army", "province": "lon", "order": "move par" }
  ]
}
```

**Response body:**
```json
{
  "orders": [
    {
      "power": "england",
      "unit": "army",
      "province": "lon",
      "order": "move par",
      "valid": false,
      "reason": "Army in lon is not adjacent to par"
    }
  ]
}
```

---

### Error Handling

| Condition | HTTP Status | Behavior |
|---|---|---|
| Malformed JSON | 400 | Request rejected with error detail |
| Unknown province code | 400 | Request rejected, unknown codes listed |
| Unknown ruleset version | 400 | Request rejected with supported versions listed |
| Void order (e.g., support a non-existent move) | 200 | Adjudicated as hold; annotated in response |
| Illegal order (e.g., ordering a unit you don't own) | 200 | Adjudicated as hold; annotated in response |
| Syntactically invalid order string | 200 | Adjudicated as hold; annotated in response |

Orders are never silently dropped. Every submitted order appears in the response with its interpreted result and reason.

---

## Domain Model (Core Library)

### Provinces
- Represented as canonical lowercase string codes: `"lon"`, `"bre"`, `"spa_nc"`, `"spa_sc"`, `"bul_ec"`, `"bul_sc"`, `"stp_nc"`, `"stp_sc"`
- Bicoastal provinces have distinct coast identifiers for fleet movement
- Map topology (adjacency, supply center status, home center ownership, land/sea/coast classification) is defined in `standard_map.json`, loaded at startup

### Units
- **Type:** `army` | `fleet`
- **Power:** lowercase power name (`"england"`, `"france"`, etc.)
- **Province:** string code of current location

### Orders (Movement Phase)
| Order Type | Example |
|---|---|
| Hold | `"hold"` |
| Move | `"move yor"` |
| Support hold | `"support army yor"` |
| Support move | `"support army lon move yor"` |
| Convoy | `"convoy army lon move yor"` |

### Orders (Retreat Phase)
| Order Type | Example |
|---|---|
| Retreat | `"retreat pic"` |
| Disband | `"disband"` |

### Orders (Build Phase)
| Order Type | Example |
|---|---|
| Build | `"build army lon"` |
| Disband | `"disband army lon"` |
| Waive | `"waive"` |

---

## Adjudication Algorithm

The Core library implements **Kruijswijk's algorithm** as specified in the DATC reference document. This algorithm resolves dependency cycles in order resolution (including convoy paradoxes, circular movement, and self-dislodgement cases) iteratively and is proven correct for all standard ruleset cases.

### Adjudication Phases
1. **Order parsing and validation** — parse order strings, classify by type, identify void orders
2. **Dependency graph construction** — identify which orders depend on the success/failure of others
3. **Iterative resolution** — apply Kruijswijk's algorithm to resolve all dependencies
4. **Result annotation** — assign outcome and reason to each order
5. **State delta computation** — determine dislodged units, successful moves, and next phase

---

## Correctness Requirements

- The test suite must include all DATC test cases (~170 cases)
- Every DATC case must pass before any release
- DATC cases are implemented as xUnit `[Theory]` tests driven from a data file (JSON or YAML) to allow easy addition of new cases
- The adjudication result for any position must be deterministic and idempotent

---

## Map Data

The standard Diplomacy map is defined in `DiplomacyAdjudicator.Core/Data/standard_map.json` and includes:

- All 75 provinces with type (`land` | `sea` | `coast`), display name, and string code
- Adjacency list per province (with coast qualifiers for bicoastal provinces)
- Supply center flag per province
- Home center ownership per power
- Starting unit positions per power

The map is loaded once at application startup and held in memory as an adjacency graph. No database is required.

---

## Authentication & Rate Limiting

- v1 ships with no authentication enforcement
- The ASP.NET Core middleware pipeline must include a no-op authentication hook that can be replaced with API key enforcement in a future version without restructuring the pipeline
- No rate limiting in v1

---

## Licensing & IP

- Published under the **MIT License**
- The adjudication logic, map topology data, and API contract are original work
- No Avalon Hill or Hasbro artwork, map images, or trademarked assets are included
- Game mechanics are not copyrightable; this project adjudicates rules, not reproduces artwork

---

## Milestones

| Milestone | Deliverable |
|---|---|
| M1 | Solution scaffold, domain models, map data file |
| M2 | Order parsing and validation (all phases) |
| M3 | Movement adjudication (Kruijswijk's algorithm) |
| M4 | Retreat and build adjudication |
| M5 | Full DATC test suite passing |
| M6 | ASP.NET Core API layer with all four endpoints |
| M7 | Prior ruleset version support (first historical edition) |
| M8 | NuGet package publication |
