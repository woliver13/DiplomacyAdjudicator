using DiplomacyAdjudicator.Core.Domain;
using Xunit;

namespace DiplomacyAdjudicator.Tests.Adjudication;

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
    // The outside attacker fails; the circular movement still succeeds.
    // A CON→BUL (attack 1) cannot break the cycle because BUL's prevent strength (from circular) blocks it.
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
            .AssertOutcome("bul", OrderOutcome.Success)
            .AssertOutcome("rum", OrderOutcome.Success)
            .AssertOutcome("ser", OrderOutcome.Success)
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
}
