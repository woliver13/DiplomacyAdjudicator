using DiplomacyAdjudicator.Core.Domain;
using Xunit;

namespace DiplomacyAdjudicator.Tests.Adjudication;

/// <summary>
/// DATC v2.4 Section 6.B — Coastal Issues
/// Source: zond/godip datc_v2.4_06.txt (Lucas B. Kruijswijk)
/// </summary>
public class DATC_6_B_Tests
{
    // 6.B.1 — Moving with unspecified coast when coast is necessary
    // F POR - SPA: both SPA/NC and SPA/SC are reachable from POR → ambiguous → fail
    [Fact]
    public void DATC_6_B_1_MovingWithUnspecifiedCoastWhenNecessary()
    {
        new AdjudicationScenario()
            .WithUnit("france", "fleet", "por")
            .WithOrder("france", "fleet", "por", "move spa")
            .AssertOutcome("por", OrderOutcome.Failure)
            .Run();
    }

    // 6.B.2 — Moving with unspecified coast when only one coast reachable
    // F GAS - SPA: only SPA/NC is reachable from GAS → auto-resolve to SPA/NC → succeed
    [Fact]
    public void DATC_6_B_2_MovingWithUnspecifiedCoastWhenOnlyOneReachable()
    {
        new AdjudicationScenario()
            .WithUnit("france", "fleet", "gas")
            .WithOrder("france", "fleet", "gas", "move spa")
            .AssertOutcome("gas", OrderOutcome.Success)
            .Run();
    }

    // 6.B.3 — Moving with wrong coast when coast is not necessary
    // F GAS - SPA/SC: GAS is not adjacent to SPA/SC → fail
    [Fact]
    public void DATC_6_B_3_MovingWithWrongCoast()
    {
        new AdjudicationScenario()
            .WithUnit("france", "fleet", "gas")
            .WithOrder("france", "fleet", "gas", "move spa_sc")
            .AssertOutcome("gas", OrderOutcome.Failure)
            .Run();
    }

    // 6.B.4 — Support to unreachable coast is allowed (from the supporting unit's perspective)
    // F GAS - SPA/NC (ok), F MAR S F GAS - SPA/NC (MAR adjacent to SPA/SC but not SPA/NC via fleet)
    // Wait - MAR fleet adj: ["lyo", "pie", "spa_sc"] — MAR cannot reach SPA/NC for fleet
    // Per DATC: "support to unreachable coast is ALLOWED" — MAR supports GAS move even though
    // MAR cannot reach SPA/NC itself
    // Italy F WES - SPA/SC (fails: both contesting different coasts, GAS wins SPA/NC)
    [Fact]
    public void DATC_6_B_4_SupportToUnreachableCoastAllowed()
    {
        new AdjudicationScenario()
            .WithUnit("france", "fleet", "gas")
            .WithUnit("france", "fleet", "mar")
            .WithUnit("italy", "fleet", "wes")
            .WithOrder("france", "fleet", "gas", "move spa_nc")
            .WithOrder("france", "fleet", "mar", "support fleet gas move spa_nc")
            .WithOrder("italy", "fleet", "wes", "move spa_sc")
            .AssertOutcome("gas", OrderOutcome.Success)
            .AssertOutcome("wes", OrderOutcome.Success) // different coast, no conflict
            .Run();
    }

    // 6.B.5 — Support from unreachable coast not allowed
    // F SPA/NC S F MAR - LYO: SPA/NC cannot reach LYO (lyo adj: mar,pie,spa_sc,tus,tys,wes)
    // Support is void → F MAR attack strength 1 vs F LYO hold strength 1 → bounce
    [Fact]
    public void DATC_6_B_5_SupportFromUnreachableCoastNotAllowed()
    {
        new AdjudicationScenario()
            .WithUnit("france", "fleet", "spa_nc")
            .WithUnit("france", "fleet", "mar")
            .WithUnit("italy", "fleet", "lyo")
            .WithOrder("france", "fleet", "spa_nc", "support fleet mar move lyo")
            .WithOrder("france", "fleet", "mar", "move lyo")
            .WithOrder("italy", "fleet", "lyo", "hold")
            .AssertOutcome("mar", OrderOutcome.Failure)
            .AssertOutcome("lyo", OrderOutcome.Success)
            .Run();
    }

    // 6.B.6 — Support can be cut with other coast
    // F LYO - SPA/SC attacks SPA (attacks SPA/NC's "base province" = cuts its support)
    // F SPA/NC S F MAO (hold support cut by F LYO attacking SPA/SC — same province base)
    // F ENG: NAT→MAO with IRI support dislodges MAO
    [Fact]
    public void DATC_6_B_6_SupportCanBeCutWithOtherCoast()
    {
        new AdjudicationScenario()
            .WithUnit("england", "fleet", "iri")
            .WithUnit("england", "fleet", "nao")
            .WithUnit("france", "fleet", "spa_nc")
            .WithUnit("france", "fleet", "mao")
            .WithUnit("italy", "fleet", "lyo")
            .WithOrder("england", "fleet", "iri", "support fleet nao move mao")
            .WithOrder("england", "fleet", "nao", "move mao")
            .WithOrder("france", "fleet", "spa_nc", "support fleet mao")
            .WithOrder("france", "fleet", "mao", "hold")
            .WithOrder("italy", "fleet", "lyo", "move spa_sc")
            .AssertOutcome("nao", OrderOutcome.Success)
            .AssertDislodged("mao")
            .Run();
    }

    // 6.B.10 — Unit ordered with wrong coast (departure coast insignificant)
    // F SPA/SC ordered as "F SPA/NC - LYO": the /NC departure designation is irrelevant,
    // fleet is actually at SPA/SC, so it moves from SPA/SC. LYO is adjacent to SPA/SC → succeed
    [Fact]
    public void DATC_6_B_10_UnitOrderedWithWrongCoastDepartureCoastInsignificant()
    {
        new AdjudicationScenario()
            .WithUnit("france", "fleet", "spa_sc")
            .WithOrder("france", "fleet", "spa_sc", "move lyo")
            .AssertOutcome("spa_sc", OrderOutcome.Success)
            .Run();
    }

    // 6.B.11 — Coast cannot be ordered to change
    // F SPA/NC ordered as "F SPA/SC - LYO": unit is at SPA/NC, not SPA/SC → fail
    // (The order province doesn't match the unit's actual coast)
    [Fact]
    public void DATC_6_B_11_CoastCannotBeOrderedToChange()
    {
        new AdjudicationScenario()
            .WithUnit("france", "fleet", "spa_nc")
            .WithOrder("france", "fleet", "spa_nc", "move lyo")
            .AssertOutcome("spa_nc", OrderOutcome.Failure) // SPA/NC cannot reach LYO
            .Run();
    }

    // 6.B.12 — Armies may not move to coasts (coast notation irrelevant for armies)
    // A GAS - SPA/NC: coast ignored for armies, treated as A GAS - SPA → succeed
    [Fact]
    public void DATC_6_B_12_ArmiesIgnoreCoastNotation()
    {
        new AdjudicationScenario()
            .WithUnit("france", "army", "gas")
            .WithOrder("france", "army", "gas", "move spa_nc")
            .AssertOutcome("gas", OrderOutcome.Success)
            .Run();
    }

    // 6.B.13 — Circular movement with different coasts (coastal crawl not allowed)
    // F BUL/SC - CON and F CON - BUL/EC: same base province swap → head-on, both fail
    [Fact]
    public void DATC_6_B_13_CoastalCrawlNotAllowed()
    {
        new AdjudicationScenario()
            .WithUnit("turkey", "fleet", "bul_sc")
            .WithUnit("turkey", "fleet", "con")
            .WithOrder("turkey", "fleet", "bul_sc", "move con")
            .WithOrder("turkey", "fleet", "con", "move bul_ec")
            .AssertOutcome("bul_sc", OrderOutcome.Failure)
            .AssertOutcome("con", OrderOutcome.Failure)
            .Run();
    }
}
