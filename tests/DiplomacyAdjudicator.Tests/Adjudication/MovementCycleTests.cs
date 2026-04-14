using woliver13.DiplomacyAdjudicator.Core.Domain;

namespace woliver13.DiplomacyAdjudicator.Tests.Adjudication;

/// <summary>
/// Dedicated tests for circular-movement detection and fix-up logic
/// (IsCircularMovement / FixCircularMovements in MovementResolver).
/// </summary>
public class MovementCycleTests
{
    // DATC additional: 3-unit cycle disrupted when an external supported attacker
    // dislodges one member, breaking the cycle.
    // Setup:
    //   A vie→tyr, A tyr→boh, A boh→vie  (attempted 3-unit cycle — Austria)
    //   A mun→boh, supported by A sil     (Germany)
    // Expected:
    //   mun → Success (strength 2 beats boh's defend 1)
    //   boh → Dislodged
    //   tyr → Failure (boh slot taken by mun)
    //   vie → Failure (tyr still occupied)
    [Fact]
    public void CircularMovement_DisruptedByExternalDislodge_CycleFails()
    {
        new AdjudicationScenario()
            .WithUnit("austria", "army", "vie")
            .WithUnit("austria", "army", "tyr")
            .WithUnit("austria", "army", "boh")
            .WithUnit("germany", "army", "mun")
            .WithUnit("germany", "army", "sil")
            .WithOrder("austria", "army", "vie", "move tyr")
            .WithOrder("austria", "army", "tyr", "move boh")
            .WithOrder("austria", "army", "boh", "move vie")
            .WithOrder("germany", "army", "mun", "move boh")
            .WithOrder("germany", "army", "sil", "support army mun move boh")
            .AssertOutcome("mun", OrderOutcome.Success)
            .AssertOutcome("boh", OrderOutcome.Dislodged)
            .AssertOutcome("tyr", OrderOutcome.Failure)
            .AssertOutcome("vie", OrderOutcome.Failure)
            .Run();
    }

    // Two-unit attempted cycle (swap) with no convoy — head-on collision, both fail.
    // This exercises IsCircularMovement returning false for a 2-unit back-and-forth.
    [Fact]
    public void CircularMovement_TwoUnitSwapWithoutConvoy_BothFail()
    {
        new AdjudicationScenario()
            .WithUnit("austria", "army", "vie")
            .WithUnit("russia", "army", "bud")
            .WithOrder("austria", "army", "vie", "move bud")
            .WithOrder("russia", "army", "bud", "move vie")
            .AssertOutcome("vie", OrderOutcome.Failure)
            .AssertOutcome("bud", OrderOutcome.Failure)
            .Run();
    }
}
