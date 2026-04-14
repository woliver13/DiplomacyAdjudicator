using woliver13.DiplomacyAdjudicator.Core.Adjudication;
using woliver13.DiplomacyAdjudicator.Core.Domain;
using woliver13.DiplomacyAdjudicator.Core.Map;
using Xunit;

namespace woliver13.DiplomacyAdjudicator.Tests.Adjudication;

/// <summary>
/// DATC v3.0 Section 6.J — Civil Disorder and Disbands
///
/// Civil disorder auto-disband algorithm (applied when a power submits
/// fewer disband orders than required):
///   1. Remove units with the GREATEST distance to any home supply centre first.
///   2. Distance for armies: BFS through ALL provinces (armies may traverse sea
///      areas for distance purposes — each province adds 1).
///   3. Distance for fleets: BFS through fleet-accessible provinces only
///      (fleets cannot cross land).
///   4. When distance is equal: fleets are removed before armies.
///   5. When type and distance are equal: alphabetical by province code.
/// </summary>
public class DATC_6_J_Tests
{
    private static readonly MapGraph Map = MapGraph.LoadStandard();
    private static BuildAdjudicator Adjudicator => new();

    // -------------------------------------------------------------------------
    // 6.J.1 — Too many remove orders.
    // France has 2 units (A PIC, A PAR) and controls PAR (1 SC) → must disband 1.
    // France submits 3 disband orders: F LYO (non-existent), A PIC, A PAR.
    // F LYO → Void (no unit there). A PIC → OK (satisfies the 1 required disband).
    // A PAR → Void (no disbands remaining).
    // -------------------------------------------------------------------------
    [Fact]
    public void DATC_6_J_1_TooManyRemoveOrders()
    {
        var units = new List<Unit>
        {
            new(UnitType.Army, Power.France, new Province("pic")),
            new(UnitType.Army, Power.France, new Province("par")),
        };
        var scs = new Dictionary<Power, IReadOnlyList<Province>>
        {
            [Power.France] = [new Province("par")]
        };
        // Three disband orders: one for a non-existent unit, then PIC, then PAR
        var orders = new List<Order>
        {
            new DisbandOrder(new Unit(UnitType.Fleet, Power.France, new Province("lyo"))),
            new DisbandOrder(new Unit(UnitType.Army,  Power.France, new Province("pic"))),
            new DisbandOrder(new Unit(UnitType.Army,  Power.France, new Province("par"))),
        };
        var result = Adjudicator.Adjudicate(new BuildAdjudicationRequest(Map, units, scs, orders));

        Assert.Equal(OrderOutcome.Void,    result.OrderResults[0].Outcome); // F LYO — no unit
        Assert.Equal(OrderOutcome.Success, result.OrderResults[1].Outcome); // A PIC — removed
        Assert.Equal(OrderOutcome.Void,    result.OrderResults[2].Outcome); // A PAR — surplus

        // A PIC is gone; A PAR remains
        Assert.Single(result.ResultingUnits);
        Assert.Equal("par", result.ResultingUnits[0].Province.Code);
    }

    // -------------------------------------------------------------------------
    // 6.J.2 — Removing the same unit twice.
    // France has 3 units (A PIC, A PAR, F NAO) and controls PAR (1 SC) → -2.
    // France submits A PAR D twice. First succeeds; second is void (unit gone).
    // Civil disorder removes one more unit (F NAO is farthest from French home SCs).
    // -------------------------------------------------------------------------
    [Fact]
    public void DATC_6_J_2_RemovingTheSameUnitTwice()
    {
        var units = new List<Unit>
        {
            new(UnitType.Army,  Power.France, new Province("pic")),
            new(UnitType.Army,  Power.France, new Province("par")),
            new(UnitType.Fleet, Power.France, new Province("nao")),
        };
        var scs = new Dictionary<Power, IReadOnlyList<Province>>
        {
            [Power.France] = [new Province("par")]
        };
        // Two identical A PAR D orders → one succeeds, one is void
        var orders = new List<Order>
        {
            new DisbandOrder(new Unit(UnitType.Army, Power.France, new Province("par"))),
            new DisbandOrder(new Unit(UnitType.Army, Power.France, new Province("par"))),
        };
        var result = Adjudicator.Adjudicate(new BuildAdjudicationRequest(Map, units, scs, orders));

        // Exactly one of the two A PAR orders is OK and one is Void
        var parResults = result.OrderResults
            .Where(r => r.Order is DisbandOrder d &&
                        MapGraph.BaseCode(d.Unit.Province.Code) == "par")
            .ToList();
        Assert.Equal(2, parResults.Count);
        Assert.Contains(parResults, r => r.Outcome == OrderOutcome.Success);
        Assert.Contains(parResults, r => r.Outcome == OrderOutcome.Void);

        // A PAR was removed; civil disorder also removed one more unit
        Assert.Single(result.ResultingUnits);
        Assert.DoesNotContain(result.ResultingUnits, u => u.Province.Code == "par");
    }

    // -------------------------------------------------------------------------
    // 6.J.3 — Civil disorder: two armies with different distance.
    // Russia controls SWE (1 SC), has A LVN and A SWE, → must remove 1.
    // No orders submitted. A LVN is 1 step from Russian home SCs; A SWE is 2.
    // A SWE (farthest) is auto-disbanded; A LVN remains.
    // -------------------------------------------------------------------------
    [Fact]
    public void DATC_6_J_3_CivilDisorderTwoArmiesDifferentDistance()
    {
        var units = new List<Unit>
        {
            new(UnitType.Army, Power.Russia, new Province("lvn")),
            new(UnitType.Army, Power.Russia, new Province("swe")),
        };
        var scs = new Dictionary<Power, IReadOnlyList<Province>>
        {
            [Power.Russia] = [new Province("swe")]
        };
        var result = Adjudicator.Adjudicate(new BuildAdjudicationRequest(Map, units, scs, []));

        Assert.Single(result.ResultingUnits);
        Assert.Equal("lvn", result.ResultingUnits[0].Province.Code);
    }

    // -------------------------------------------------------------------------
    // 6.J.4 — Civil disorder: two armies with equal distance.
    // Russia controls STP (1 SC), has A LVN and A UKR, → must remove 1.
    // No orders. Both armies have distance 1 to Russian home SCs.
    // Tiebreaker: alphabetical → A LVN removed ("lvn" < "ukr").
    // -------------------------------------------------------------------------
    [Fact]
    public void DATC_6_J_4_CivilDisorderTwoArmiesEqualDistance()
    {
        var units = new List<Unit>
        {
            new(UnitType.Army, Power.Russia, new Province("lvn")),
            new(UnitType.Army, Power.Russia, new Province("ukr")),
        };
        var scs = new Dictionary<Power, IReadOnlyList<Province>>
        {
            [Power.Russia] = [new Province("stp")]
        };
        var result = Adjudicator.Adjudicate(new BuildAdjudicationRequest(Map, units, scs, []));

        Assert.Single(result.ResultingUnits);
        Assert.Equal("ukr", result.ResultingUnits[0].Province.Code);
    }

    // -------------------------------------------------------------------------
    // 6.J.5 — Civil disorder: two fleets with different distance.
    // Russia controls BER (1 SC), has F SKA and F BER, → must remove 1.
    // Fleets cannot cross land. F SKA→NOR→STP_NC = distance 2.
    // F BER→BAL→BOT→STP_SC = distance 3. F BER (farthest) removed.
    // -------------------------------------------------------------------------
    [Fact]
    public void DATC_6_J_5_CivilDisorderTwoFleetsDifferentDistance()
    {
        var units = new List<Unit>
        {
            new(UnitType.Fleet, Power.Russia, new Province("ska")),
            new(UnitType.Fleet, Power.Russia, new Province("ber")),
        };
        var scs = new Dictionary<Power, IReadOnlyList<Province>>
        {
            [Power.Russia] = [new Province("ber")]
        };
        var result = Adjudicator.Adjudicate(new BuildAdjudicationRequest(Map, units, scs, []));

        Assert.Single(result.ResultingUnits);
        Assert.Equal("ska", result.ResultingUnits[0].Province.Code);
    }

    // -------------------------------------------------------------------------
    // 6.J.6 — Civil disorder: two fleets with equal distance.
    // Russia controls BER (1 SC), has F BER and F HEL, → must remove 1.
    // Both have the same distance to Russian home SCs (fleets cannot cut through
    // land: BER cannot reach WAR by fleet, so its path is longer).
    // Alphabetical tiebreaker: F BER removed ("ber" < "hel").
    // -------------------------------------------------------------------------
    [Fact]
    public void DATC_6_J_6_CivilDisorderTwoFleetsEqualDistance()
    {
        var units = new List<Unit>
        {
            new(UnitType.Fleet, Power.Russia, new Province("ber")),
            new(UnitType.Fleet, Power.Russia, new Province("hel")),
        };
        var scs = new Dictionary<Power, IReadOnlyList<Province>>
        {
            [Power.Russia] = [new Province("ber")]
        };
        var result = Adjudicator.Adjudicate(new BuildAdjudicationRequest(Map, units, scs, []));

        Assert.Single(result.ResultingUnits);
        Assert.Equal("hel", result.ResultingUnits[0].Province.Code);
    }

    // -------------------------------------------------------------------------
    // 6.J.7 — Civil disorder: two fleets and army with equal distance.
    // Russia controls STP and MOS (2 SCs), has A BOH, F SKA, F NTH → must remove 1.
    // All three are distance 2 from a Russian home SC. Fleets take precedence over
    // armies (fleets removed first). Among the fleets, alphabetical: NTH < SKA →
    // F NTH removed. A BOH and F SKA remain.
    // -------------------------------------------------------------------------
    [Fact]
    public void DATC_6_J_7_CivilDisorderTwoFleetsAndArmyEqualDistance()
    {
        var units = new List<Unit>
        {
            new(UnitType.Army,  Power.Russia, new Province("boh")),
            new(UnitType.Fleet, Power.Russia, new Province("ska")),
            new(UnitType.Fleet, Power.Russia, new Province("nth")),
        };
        var scs = new Dictionary<Power, IReadOnlyList<Province>>
        {
            [Power.Russia] = [new Province("stp"), new Province("mos")]
        };
        var result = Adjudicator.Adjudicate(new BuildAdjudicationRequest(Map, units, scs, []));

        Assert.Equal(2, result.ResultingUnits.Count);
        Assert.DoesNotContain(result.ResultingUnits, u => u.Province.Code == "nth");
        Assert.Contains(result.ResultingUnits,       u => u.Province.Code == "boh");
        Assert.Contains(result.ResultingUnits,       u => u.Province.Code == "ska");
    }

    // -------------------------------------------------------------------------
    // 6.J.8 — Civil disorder: fleet with shorter distance than army.
    // Russia controls STP (1 SC), has A TYR and F BAL → must remove 1.
    // F BAL→BOT→STP_SC = distance 2. A TYR→BOH→SIL→WAR = distance 3.
    // A TYR (farthest) removed; F BAL remains.
    // -------------------------------------------------------------------------
    [Fact]
    public void DATC_6_J_8_CivilDisorderFleetShorterDistanceThanArmy()
    {
        var units = new List<Unit>
        {
            new(UnitType.Army,  Power.Russia, new Province("tyr")),
            new(UnitType.Fleet, Power.Russia, new Province("bal")),
        };
        var scs = new Dictionary<Power, IReadOnlyList<Province>>
        {
            [Power.Russia] = [new Province("stp")]
        };
        var result = Adjudicator.Adjudicate(new BuildAdjudicationRequest(Map, units, scs, []));

        Assert.Single(result.ResultingUnits);
        Assert.Equal("bal", result.ResultingUnits[0].Province.Code);
    }

    // -------------------------------------------------------------------------
    // 6.J.9 — Civil disorder: distance must be counted from BOTH coasts.
    // For bicoastal provinces (STP), the shortest distance via either coast is used.
    //
    // (a) Russia controls STP (1 SC), has A TYR and F BAL.
    //     F BAL→BOT→STP_SC = distance 2 (shorter than STP_NC path).
    //     A TYR has distance 3 (by land). → A TYR removed.
    //
    // (b) Russia controls STP (1 SC), has A TYR and F SKA.
    //     F SKA→NOR→STP_NC = distance 2 (shorter than STP_SC path).
    //     A TYR has distance 3. → A TYR removed.
    // -------------------------------------------------------------------------
    [Theory]
    [InlineData("bal")]   // F BAL reaches STP via south coast
    [InlineData("ska")]   // F SKA reaches STP via north coast
    public void DATC_6_J_9_CivilDisorderMustBeCountedFromBothCoasts(string fleetProvince)
    {
        var units = new List<Unit>
        {
            new(UnitType.Army,  Power.Russia, new Province("tyr")),
            new(UnitType.Fleet, Power.Russia, new Province(fleetProvince)),
        };
        var scs = new Dictionary<Power, IReadOnlyList<Province>>
        {
            [Power.Russia] = [new Province("stp")]
        };
        var result = Adjudicator.Adjudicate(new BuildAdjudicationRequest(Map, units, scs, []));

        Assert.Single(result.ResultingUnits);
        Assert.Equal(fleetProvince, result.ResultingUnits[0].Province.Code);
    }

    // -------------------------------------------------------------------------
    // 6.J.10 — Civil disorder: armies may traverse sea areas when counting distance.
    // Italy controls GRE and NAP (2 SCs), has F ION, A GRE, A SIL → must remove 1.
    // A GRE can "traverse" ION to reach NAP: GRE→ION→NAP = distance 2.
    // A SIL→BOH→TYR→VEN = distance 3 (must go overland, no adjacent sea).
    // A SIL (farthest) removed; F ION and A GRE remain.
    // -------------------------------------------------------------------------
    [Fact]
    public void DATC_6_J_10_CivilDisorderCountingConvoyingDistance()
    {
        var units = new List<Unit>
        {
            new(UnitType.Fleet, Power.Italy, new Province("ion")),
            new(UnitType.Army,  Power.Italy, new Province("gre")),
            new(UnitType.Army,  Power.Italy, new Province("sil")),
        };
        var scs = new Dictionary<Power, IReadOnlyList<Province>>
        {
            [Power.Italy] = [new Province("gre"), new Province("nap")]
        };
        var result = Adjudicator.Adjudicate(new BuildAdjudicationRequest(Map, units, scs, []));

        Assert.Equal(2, result.ResultingUnits.Count);
        Assert.DoesNotContain(result.ResultingUnits, u => u.Province.Code == "sil");
        Assert.Contains(result.ResultingUnits,       u => u.Province.Code == "gre");
        Assert.Contains(result.ResultingUnits,       u => u.Province.Code == "ion");
    }

    // -------------------------------------------------------------------------
    // 6.J.11 — Civil disorder: distance counted without a convoying fleet.
    // Italy controls GRE (1 SC), has A GRE and A SIL → must remove 1.
    // Even without F ION present, sea areas still count as 1 step for army distance.
    // A GRE→ION (sea, 1)→NAP = distance 2.  A SIL→BOH→TYR→VEN = distance 3.
    // A SIL (farthest) removed; A GRE remains.
    // -------------------------------------------------------------------------
    [Fact]
    public void DATC_6_J_11_CivilDisorderCountingDistanceWithoutConvoyingFleet()
    {
        var units = new List<Unit>
        {
            new(UnitType.Army, Power.Italy, new Province("gre")),
            new(UnitType.Army, Power.Italy, new Province("sil")),
        };
        var scs = new Dictionary<Power, IReadOnlyList<Province>>
        {
            [Power.Italy] = [new Province("gre")]
        };
        var result = Adjudicator.Adjudicate(new BuildAdjudicationRequest(Map, units, scs, []));

        Assert.Single(result.ResultingUnits);
        Assert.Equal("gre", result.ResultingUnits[0].Province.Code);
    }
}
