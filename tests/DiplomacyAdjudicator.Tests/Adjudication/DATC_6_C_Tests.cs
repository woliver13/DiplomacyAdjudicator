using woliver13.DiplomacyAdjudicator.Core.Domain;
using Xunit;

namespace woliver13.DiplomacyAdjudicator.Tests.Adjudication;

/// <summary>
/// DATC v2.4 Section 6.C — Circular Movement
/// </summary>
public class DATC_6_C_Tests
{
    // 6.C.1 — Three army circular movement: A BUL→RUM, A RUM→SER, A SER→BUL
    // All three moves succeed (circular movement ruling).
    [Fact]
    public void DATC_6_C_1_ThreeArmyCircularMovement()
    {
        new AdjudicationScenario()
            .WithUnit("turkey",  "army", "bul")
            .WithUnit("turkey",  "army", "rum")
            .WithUnit("turkey",  "army", "ser")
            .WithOrder("turkey", "army", "bul", "move rum")
            .WithOrder("turkey", "army", "rum", "move ser")
            .WithOrder("turkey", "army", "ser", "move bul")
            .AssertOutcome("bul", OrderOutcome.Success)
            .AssertOutcome("rum", OrderOutcome.Success)
            .AssertOutcome("ser", OrderOutcome.Success)
            .Run();
    }

    // 6.C.2 — Three army circular movement with an outside attack (no support).
    // A CON→BUL (attack/prevent strength 1) competes with A SER→BUL (cycle, attack 1)
    // for the vacated province BUL. Equal strength → both bounced → cycle disrupted.
    // Per DATC: outside competition at equal strength disrupts the circular movement.
    [Fact]
    public void DATC_6_C_2_CircularMovementWithOutsideAttacker()
    {
        new AdjudicationScenario()
            .WithUnit("turkey",  "army", "bul")
            .WithUnit("turkey",  "army", "rum")
            .WithUnit("turkey",  "army", "ser")
            .WithUnit("austria", "army", "con")
            .WithOrder("turkey",  "army", "bul", "move rum")
            .WithOrder("turkey",  "army", "rum", "move ser")
            .WithOrder("turkey",  "army", "ser", "move bul")
            .WithOrder("austria", "army", "con", "move bul")
            .AssertOutcome("bul", OrderOutcome.Failure)
            .AssertOutcome("rum", OrderOutcome.Failure)
            .AssertOutcome("ser", OrderOutcome.Failure)
            .AssertOutcome("con", OrderOutcome.Failure)
            .Run();
    }

    // 6.C.3 — Disrupted three army circular movement.
    // A CON→BUL supported by A GRE (attack strength 2) dislodges BUL.
    // Because BUL is dislodged, the circular movement is invalid — all three fail.
    [Fact]
    public void DATC_6_C_3_DisruptedCircularMovement()
    {
        new AdjudicationScenario()
            .WithUnit("turkey",  "army", "bul")
            .WithUnit("turkey",  "army", "rum")
            .WithUnit("turkey",  "army", "ser")
            .WithUnit("austria", "army", "con")
            .WithUnit("austria", "army", "gre")
            .WithOrder("turkey",  "army", "bul", "move rum")
            .WithOrder("turkey",  "army", "rum", "move ser")
            .WithOrder("turkey",  "army", "ser", "move bul")
            .WithOrder("austria", "army", "con", "move bul")
            .WithOrder("austria", "army", "gre", "support army con move bul")
            .AssertDislodged("bul")
            .AssertOutcome("rum", OrderOutcome.Failure)
            .AssertOutcome("ser", OrderOutcome.Failure)
            .AssertOutcome("con", OrderOutcome.Success)
            .Run();
    }

    // 6.C.5 — A disrupted circular movement due to dislodged convoy.
    // Same as 6.C.4 but Italy adds F TUN supporting F NAP→ION (attack strength 2).
    // F ION is dislodged → convoy disrupted → circular movement fails.
    [Fact]
    public void DATC_6_C_5_DisruptedCircularMovementDueToDislodgedConvoy()
    {
        new AdjudicationScenario()
            .WithUnit("austria", "army",  "tri")
            .WithUnit("austria", "army",  "ser")
            .WithUnit("turkey",  "army",  "bul")
            .WithUnit("turkey",  "fleet", "aeg")
            .WithUnit("turkey",  "fleet", "ion")
            .WithUnit("turkey",  "fleet", "adr")
            .WithUnit("italy",   "fleet", "nap")
            .WithUnit("italy",   "fleet", "tun")
            .WithOrder("austria", "army",  "tri", "move ser")
            .WithOrder("austria", "army",  "ser", "move bul")
            .WithOrder("turkey",  "army",  "bul", "move tri")
            .WithOrder("turkey",  "fleet", "aeg", "convoy army bul move tri")
            .WithOrder("turkey",  "fleet", "ion", "convoy army bul move tri")
            .WithOrder("turkey",  "fleet", "adr", "convoy army bul move tri")
            .WithOrder("italy",   "fleet", "nap", "move ion")
            .WithOrder("italy",   "fleet", "tun", "support fleet nap move ion")
            .AssertOutcome("tri", OrderOutcome.Failure)
            .AssertOutcome("ser", OrderOutcome.Failure)
            .AssertOutcome("bul", OrderOutcome.Failure)
            .AssertDislodged("ion")
            .AssertOutcome("nap", OrderOutcome.Success)
            .Run();
    }

    // 6.C.6 — Two armies with two convoys.
    // England: F NTH convoys A LON→BEL. France: F ENG convoys A BEL→LON.
    // Both armies swap positions via convoy.
    [Fact]
    public void DATC_6_C_6_TwoArmiesWithTwoConvoys()
    {
        new AdjudicationScenario()
            .WithUnit("england", "fleet", "nth")
            .WithUnit("england", "army",  "lon")
            .WithUnit("france",  "fleet", "eng")
            .WithUnit("france",  "army",  "bel")
            .WithOrder("england", "fleet", "nth", "convoy army lon move bel")
            .WithOrder("england", "army",  "lon", "move bel")
            .WithOrder("france",  "fleet", "eng", "convoy army bel move lon")
            .WithOrder("france",  "army",  "bel", "move lon")
            .AssertOutcome("lon", OrderOutcome.Success)
            .AssertOutcome("bel", OrderOutcome.Success)
            .Run();
    }

    // 6.C.7 — Disrupted unit swap.
    // Same as 6.C.6 but France also has A BUR→BEL. A LON and A BUR both compete for BEL
    // (equal prevent strength), so neither BEL nor LON are captured — the swap fails.
    [Fact]
    public void DATC_6_C_7_DisruptedUnitSwap()
    {
        new AdjudicationScenario()
            .WithUnit("england", "fleet", "nth")
            .WithUnit("england", "army",  "lon")
            .WithUnit("france",  "fleet", "eng")
            .WithUnit("france",  "army",  "bel")
            .WithUnit("france",  "army",  "bur")
            .WithOrder("england", "fleet", "nth", "convoy army lon move bel")
            .WithOrder("england", "army",  "lon", "move bel")
            .WithOrder("france",  "fleet", "eng", "convoy army bel move lon")
            .WithOrder("france",  "army",  "bel", "move lon")
            .WithOrder("france",  "army",  "bur", "move bel")
            .AssertOutcome("lon", OrderOutcome.Failure)
            .AssertOutcome("bel", OrderOutcome.Failure)
            .Run();
    }

    // 6.C.4 — A circular movement with attacked convoy.
    // Austria: A TRI→SER, A SER→BUL. Turkey: A BUL→TRI via convoy (F AEG, F ION, F ADR).
    // Italy: F NAP→ION (attacks convoy fleet but cannot dislodge it — no support).
    // Result: ION survives; circular movement succeeds; NAP fails.
    [Fact]
    public void DATC_6_C_4_CircularMovementWithAttackedConvoy()
    {
        new AdjudicationScenario()
            .WithUnit("austria", "army",  "tri")
            .WithUnit("austria", "army",  "ser")
            .WithUnit("turkey",  "army",  "bul")
            .WithUnit("turkey",  "fleet", "aeg")
            .WithUnit("turkey",  "fleet", "ion")
            .WithUnit("turkey",  "fleet", "adr")
            .WithUnit("italy",   "fleet", "nap")
            .WithOrder("austria", "army",  "tri", "move ser")
            .WithOrder("austria", "army",  "ser", "move bul")
            .WithOrder("turkey",  "army",  "bul", "move tri")
            .WithOrder("turkey",  "fleet", "aeg", "convoy army bul move tri")
            .WithOrder("turkey",  "fleet", "ion", "convoy army bul move tri")
            .WithOrder("turkey",  "fleet", "adr", "convoy army bul move tri")
            .WithOrder("italy",   "fleet", "nap", "move ion")
            .AssertOutcome("tri", OrderOutcome.Success)
            .AssertOutcome("ser", OrderOutcome.Success)
            .AssertOutcome("bul", OrderOutcome.Success)
            .AssertOutcome("nap", OrderOutcome.Failure)
            .Run();
    }
}
