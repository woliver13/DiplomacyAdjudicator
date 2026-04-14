using woliver13.DiplomacyAdjudicator.Core.Domain;
using Xunit;

namespace woliver13.DiplomacyAdjudicator.Tests.Adjudication;

/// <summary>
/// Targeted tests for strength-calculation logic in MovementResolver:
/// AttackStrength, DefendStrength, PreventStrength, HoldStrength.
/// These serve as regression guards for the CountSupports refactoring (issue #15).
/// </summary>
public class MovementStrengthTests
{
    // Attack strength 3 (1 mover + 2 supporters) beats hold strength 2 (1 holder + 1 supporter).
    // A MUN → BOH (adj), supported by A TYR and A SIL (both adj to BOH).
    // A BOH holds, supported by A VIE (adj to BOH).
    // Attack 3 > Hold 2 → mun succeeds, boh dislodged.
    // Exercises CountSupports<SupportMoveOrder> in AttackStrength
    // and CountSupports<SupportHoldOrder> in HoldStrength.
    [Fact]
    public void AttackStrength_ThreeBeatsHoldStrengthTwo_MoverSucceeds()
    {
        new AdjudicationScenario()
            .WithUnit("germany", "army", "mun")
            .WithUnit("germany", "army", "tyr")
            .WithUnit("germany", "army", "sil")
            .WithUnit("austria", "army", "boh")
            .WithUnit("austria", "army", "vie")
            .WithOrder("germany", "army", "mun", "move boh")
            .WithOrder("germany", "army", "tyr", "support army mun move boh")
            .WithOrder("germany", "army", "sil", "support army mun move boh")
            .WithOrder("austria", "army", "boh", "hold")
            .WithOrder("austria", "army", "vie", "support army boh")
            .AssertOutcome("mun", OrderOutcome.Success)
            .AssertDislodged("boh")
            .Run();
    }

    // Attack strength 2 does NOT beat hold strength 2 — move fails.
    // A MUN → BOH (supported by A TYR) vs A BOH hold (supported by A VIE).
    // Attack 2 == Hold 2 → tie → mun fails.
    [Fact]
    public void AttackStrength_TwoDoesNotBeatHoldStrengthTwo_MoveFails()
    {
        new AdjudicationScenario()
            .WithUnit("germany", "army", "mun")
            .WithUnit("germany", "army", "tyr")
            .WithUnit("austria", "army", "boh")
            .WithUnit("austria", "army", "vie")
            .WithOrder("germany", "army", "mun", "move boh")
            .WithOrder("germany", "army", "tyr", "support army mun move boh")
            .WithOrder("austria", "army", "boh", "hold")
            .WithOrder("austria", "army", "vie", "support army boh")
            .AssertOutcome("mun", OrderOutcome.Failure)
            .AssertOutcome("boh", OrderOutcome.Success)
            .Run();
    }

    // DefendStrength > DefendStrength in a head-on: the stronger side wins.
    // Germany A BER→MUN (supported by A SIL) vs Austria A MUN→BER (no support).
    // DefendStrength(BER→MUN) = 2, DefendStrength(MUN→BER) = 1.
    // MUN→BER has DefendStrength 1 <= 2, so it loses the head-on.
    // Then AttackStrength(BER→MUN)=2 > HoldStrength(MUN)=1 → BER succeeds.
    [Fact]
    public void DefendStrength_SupportedAttackerWinsHeadOn()
    {
        new AdjudicationScenario()
            .WithUnit("germany", "army", "ber")
            .WithUnit("germany", "army", "sil")
            .WithUnit("austria", "army", "mun")
            .WithOrder("germany", "army", "ber", "move mun")
            .WithOrder("germany", "army", "sil", "support army ber move mun")
            .WithOrder("austria", "army", "mun", "move ber")
            .AssertOutcome("ber", OrderOutcome.Success)
            .AssertDislodged("mun")
            .Run();
    }
}
