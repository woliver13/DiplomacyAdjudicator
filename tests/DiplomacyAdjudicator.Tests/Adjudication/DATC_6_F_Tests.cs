using DiplomacyAdjudicator.Core.Domain;
using Xunit;

namespace DiplomacyAdjudicator.Tests.Adjudication;

/// <summary>
/// DATC v2.4 Section 6.F — Convoys
/// </summary>
public class DATC_6_F_Tests
{
    // 6.F.1 — Basic convoy: A LON→NWY via F NTH.
    // LON army is not adjacent to NWY; F NTH is adjacent to both.
    // Attack 1 vs NWY hold 0 (empty) → LON succeeds.
    [Fact]
    public void DATC_6_F_1_BasicConvoySuccess()
    {
        new AdjudicationScenario()
            .WithUnit("england", "army",  "lon")
            .WithUnit("england", "fleet", "nth")
            .WithOrder("england", "army",  "lon", "move nwy")
            .WithOrder("england", "fleet", "nth", "convoy army lon move nwy")
            .AssertOutcome("lon", OrderOutcome.Success)
            .Run();
    }

    // 6.F.2 — Convoy disrupted: F NTH dislodged by F SKA (supported by F DEN).
    // When F NTH is dislodged the convoy fails. A LON does not move.
    [Fact]
    public void DATC_6_F_2_ConvoyFailsWhenFleetDislodged()
    {
        new AdjudicationScenario()
            .WithUnit("england", "army",  "lon")
            .WithUnit("england", "fleet", "nth")
            .WithUnit("germany", "fleet", "ska")
            .WithUnit("germany", "fleet", "den")
            .WithOrder("england", "army",  "lon", "move nwy")
            .WithOrder("england", "fleet", "nth", "convoy army lon move nwy")
            .WithOrder("germany", "fleet", "ska", "move nth")
            .WithOrder("germany", "fleet", "den", "support fleet ska move nth")
            .AssertOutcome("lon", OrderOutcome.Failure)
            .AssertDislodged("nth")
            .Run();
    }

    // 6.F.3 — Convoy with two fleets: F NTH + F NWG relay A LON→NWY.
    // NWG is adjacent to NTH and to NWY, but NWG is NOT adjacent to LON.
    // NTH is adjacent to both LON and NWY, so NTH alone is sufficient.
    // Both fleets issuing convoy orders: BFS finds path via NTH (adj LON, adj NWY). Success.
    [Fact]
    public void DATC_6_F_3_MultiFleetConvoySuccess()
    {
        new AdjudicationScenario()
            .WithUnit("england", "army",  "lon")
            .WithUnit("england", "fleet", "nth")
            .WithUnit("england", "fleet", "nwg")
            .WithOrder("england", "army",  "lon", "move nwy")
            .WithOrder("england", "fleet", "nth", "convoy army lon move nwy")
            .WithOrder("england", "fleet", "nwg", "convoy army lon move nwy")
            .AssertOutcome("lon", OrderOutcome.Success)
            .Run();
    }

    // 6.F.4 — Convoy via alternate route when one fleet is dislodged.
    // A LON→NWY: F NTH (adj LON, adj NWY) and F NWG both convoy.
    // F NWG is dislodged by F BAR (supported by F CLY S F BAR→NWG, attack 2 > hold 1).
    // The convoy still works via NTH alone (NTH adj LON and adj NWY). NWY is empty.
    [Fact]
    public void DATC_6_F_4_ConvoySucceedsViaAlternateRoute()
    {
        new AdjudicationScenario()
            .WithUnit("england", "army",  "lon")
            .WithUnit("england", "fleet", "nth")
            .WithUnit("england", "fleet", "nwg")
            .WithUnit("russia",  "fleet", "bar")
            .WithUnit("russia",  "fleet", "cly")
            .WithOrder("england", "army",  "lon", "move nwy")
            .WithOrder("england", "fleet", "nth", "convoy army lon move nwy")
            .WithOrder("england", "fleet", "nwg", "convoy army lon move nwy")
            .WithOrder("russia",  "fleet", "bar", "move nwg")
            .WithOrder("russia",  "fleet", "cly", "support fleet bar move nwg")
            .AssertOutcome("lon", OrderOutcome.Success) // NTH alone still provides convoy
            .AssertDislodged("nwg")
            .Run();
    }

    // 6.F.5 — Convoyed army dislodges a unit at the destination.
    // A NAP→TUN via F ION. F TUN holds. Attack 1 > hold 0? No, TUN has a fleet: hold 1.
    // Bounce. Tests that convoy BFS is used and attack strength is computed correctly.
    [Fact]
    public void DATC_6_F_5_ConvoyBouncesAgainstOccupiedProvince()
    {
        new AdjudicationScenario()
            .WithUnit("italy",  "army",  "nap")
            .WithUnit("italy",  "fleet", "ion")
            .WithUnit("turkey", "fleet", "tun")
            .WithOrder("italy",  "army",  "nap", "move tun")
            .WithOrder("italy",  "fleet", "ion", "convoy army nap move tun")
            .WithOrder("turkey", "fleet", "tun", "hold")
            .AssertOutcome("nap", OrderOutcome.Failure) // attack 1 vs hold 1 → bounce
            .AssertOutcome("tun", OrderOutcome.Success)
            .Run();
    }

    // 6.F.6 — Convoyed army with support dislodges fleet.
    // A NAP→TUN via F ION, supported by F TYS S A NAP→TUN.
    // Attack strength 2 > TUN hold 1 → TUN dislodged.
    [Fact]
    public void DATC_6_F_6_SupportedConvoyDislodgesFleet()
    {
        new AdjudicationScenario()
            .WithUnit("italy",  "army",  "nap")
            .WithUnit("italy",  "fleet", "ion")
            .WithUnit("italy",  "fleet", "tys")
            .WithUnit("turkey", "fleet", "tun")
            .WithOrder("italy",  "army",  "nap", "move tun")
            .WithOrder("italy",  "fleet", "ion", "convoy army nap move tun")
            .WithOrder("italy",  "fleet", "tys", "support army nap move tun")
            .WithOrder("turkey", "fleet", "tun", "hold")
            .AssertOutcome("nap", OrderOutcome.Success)
            .AssertDislodged("tun")
            .Run();
    }
}
