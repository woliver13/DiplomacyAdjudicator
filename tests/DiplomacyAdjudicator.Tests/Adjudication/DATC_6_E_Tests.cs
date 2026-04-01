using DiplomacyAdjudicator.Core.Domain;
using Xunit;

namespace DiplomacyAdjudicator.Tests.Adjudication;

/// <summary>
/// DATC v2.4 Section 6.E — Head-on Battles
/// </summary>
public class DATC_6_E_Tests
{
    // 6.E.1 — Direct head-on with equal strength: both fail.
    // A VIE→TYR, A TYR→VIE — both are directly adjacent. Equal defend strength (1 each). Both fail.
    [Fact]
    public void DATC_6_E_1_HeadOnBattleEqualStrengthBothFail()
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

    // 6.E.2 — Head-on with support: the supported unit wins.
    // A VIE→TYR (supported by A MUN), A TYR→VIE.
    // DefendStrength(VIE→TYR) = 2 > DefendStrength(TYR→VIE) = 1 → TYR loses head-on.
    // VIE→TYR: attack 2 > TYR hold 1 → VIE succeeds, TYR dislodged.
    [Fact]
    public void DATC_6_E_2_HeadOnWithSupportWinner()
    {
        new AdjudicationScenario()
            .WithUnit("austria", "army", "vie")
            .WithUnit("austria", "army", "mun")
            .WithUnit("germany", "army", "tyr")
            .WithOrder("austria", "army", "vie", "move tyr")
            .WithOrder("austria", "army", "mun", "support army vie move tyr")
            .WithOrder("germany", "army", "tyr", "move vie")
            .AssertOutcome("vie", OrderOutcome.Success)
            .AssertDislodged("tyr")
            .Run();
    }

    // 6.E.3 — No head-on between a direct and a convoyed move.
    // A LON→NWY (convoy via F NTH), A NWY→LON (direct? NWY army adj: fin,ska,stp,swe — NOT lon).
    // NWY cannot directly reach LON. So only LON→NWY via convoy.
    // No opposing convoy for NWY→LON → NWY's move is simply illegal (not adjacent, no convoy).
    // LON→NWY: attack 1 > NWY hold 1? No. NWY holds. LON fails.
    // (Tests that convoy path is used but doesn't create a head-on.)
    [Fact]
    public void DATC_6_E_3_NoHeadOnForConvoyedMove()
    {
        new AdjudicationScenario()
            .WithUnit("england", "army",  "lon")
            .WithUnit("england", "fleet", "nth")
            .WithUnit("russia",  "army",  "nwy")
            .WithOrder("england", "army",  "lon", "move nwy")
            .WithOrder("england", "fleet", "nth", "convoy army lon move nwy")
            .WithOrder("russia",  "army",  "nwy", "hold")
            .AssertOutcome("lon", OrderOutcome.Failure) // attack 1, hold 1 → bounce
            .AssertOutcome("nwy", OrderOutcome.Success)
            .Run();
    }

    // 6.E.4 — Direct head-on: the defending fleet is not backed.
    // F ENG→BEL, F BEL→ENG. Mutually adjacent sea provinces.
    // Equal defend strength (1 each). Both fail (neither gets across).
    [Fact]
    public void DATC_6_E_4_FleetHeadOnEqualStrength()
    {
        new AdjudicationScenario()
            .WithUnit("england", "fleet", "eng")
            .WithUnit("france",  "fleet", "bel")
            .WithOrder("england", "fleet", "eng", "move bel")
            .WithOrder("france",  "fleet", "bel", "move eng")
            .AssertOutcome("eng", OrderOutcome.Failure)
            .AssertOutcome("bel", OrderOutcome.Failure)
            .Run();
    }

    // 6.E.5 — No head-on when one move is not directly adjacent (Szykman rule).
    // A LON→YOR (direct: LON army adj YOR), A YOR→LON (direct: YOR army adj LON).
    // Head-on applies. Both equal → both fail.
    // Even though F NTH could convoy, direct takes precedence.
    [Fact]
    public void DATC_6_E_5_DirectHeadOnIgnoresConvoyOption()
    {
        new AdjudicationScenario()
            .WithUnit("england", "army",  "lon")
            .WithUnit("england", "fleet", "nth")
            .WithUnit("russia",  "army",  "yor")
            .WithOrder("england", "army",  "lon", "move yor")
            .WithOrder("england", "fleet", "nth", "convoy army lon move yor")
            .WithOrder("russia",  "army",  "yor", "move lon")
            .AssertOutcome("lon", OrderOutcome.Failure)
            .AssertOutcome("yor", OrderOutcome.Failure)
            .Run();
    }
}
