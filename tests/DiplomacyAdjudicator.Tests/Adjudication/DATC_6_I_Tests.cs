using woliver13.DiplomacyAdjudicator.Core.Adjudication;
using woliver13.DiplomacyAdjudicator.Core.Domain;
using woliver13.DiplomacyAdjudicator.Core.Map;
using Xunit;

namespace woliver13.DiplomacyAdjudicator.Tests.Adjudication;

/// <summary>
/// DATC v2.4 Section 6.I — Building
/// </summary>
public class DATC_6_I_Tests
{
    private static readonly MapGraph Map = MapGraph.LoadStandard();
    private static BuildAdjudicator Adjudicator => new();

    // 6.I.1 — Build in unoccupied home supply center succeeds.
    // England controls LON, EDI, LVP + one extra (4 SCs), has 3 units. Can build in LVP.
    [Fact]
    public void DATC_6_I_1_BuildInUnoccupiedHomeScSucceeds()
    {
        var units = new List<Unit>
        {
            new(UnitType.Fleet, Power.England, new Province("lon")),
            new(UnitType.Fleet, Power.England, new Province("edi")),
            new(UnitType.Army,  Power.England, new Province("bel")),
        };
        var scs = new Dictionary<Power, IReadOnlyList<Province>>
        {
            [Power.England] = [new Province("lon"), new Province("edi"),
                               new Province("lvp"), new Province("hol")]
        };
        var unit = new Unit(UnitType.Army, Power.England, new Province("lvp"));
        var result = Adjudicator.Adjudicate(
            new BuildAdjudicationRequest(Map, units, scs, [new BuildOrder(unit)]));

        Assert.Equal(OrderOutcome.Success, result.OrderResults[0].Outcome);
        Assert.Equal(4, result.ResultingUnits.Count);
    }

    // 6.I.2 — Build in occupied home center is void.
    // LON is already occupied by a fleet — cannot build there.
    [Fact]
    public void DATC_6_I_2_BuildInOccupiedHomeCenterIsVoid()
    {
        var units = new List<Unit>
        {
            new(UnitType.Fleet, Power.England, new Province("lon")),
            new(UnitType.Fleet, Power.England, new Province("edi")),
        };
        var scs = new Dictionary<Power, IReadOnlyList<Province>>
        {
            [Power.England] = [new Province("lon"), new Province("edi"),
                               new Province("lvp"), new Province("hol")]
        };
        // Try to build in LON which is occupied
        var unit = new Unit(UnitType.Army, Power.England, new Province("lon"));
        var result = Adjudicator.Adjudicate(
            new BuildAdjudicationRequest(Map, units, scs, [new BuildOrder(unit)]));

        Assert.Equal(OrderOutcome.Void, result.OrderResults[0].Outcome);
        Assert.Equal(2, result.ResultingUnits.Count);
    }

    // 6.I.3 — Build in non-home supply center is void.
    // England controls HOL but HOL is not a home SC — build is void.
    [Fact]
    public void DATC_6_I_3_BuildInNonHomeScIsVoid()
    {
        var units = new List<Unit>
        {
            new(UnitType.Fleet, Power.England, new Province("lon")),
        };
        var scs = new Dictionary<Power, IReadOnlyList<Province>>
        {
            [Power.England] = [new Province("lon"), new Province("edi"),
                               new Province("lvp"), new Province("hol")]
        };
        var unit = new Unit(UnitType.Army, Power.England, new Province("hol"));
        var result = Adjudicator.Adjudicate(
            new BuildAdjudicationRequest(Map, units, scs, [new BuildOrder(unit)]));

        Assert.Equal(OrderOutcome.Void, result.OrderResults[0].Outcome);
    }

    // 6.I.4 — Fleet cannot be built in an inland province.
    // MUN is a home SC for Germany but it is inland — fleet build is void.
    [Fact]
    public void DATC_6_I_4_FleetCannotBeBuiltInlandProvince()
    {
        var units = new List<Unit>
        {
            new(UnitType.Army, Power.Germany, new Province("ber")),
        };
        var scs = new Dictionary<Power, IReadOnlyList<Province>>
        {
            [Power.Germany] = [new Province("ber"), new Province("kie"),
                               new Province("mun"), new Province("hol")]
        };
        var unit = new Unit(UnitType.Fleet, Power.Germany, new Province("mun"));
        var result = Adjudicator.Adjudicate(
            new BuildAdjudicationRequest(Map, units, scs, [new BuildOrder(unit)]));

        Assert.Equal(OrderOutcome.Void, result.OrderResults[0].Outcome);
    }

    // 6.I.5 — Waive consumes one build slot. No unit placed.
    [Fact]
    public void DATC_6_I_5_WaiveConsumesOneBuildSlot()
    {
        var units = new List<Unit>
        {
            new(UnitType.Fleet, Power.England, new Province("lon")),
        };
        var scs = new Dictionary<Power, IReadOnlyList<Province>>
        {
            [Power.England] = [new Province("lon"), new Province("edi")]
        };
        var result = Adjudicator.Adjudicate(
            new BuildAdjudicationRequest(Map, units, scs, [new WaiveOrder(Power.England)]));

        Assert.Equal(OrderOutcome.Success, result.OrderResults[0].Outcome);
        Assert.Single(result.ResultingUnits); // no unit added
    }

    // 6.I.6 — Disband when required. Germany has 3 units, 2 SCs — must disband 1.
    [Fact]
    public void DATC_6_I_6_DisbandWhenUnitExceedsSupplyCenters()
    {
        var units = new List<Unit>
        {
            new(UnitType.Army,  Power.Germany, new Province("ber")),
            new(UnitType.Fleet, Power.Germany, new Province("kie")),
            new(UnitType.Army,  Power.Germany, new Province("mun")),
        };
        var scs = new Dictionary<Power, IReadOnlyList<Province>>
        {
            [Power.Germany] = [new Province("ber"), new Province("kie")]
        };
        var disband = new DisbandOrder(new Unit(UnitType.Army, Power.Germany, new Province("mun")));
        var result = Adjudicator.Adjudicate(
            new BuildAdjudicationRequest(Map, units, scs, [disband]));

        Assert.Equal(OrderOutcome.Success, result.OrderResults[0].Outcome);
        Assert.Equal(2, result.ResultingUnits.Count);
        Assert.DoesNotContain(result.ResultingUnits,
            u => MapGraph.BaseCode(u.Province.Code) == "mun");
    }

    // 6.I.7 — Auto-disband: not enough disband orders submitted, units disbanded alphabetically.
    // Germany has 3 units (ber, kie, mun), 1 SC (ber). Must disband 2, submits 0.
    // Auto-disband picks ber then kie (alphabetical), but ber is the only SC... actually
    // auto-disband is just alphabetical by province code regardless of SC status.
    [Fact]
    public void DATC_6_I_7_AutoDisbandWhenInsufficientDisbandOrders()
    {
        var units = new List<Unit>
        {
            new(UnitType.Army,  Power.Germany, new Province("ber")),
            new(UnitType.Fleet, Power.Germany, new Province("kie")),
            new(UnitType.Army,  Power.Germany, new Province("mun")),
        };
        var scs = new Dictionary<Power, IReadOnlyList<Province>>
        {
            [Power.Germany] = [new Province("ber")]
        };
        // No disband orders submitted
        var result = Adjudicator.Adjudicate(
            new BuildAdjudicationRequest(Map, units, scs, []));

        // 3 units, 1 SC → must disband 2. Auto-disband: ber, kie (alphabetical).
        Assert.Single(result.ResultingUnits);
        Assert.Equal("mun", result.ResultingUnits[0].Province.Code);
    }
}
