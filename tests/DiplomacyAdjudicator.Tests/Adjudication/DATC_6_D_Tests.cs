using woliver13.DiplomacyAdjudicator.Core.Domain;
using Xunit;

namespace woliver13.DiplomacyAdjudicator.Tests.Adjudication;

/// <summary>
/// DATC v2.4 Section 6.D — Supports and Dislodges (selected cases)
/// </summary>
public class DATC_6_D_Tests
{
    // 6.D.1 — Supported unit holds off attack.
    // A TYR→VIE (attack 1) cannot dislodge A VIE (hold 2 with BUD support).
    [Fact]
    public void DATC_6_D_1_SupportedHoldStopsAttack()
    {
        new AdjudicationScenario()
            .WithUnit("austria", "army", "vie")
            .WithUnit("austria", "army", "bud")
            .WithUnit("germany", "army", "tyr")
            .WithOrder("austria", "army", "vie", "hold")
            .WithOrder("austria", "army", "bud", "support army vie")
            .WithOrder("germany", "army", "tyr", "move vie")
            .AssertOutcome("tyr", OrderOutcome.Failure)
            .AssertOutcome("vie", OrderOutcome.Success)
            .Run();
    }

    // 6.D.2 — Support is cut when supporter's province is attacked.
    // A SER attacks BUD, cutting BUD's support for VIE→TRI.
    // VIE→TRI then has attack 1 vs TRI hold 1 — bounces.
    [Fact]
    public void DATC_6_D_2_MoveCutsSupportOfAdjacentProvince()
    {
        new AdjudicationScenario()
            .WithUnit("austria", "army", "vie")
            .WithUnit("austria", "army", "bud")
            .WithUnit("austria", "army", "tri")
            .WithUnit("russia",  "army", "ser")
            .WithOrder("austria", "army", "vie", "move tri")
            .WithOrder("austria", "army", "bud", "support army vie move tri")
            .WithOrder("austria", "army", "tri", "hold")
            .WithOrder("russia",  "army", "ser", "move bud")
            .AssertOutcome("vie", OrderOutcome.Failure)
            .AssertOutcome("bud", OrderOutcome.Failure) // bounced by TRI hold; SER cuts BUD's support
            .AssertOutcome("tri", OrderOutcome.Success)
            .Run();
    }

    // 6.D.3 — A unit cannot dislodge a unit of the same power (national restriction).
    // A MAR→SPA: attacker and defender are both France.
    // GAS supports MAR→SPA but support excluded due to same nationality as SPA.
    // Attack strength = 1, hold = 1 → bounce.
    [Fact]
    public void DATC_6_D_3_SelfDislodgeImpossible()
    {
        new AdjudicationScenario()
            .WithUnit("france", "army", "mar")
            .WithUnit("france", "army", "gas")
            .WithUnit("france", "army", "spa")
            .WithOrder("france", "army", "mar", "move spa")
            .WithOrder("france", "army", "gas", "support army mar move spa")
            .WithOrder("france", "army", "spa", "hold")
            .AssertOutcome("mar", OrderOutcome.Failure)
            .AssertOutcome("spa", OrderOutcome.Success)
            .Run();
    }

    // 6.D.4 — Support cannot be cut by the unit being supported against.
    // F ANK→BLA, F CON supports ANK→BLA, F BLA→CON (tries to cut CON support).
    // F BLA attacks CON. CON is supporting ANK→BLA, so attack from BLA (the destination) cannot cut.
    // BLA attack vs CON hold: fails. ANK→BLA attack 2 (supported) vs F BLA hold 1 → ANK succeeds.
    [Fact]
    public void DATC_6_D_4_SupportNotCutByAttackFromSupportedDestination()
    {
        new AdjudicationScenario()
            .WithUnit("turkey",  "fleet", "ank")
            .WithUnit("turkey",  "fleet", "con")
            .WithUnit("russia",  "fleet", "bla")
            .WithOrder("turkey",  "fleet", "ank", "move bla")
            .WithOrder("turkey",  "fleet", "con", "support fleet ank move bla")
            .WithOrder("russia",  "fleet", "bla", "move con")
            .AssertOutcome("ank", OrderOutcome.Success)
            .AssertDislodged("bla")
            .AssertOutcome("con", OrderOutcome.Success)  // support held
            .Run();
    }

    // 6.D.5 — Two armies bounce each other with equal strength.
    // A VIE→TYR, A TYR→VIE: head-on, equal — both fail.
    // (Also tested in 6.E, included here as a dislodge-prevention check.)
    [Fact]
    public void DATC_6_D_5_BounceWithEqualStrength()
    {
        new AdjudicationScenario()
            .WithUnit("austria", "army", "vie")
            .WithUnit("germany", "army", "tyr")
            .WithOrder("austria", "army", "vie", "move tyr")
            .WithOrder("germany", "army", "tyr", "move vie")
            .AssertOutcome("vie", OrderOutcome.Failure)
            .AssertOutcome("tyr", OrderOutcome.Failure)
            .Run();
    }

    // 6.D.6 — Convoyed army can cut support in the province it attacks.
    // A NAP→TUN (via F ION), F TYS S F TUN hold.
    // A NAP attacks TUN. This cuts no support (TYS is the supporter of TUN, not TUN).
    // But A NAP cuts F ION's support if ION were supporting something — here it doesn't.
    // Key: A NAP arrives at TUN with attack 1. F TUN hold = 1 + TYS support = 2.
    // A NAP fails to dislodge. (Tests that convoy path is correctly computed.)
    [Fact]
    public void DATC_6_D_6_ConvoyedArmyCannotDislodgeSupportedFleet()
    {
        new AdjudicationScenario()
            .WithUnit("italy",  "army",  "nap")
            .WithUnit("italy",  "fleet", "ion")
            .WithUnit("turkey", "fleet", "tun")
            .WithUnit("turkey", "fleet", "tys")
            .WithOrder("italy",  "army",  "nap", "move tun")
            .WithOrder("italy",  "fleet", "ion", "convoy army nap move tun")
            .WithOrder("turkey", "fleet", "tun", "hold")
            .WithOrder("turkey", "fleet", "tys", "support fleet tun")
            .AssertOutcome("nap", OrderOutcome.Failure)
            .AssertOutcome("tun", OrderOutcome.Success)
            .Run();
    }
}
