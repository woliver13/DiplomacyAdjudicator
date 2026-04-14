using woliver13.DiplomacyAdjudicator.Core.Domain;
using Xunit;

namespace woliver13.DiplomacyAdjudicator.Tests.Adjudication;

/// <summary>
/// DATC v2.4 Section 6.A — Basic Checks
/// Source: zond/godip datc_v2.4_06.txt (Lucas B. Kruijswijk)
/// </summary>
public class DATC_6_A_Tests
{
    // 6.A.1 — Moving to an area that is not a neighbour
    // F NTH - PIC: NTH is not adjacent to PIC without convoy
    [Fact]
    public void DATC_6_A_1_MovingToAnAreaThatIsNotANeighbour()
    {
        new AdjudicationScenario()
            .WithUnit("england", "fleet", "nth")
            .WithOrder("england", "fleet", "nth", "move pic")
            .AssertOutcome("nth", OrderOutcome.Failure)
            .Run();
    }

    // 6.A.2 — Army cannot move to sea
    // A LVP - IRI: armies may not enter sea provinces
    [Fact]
    public void DATC_6_A_2_ArmyCannotMoveToSea()
    {
        new AdjudicationScenario()
            .WithUnit("england", "army", "lvp")
            .WithOrder("england", "army", "lvp", "move iri")
            .AssertOutcome("lvp", OrderOutcome.Failure)
            .Run();
    }

    // 6.A.3 — Fleet cannot move to land
    // F KIE - MUN: fleets may not enter inland provinces
    [Fact]
    public void DATC_6_A_3_FleetCannotMoveToLand()
    {
        new AdjudicationScenario()
            .WithUnit("germany", "fleet", "kie")
            .WithOrder("germany", "fleet", "kie", "move mun")
            .AssertOutcome("kie", OrderOutcome.Failure)
            .Run();
    }

    // 6.A.3 (extended) — Fleet support of inland province is void
    // F TRI S A BUD: TRI cannot reach BUD (fleet can't enter inland)
    // A GAL - BUD supported by A RUM succeeds
    [Fact]
    public void DATC_6_A_3_FleetSupportOfInlandProvinceIsVoid()
    {
        new AdjudicationScenario()
            .WithUnit("austria", "fleet", "tri")
            .WithUnit("austria", "army", "bud")
            .WithUnit("russia", "army", "gal")
            .WithUnit("russia", "army", "rum")
            .WithOrder("austria", "fleet", "tri", "support army bud")
            .WithOrder("austria", "army", "bud", "hold")
            .WithOrder("russia", "army", "gal", "move bud")
            .WithOrder("russia", "army", "rum", "support army gal move bud")
            .AssertOutcome("gal", OrderOutcome.Success)
            .AssertDislodged("bud")
            .Run();
    }

    // 6.A.4 — Move to same sector is illegal
    // F KIE - KIE: a unit cannot be ordered to its own province
    [Fact]
    public void DATC_6_A_4_MoveToSameSectorIsIllegal()
    {
        new AdjudicationScenario()
            .WithUnit("germany", "fleet", "kie")
            .WithOrder("germany", "fleet", "kie", "move kie")
            .AssertOutcome("kie", OrderOutcome.Failure)
            .Run();
    }

    // 6.A.5 — Move to own sector even with convoy is illegal
    // A YOR - YOR is illegal; A LVP supports void; F NTH convoy is void
    // Germany F LON - YOR with A WAL support (strength 2) dislodges A YOR (strength 1)
    [Fact]
    public void DATC_6_A_5_MoveToOwnSectorWithConvoyIsIllegal()
    {
        new AdjudicationScenario()
            .WithUnit("england", "army", "yor")
            .WithUnit("england", "army", "lvp")
            .WithUnit("england", "fleet", "nth")
            .WithUnit("germany", "fleet", "lon")
            .WithUnit("germany", "army", "wal")
            .WithOrder("england", "fleet", "nth", "convoy army yor move yor")
            .WithOrder("england", "army", "yor", "move yor")
            .WithOrder("england", "army", "lvp", "support army yor move yor")
            .WithOrder("germany", "fleet", "lon", "move yor")
            .WithOrder("germany", "army", "wal", "support fleet lon move yor")
            .AssertOutcome("lon", OrderOutcome.Success)
            .AssertDislodged("yor")
            .Run();
    }

    // 6.A.8 — Army cannot get additional hold power by supporting itself
    // F TRI S F TRI is void (self-support); A VEN (attack 2) beats hold 1
    [Fact]
    public void DATC_6_A_8_ArmyCannotSupportItself()
    {
        new AdjudicationScenario()
            .WithUnit("italy", "army", "ven")
            .WithUnit("italy", "army", "tyr")
            .WithUnit("austria", "fleet", "tri")
            .WithOrder("italy", "army", "ven", "move tri")
            .WithOrder("italy", "army", "tyr", "support army ven move tri")
            .WithOrder("austria", "fleet", "tri", "support fleet tri")
            .AssertOutcome("ven", OrderOutcome.Success)
            .AssertDislodged("tri")
            .Run();
    }

    // 6.A.9 — Fleets must follow coasts (F ROM - VEN not adjacent by sea)
    [Fact]
    public void DATC_6_A_9_FleetsMustFollowCoasts()
    {
        new AdjudicationScenario()
            .WithUnit("italy", "fleet", "rom")
            .WithOrder("italy", "fleet", "rom", "move ven")
            .AssertOutcome("rom", OrderOutcome.Failure)
            .Run();
    }

    // 6.A.10 — Support on unreachable destination is void
    // F ROM cannot reach VEN (not in fleet adjacency list); support is void
    // A APU attack strength 1 vs A VEN hold strength 1 → bounce
    [Fact]
    public void DATC_6_A_10_SupportOnUnreachableDestinationIsVoid()
    {
        new AdjudicationScenario()
            .WithUnit("austria", "army", "ven")
            .WithUnit("italy", "fleet", "rom")
            .WithUnit("italy", "army", "apu")
            .WithOrder("austria", "army", "ven", "hold")
            .WithOrder("italy", "fleet", "rom", "support army apu move ven")
            .WithOrder("italy", "army", "apu", "move ven")
            .AssertOutcome("apu", OrderOutcome.Failure)
            .AssertOutcome("ven", OrderOutcome.Success)
            .Run();
    }

    // 6.A.11 — Simple bounce
    // A VIE - TYR and A VEN - TYR: equal strength, both bounce
    [Fact]
    public void DATC_6_A_11_SimpleBounce()
    {
        new AdjudicationScenario()
            .WithUnit("austria", "army", "vie")
            .WithUnit("italy", "army", "ven")
            .WithOrder("austria", "army", "vie", "move tyr")
            .WithOrder("italy", "army", "ven", "move tyr")
            .AssertOutcome("vie", OrderOutcome.Failure)
            .AssertOutcome("ven", OrderOutcome.Failure)
            .Run();
    }

    // 6.A.12 — Three-unit bounce
    // A VIE - TYR, A VEN - TYR, A MUN - TYR: all equal, all bounce
    [Fact]
    public void DATC_6_A_12_ThreeUnitBounce()
    {
        new AdjudicationScenario()
            .WithUnit("austria", "army", "vie")
            .WithUnit("italy", "army", "ven")
            .WithUnit("germany", "army", "mun")
            .WithOrder("austria", "army", "vie", "move tyr")
            .WithOrder("italy", "army", "ven", "move tyr")
            .WithOrder("germany", "army", "mun", "move tyr")
            .AssertOutcome("vie", OrderOutcome.Failure)
            .AssertOutcome("ven", OrderOutcome.Failure)
            .AssertOutcome("mun", OrderOutcome.Failure)
            .Run();
    }

    // Additional: successful move to empty province
    [Fact]
    public void Additional_SuccessfulMoveToEmptyProvince()
    {
        new AdjudicationScenario()
            .WithUnit("england", "army", "lon")
            .WithOrder("england", "army", "lon", "move yor")
            .AssertOutcome("lon", OrderOutcome.Success)
            .Run();
    }

    // Additional: supported move dislodges defended unit
    // A MUN - TYR (attack 2) vs A TYR H (hold 1) → MUN succeeds, TYR dislodged
    [Fact]
    public void Additional_SupportedMoveDislodgesDefendedUnit()
    {
        new AdjudicationScenario()
            .WithUnit("germany", "army", "mun")
            .WithUnit("germany", "army", "boh")
            .WithUnit("austria", "army", "tyr")
            .WithOrder("germany", "army", "mun", "move tyr")
            .WithOrder("germany", "army", "boh", "support army mun move tyr")
            .WithOrder("austria", "army", "tyr", "hold")
            .AssertOutcome("mun", OrderOutcome.Success)
            .AssertDislodged("tyr")
            .Run();
    }

    // Additional: support cut by attack (attacker at destination wins)
    // A PAR - BUR (attack 1) vs A MUN - BUR (attack 1 + cut support = attack 1)
    // A MAR S A PAR - BUR but A MUN attacks MAR → support cut → both bounce
    [Fact]
    public void Additional_SupportCutByAttack()
    {
        new AdjudicationScenario()
            .WithUnit("france", "army", "par")
            .WithUnit("france", "army", "mar")
            .WithUnit("germany", "army", "mun")
            .WithUnit("germany", "army", "ruh")
            .WithOrder("france", "army", "par", "move bur")
            .WithOrder("france", "army", "mar", "support army par move bur")
            .WithOrder("germany", "army", "mun", "move bur")
            .WithOrder("germany", "army", "ruh", "move mar")
            .AssertOutcome("par", OrderOutcome.Failure)
            .AssertOutcome("mun", OrderOutcome.Failure)
            .Run();
    }

    // Additional: three-unit circular movement — all succeed
    // F ANK - CON, A CON - SMY, A SMY - ANK
    [Fact]
    public void Additional_ThreeUnitCircularMovement()
    {
        new AdjudicationScenario()
            .WithUnit("turkey", "fleet", "ank")
            .WithUnit("turkey", "army", "con")
            .WithUnit("turkey", "army", "smy")
            .WithOrder("turkey", "fleet", "ank", "move con")
            .WithOrder("turkey", "army", "con", "move smy")
            .WithOrder("turkey", "army", "smy", "move ank")
            .AssertOutcome("ank", OrderOutcome.Success)
            .AssertOutcome("con", OrderOutcome.Success)
            .AssertOutcome("smy", OrderOutcome.Success)
            .Run();
    }

    // Additional: head-on collision, equal strength — both fail
    // A LON - YOR, A YOR - LON: head-on, defend strength 1 each
    [Fact]
    public void Additional_HeadOnCollisionBothFail()
    {
        new AdjudicationScenario()
            .WithUnit("england", "army", "lon")
            .WithUnit("france", "army", "yor")
            .WithOrder("england", "army", "lon", "move yor")
            .WithOrder("france", "army", "yor", "move lon")
            .AssertOutcome("lon", OrderOutcome.Failure)
            .AssertOutcome("yor", OrderOutcome.Failure)
            .Run();
    }

    // Additional: head-on collision, one supported — supported wins
    // A PAR - BUR (defend 2) vs A MUN - PAR (defend 1): PAR wins
    [Fact]
    public void Additional_HeadOnCollisionSupportedWins()
    {
        new AdjudicationScenario()
            .WithUnit("france", "army", "par")
            .WithUnit("france", "army", "pic")
            .WithUnit("germany", "army", "mun")
            .WithOrder("france", "army", "par", "move bur")
            .WithOrder("france", "army", "pic", "support army par move bur")
            .WithOrder("germany", "army", "mun", "move par")
            .AssertOutcome("par", OrderOutcome.Success)
            .AssertOutcome("mun", OrderOutcome.Failure)
            .Run();
    }
}
