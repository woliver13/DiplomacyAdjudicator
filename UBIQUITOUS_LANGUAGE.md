# Ubiquitous Language

## Board entities

| Term | Definition | Aliases to avoid |
|------|------------|------------------|
| **Power** | One of the seven nations that controls units (Austria, England, France, Germany, Italy, Russia, Turkey) | Player, nation, country, side |
| **Province** | A named region on the map, identified by its canonical lowercase code (e.g. `"lon"`, `"spa_nc"`) | Territory, location, tile, space |
| **Supply Center** | A province that grants unit production rights when controlled at year end | SC, production center |
| **Home Center** | A supply center where a power may build new units; fixed per power | Build location, origin, home supply center |
| **Unit** | An army or fleet controlled by a power and occupying a province | Piece, token, troop |
| **Army** | A unit that moves overland only | Ground unit, land unit |
| **Fleet** | A unit that moves along sea and coast provinces | Naval unit, ship, boat |

## Orders

| Term | Definition | Aliases to avoid |
|------|------------|------------------|
| **Order** | A command submitted by a power for one of its units during a phase | Instruction, command, action |
| **Hold** | An order directing a unit to remain in its province | Defend, stay, fortify |
| **Move** | An order directing a unit to advance to an adjacent province | Attack, advance, march |
| **Support Hold** | An order directing a unit to reinforce a friendly or neutral unit holding its province | Defend support, static support |
| **Support Move** | An order directing a unit to reinforce a friendly unit's move from an origin to a destination | Attack support, dynamic support |
| **Convoy** | An order directing a fleet to transport an army across a sea route | Transport, ferry |
| **Retreat** | An order directing a dislodged unit to move to a valid refuge province | Escape, flee |
| **Disband** | An order removing a unit from the board (retreat phase or build phase) | Destroy, eliminate, remove |
| **Build** | An order constructing a new unit at a home center during the build phase | Place, create, spawn |
| **Waive** | An order declining an available build during the build phase | Pass, skip |

## Adjudication outcomes

| Term | Definition | Aliases to avoid |
|------|------------|------------------|
| **Success** | The order executed as intended | Approved, resolved, accepted |
| **Failure** | A valid order that was adjudicated and lost (e.g. a move opposed with equal or greater force) | Rejected, blocked — and never confused with **Void** |
| **Bounced** | Mutual failure: two or more units attempted to enter the same province and all were turned back | Collision, deadlock |
| **Cut** | A support order that failed because the supporting unit was attacked | Broken, interrupted |
| **Dislodged** | A unit forced out of its province by a successful enemy move | Evicted, displaced |
| **Void** | An order that is illegal before adjudication (e.g. ordering a unit you don't own, moving to a non-adjacent province); treated as Hold | Invalid, illegal — and never confused with **Failure** |
| **NoOrder** | A unit that had no order submitted; treated as Hold | Missing order, unordered |

## Phases

| Term | Definition | Aliases to avoid |
|------|------------|------------------|
| **Movement Phase** | The phase in which units hold, move, support, and convoy | Action phase, order phase |
| **Retreat Phase** | The phase in which dislodged units retreat or disband | Escape phase |
| **Build Phase** | The year-end phase in which powers build or disband units to match their supply center count | Adjustment phase, winter phase |
| **Spring** | The first movement phase of a game year | — |
| **Fall** | The second movement phase of a game year | Autumn |

## Adjudication

| Term | Definition | Aliases to avoid |
|------|------------|------------------|
| **Adjudicate** | Resolve all orders for a phase and produce outcomes | Process, calculate, run |
| **Validate** | Check order legality without adjudicating; returns errors but does not resolve | Verify, check |
| **Bounce** (verb) | Two or more units holding each other back from a contested province | Collide, deadlock |
| **Dislodge** (verb) | A unit forcing another out of its province through a successful move | Evict, displace |
| **Cut** (verb) | An attack that severs a support order | Break, interrupt |
| **Void** (verb) | Classify an order as illegal and treat it as Hold | Invalidate, reject |

## Relationships

- A **Power** controls zero or more **Units** and zero or more **Supply Centers** at any given time.
- A **Unit** occupies exactly one **Province** and belongs to exactly one **Power**.
- Every **Unit** has exactly one **Order** per phase; if none is submitted it is treated as **Hold** (outcome: **NoOrder**).
- A **Dislodged** unit enters the **Retreat Phase** with a set of valid retreat provinces; if none exist it is automatically **Disbanded**.
- A **Supply Center** is a **Home Center** for exactly one **Power**; it may be controlled by a different **Power** at runtime.
- **Void** and **Failure** are mutually exclusive: **Void** means illegal before resolution; **Failure** means valid but lost during resolution.

## Example dialogue

> **Dev:** "If England submits a **Move** to a non-adjacent province, is that a **Failure**?"
> **Domain expert:** "No — it's **Void**. A **Void** order never enters adjudication; the engine treats it as a **Hold** immediately. **Failure** is reserved for a legal move that lost."
>
> **Dev:** "Got it. And if two units both **Move** into Paris at the same time?"
> **Domain expert:** "Both are **Bounced** — they cancel each other out. Neither unit moves, and neither **Dislodges** the other."
>
> **Dev:** "What if France is already in Paris and England attacks with more **Support**?"
> **Domain expert:** "England **Dislodges** France. France's unit is now in the **Retreat Phase** with whatever valid retreat provinces remain. If there are none, it's automatically **Disbanded**."
>
> **Dev:** "Can France **Cut** England's support before the move resolves?"
> **Domain expert:** "Only if France attacks the province of the supporting unit. That **Cuts** the support, reducing England's strength, which may be enough to stop the **Dislodgement**."

## Flagged ambiguities

- **"Support"** is used informally for both **Support Hold** and **Support Move**, which are distinct order types with different adjacency requirements. Always qualify which kind of support is meant.
- **"Hold"** is overloaded: it is both an order type (**Hold** order) and a default behaviour (any **Void** or **NoOrder** unit effectively holds). In code, distinguish `HoldOrder` (explicit player command) from the implicit hold treatment of **Void** / **NoOrder** results.
- **"next_phase"** in the API response uses strings like `"fall_movement"` and `"spring_movement"`, while the domain enum `PhaseType` only has `Movement`, `Retreat`, `Build`. The API strings encode both phase type and season; the domain model separates them via `GamePhase(Season, Year, PhaseType)`. Do not conflate the two representations.
