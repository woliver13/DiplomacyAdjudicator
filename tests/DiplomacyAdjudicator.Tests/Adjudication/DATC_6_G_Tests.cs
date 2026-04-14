using woliver13.DiplomacyAdjudicator.Core.Domain;
using Xunit;

namespace woliver13.DiplomacyAdjudicator.Tests.Adjudication;

/// <summary>
/// DATC v2.4 Section 6.G — Convoying to an Adjacent Province (Szykman Rule)
/// </summary>
public class DATC_6_G_Tests
{
    // 6.G.1 — Army can be convoyed to an adjacent province.
    // A YOR→LON via F NTH. YOR is adjacent to LON directly (army adj).
    // Szykman rule: since direct route exists, treat as direct move. Succeeds regardless.
    // LON is empty → YOR moves successfully. Convoy order on NTH is irrelevant.
    [Fact]
    public void DATC_6_G_1_ConvoyToAdjacentProvinceSucceeds()
    {
        new AdjudicationScenario()
            .WithUnit("england", "army",  "yor")
            .WithUnit("england", "fleet", "nth")
            .WithOrder("england", "army",  "yor", "move lon")
            .WithOrder("england", "fleet", "nth", "convoy army yor move lon")
            .AssertOutcome("yor", OrderOutcome.Success)
            .Run();
    }

    // 6.G.2 — Szykman rule: adjacent moves treated as direct for head-on purposes.
    // A LON→YOR and A YOR→LON, with F NTH trying to convoy LON→YOR.
    // Both are directly adjacent (Szykman treats as direct). Head-on applies. Both fail.
    [Fact]
    public void DATC_6_G_2_SzykmanHeadOnWithConvoyOption()
    {
        new AdjudicationScenario()
            .WithUnit("england", "army",  "lon")
            .WithUnit("england", "fleet", "nth")
            .WithUnit("germany", "army",  "yor")
            .WithOrder("england", "army",  "lon", "move yor")
            .WithOrder("england", "fleet", "nth", "convoy army lon move yor")
            .WithOrder("germany", "army",  "yor", "move lon")
            .AssertOutcome("lon", OrderOutcome.Failure)
            .AssertOutcome("yor", OrderOutcome.Failure)
            .Run();
    }

    // 6.G.3 — Convoying to an adjacent province while the direct route would fail head-on.
    // Szykman rule: since LON is directly adjacent to YOR, the convoy is ignored.
    // The move is treated as direct. Head-on still applies.
    // Even with support (A WAL S A LON→YOR), if A YOR→LON is also direct, head-on is resolved normally.
    // DefendStrength(LON→YOR) = 2 (WAL supports). DefendStrength(YOR→LON) = 1.
    // YOR loses head-on → YOR fails. LON: attack 2 > hold 1 → LON succeeds.
    [Fact]
    public void DATC_6_G_3_SupportedDirectMoveWinsHeadOn()
    {
        new AdjudicationScenario()
            .WithUnit("england", "army",  "lon")
            .WithUnit("england", "army",  "wal")
            .WithUnit("england", "fleet", "nth")
            .WithUnit("germany", "army",  "yor")
            .WithOrder("england", "army",  "lon", "move yor")
            .WithOrder("england", "army",  "wal", "support army lon move yor")
            .WithOrder("england", "fleet", "nth", "convoy army lon move yor")
            .WithOrder("germany", "army",  "yor", "move lon")
            .AssertOutcome("lon", OrderOutcome.Success)
            .AssertDislodged("yor")
            .Run();
    }

    // 6.G.4 — Convoyed army (truly non-adjacent) is not subject to head-on rules.
    // A NAP→TUN via F ION: not directly adjacent. A TUN→NAP: not directly adjacent.
    // No head-on (neither move is direct). NAP→TUN: attack 1 vs TUN hold 1 → bounce.
    [Fact]
    public void DATC_6_G_4_NoHeadOnForTrulyNonAdjacentConvoys()
    {
        new AdjudicationScenario()
            .WithUnit("italy",  "army",  "nap")
            .WithUnit("italy",  "fleet", "ion")
            .WithUnit("turkey", "fleet", "tun")
            .WithOrder("italy",  "army",  "nap", "move tun")
            .WithOrder("italy",  "fleet", "ion", "convoy army nap move tun")
            .WithOrder("turkey", "fleet", "tun", "move nap")
            .AssertOutcome("nap", OrderOutcome.Failure) // attack 1 vs hold 1 (TUN→NAP is illegal, TUN stays)
            .AssertOutcome("tun", OrderOutcome.Failure) // fleet not adjacent to NAP, no convoy → illegal
            .Run();
    }
}
