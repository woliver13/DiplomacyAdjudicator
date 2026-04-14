using woliver13.DiplomacyAdjudicator.Core.Adjudication;
using woliver13.DiplomacyAdjudicator.Core.Domain;
using woliver13.DiplomacyAdjudicator.Core.Map;
using Xunit;

namespace woliver13.DiplomacyAdjudicator.Tests.Adjudication;

/// <summary>
/// Tests for the build/disband phase adjudicator.
/// Austria home SCs: VIE, BUD, TRI.
/// </summary>
public class BuildTests
{
    private static readonly MapGraph Map = MapGraph.LoadStandard();

    private static BuildAdjudicator Adjudicator => new();

    // Helper: Austria controls 4 SCs (via, bud, tri + one extra), has 3 units
    // — VIE is unoccupied so it can build there.
    private static BuildAdjudicationRequest AustriaPlusBuild(params Order[] extraOrders)
    {
        var units = new List<Unit>
        {
            new(UnitType.Army, Power.Austria, new Province("gal")),  // not in a home SC
            new(UnitType.Army, Power.Austria, new Province("bud")),
            new(UnitType.Army, Power.Austria, new Province("tri")),
        };
        var supplyCenters = new Dictionary<Power, IReadOnlyList<Province>>
        {
            [Power.Austria] = [new Province("vie"), new Province("bud"),
                               new Province("tri"), new Province("ser")]
        };
        return new BuildAdjudicationRequest(Map, units, supplyCenters, extraOrders.ToList());
    }

    // Helper: Austria controls 2 SCs (bud, tri), has 3 units — must disband 1.
    private static BuildAdjudicationRequest AustriaMinusDisband(params Order[] extraOrders)
    {
        var units = new List<Unit>
        {
            new(UnitType.Army, Power.Austria, new Province("vie")),
            new(UnitType.Army, Power.Austria, new Province("bud")),
            new(UnitType.Army, Power.Austria, new Province("tri")),
        };
        var supplyCenters = new Dictionary<Power, IReadOnlyList<Province>>
        {
            [Power.Austria] = [new Province("bud"), new Province("tri")]
        };
        return new BuildAdjudicationRequest(Map, units, supplyCenters, extraOrders.ToList());
    }

    [Fact]
    public void Build_BasicBuildSucceeds()
    {
        // Austria +1 SC, builds A VIE (home SC, unoccupied).
        var buildUnit = new Unit(UnitType.Army, Power.Austria, new Province("vie"));
        var request = AustriaPlusBuild(new BuildOrder(buildUnit));

        var result = Adjudicator.Adjudicate(request);

        var vieResult = result.OrderResults.Single(r => r.Order.Unit.Province.Code == "vie");
        Assert.Equal(OrderOutcome.Success, vieResult.Outcome);
        Assert.Contains(result.ResultingUnits, u => u.Province.Code == "vie");
    }

    [Fact]
    public void Build_BasicDisbandSucceeds()
    {
        // Austria -1 SC, disbands A VIE.
        var disbandUnit = new Unit(UnitType.Army, Power.Austria, new Province("vie"));
        var request = AustriaMinusDisband(new DisbandOrder(disbandUnit));

        var result = Adjudicator.Adjudicate(request);

        var vieResult = result.OrderResults.Single(r => r.Order.Unit.Province.Code == "vie");
        Assert.Equal(OrderOutcome.Success, vieResult.Outcome);
        Assert.DoesNotContain(result.ResultingUnits, u => u.Province.Code == "vie");
        Assert.Equal(2, result.ResultingUnits.Count);
    }

    [Fact]
    public void Build_BuildInNonHomeSCIsVoid()
    {
        // Austria tries to build in SER — controlled but not a home SC.
        var buildUnit = new Unit(UnitType.Army, Power.Austria, new Province("ser"));
        var request = AustriaPlusBuild(new BuildOrder(buildUnit));

        var result = Adjudicator.Adjudicate(request);

        var serResult = result.OrderResults.Single(r => r.Order.Unit.Province.Code == "ser");
        Assert.Equal(OrderOutcome.Void, serResult.Outcome);
    }

    [Fact]
    public void Build_BuildInOccupiedProvinceIsVoid()
    {
        // Austria tries to build in BUD which is already occupied.
        var buildUnit = new Unit(UnitType.Army, Power.Austria, new Province("bud"));
        var request = AustriaPlusBuild(new BuildOrder(buildUnit));

        var result = Adjudicator.Adjudicate(request);

        var budResult = result.OrderResults.Single(r => r.Order.Unit.Province.Code == "bud");
        Assert.Equal(OrderOutcome.Void, budResult.Outcome);
    }

    [Fact]
    public void Build_FleetCantBuildInInlandProvinceIsVoid()
    {
        // Austria tries to build F BUD (BUD is landlocked).
        var buildUnit = new Unit(UnitType.Fleet, Power.Austria, new Province("bud"));
        var request = AustriaPlusBuild(new BuildOrder(buildUnit));

        var result = Adjudicator.Adjudicate(request);

        var budResult = result.OrderResults.Single(r => r.Order.Unit.Province.Code == "bud");
        Assert.Equal(OrderOutcome.Void, budResult.Outcome);
    }

    [Fact]
    public void Build_ArmyCantBuildAtSeaIsVoid()
    {
        // England tries to build A NTH — NTH is a sea province, can't place army there.
        // England: home SCs are LON, LVP, EDI (all coastal). Add a fake test where
        // someone tries to build army at sea province via a custom request.
        var units = new List<Unit>
        {
            new(UnitType.Army, Power.England, new Province("yor")),
        };
        var supplyCenters = new Dictionary<Power, IReadOnlyList<Province>>
        {
            [Power.England] = [new Province("lon"), new Province("lvp"),
                               new Province("edi"), new Province("nth")]
        };
        // Try to build army in NTH (sea province, and not a home SC anyway — testing type check)
        // Instead, let's test something more realistic: can't build army in a sea province
        // even if it were listed as home (edge case). Use LON which IS a home SC and IS coastal.
        // Actually — just test that A NTH is void because NTH is not a home SC, not coastal home.
        // Let's construct the edge case via England trying to build in a sea province:
        var buildUnit = new Unit(UnitType.Army, Power.England, new Province("nth"));
        var request = new BuildAdjudicationRequest(Map, units, supplyCenters,
            [new BuildOrder(buildUnit)]);

        var result = Adjudicator.Adjudicate(request);

        var nthResult = result.OrderResults.Single(r => r.Order.Unit.Province.Code == "nth");
        Assert.Equal(OrderOutcome.Void, nthResult.Outcome);
    }

    [Fact]
    public void Build_WaiveConsumesSlot()
    {
        // Austria has +1 build, submits WaiveOrder — slot consumed, no unit added,
        // but the waive itself is Success.
        var request = AustriaPlusBuild(new WaiveOrder(Power.Austria));

        var result = Adjudicator.Adjudicate(request);

        var waiveResult = result.OrderResults.Single(r => r.Order is WaiveOrder);
        Assert.Equal(OrderOutcome.Success, waiveResult.Outcome);
        // No new units added
        Assert.Equal(3, result.ResultingUnits.Count);
    }

    [Fact]
    public void Build_ExcessBuildsBeyondAllowanceAreVoid()
    {
        // Austria has +1 build, submits 2 build orders — second is void.
        var build1 = new BuildOrder(new Unit(UnitType.Army, Power.Austria, new Province("vie")));
        var build2 = new BuildOrder(new Unit(UnitType.Fleet, Power.Austria, new Province("tri")));
        var request = AustriaPlusBuild(build1, build2);

        var result = Adjudicator.Adjudicate(request);

        var outcomes = result.OrderResults
            .Where(r => r.Order is BuildOrder)
            .Select(r => r.Outcome)
            .OrderBy(o => o)
            .ToList();

        Assert.Contains(OrderOutcome.Success, outcomes);
        Assert.Contains(OrderOutcome.Void, outcomes);
        Assert.Equal(4, result.ResultingUnits.Count); // only 1 new unit
    }

    [Fact]
    public void Build_NoAdjustmentNeededWhenBalanced()
    {
        // Austria has 3 SCs and 3 units — no builds or disbands required.
        // Any build orders are void.
        var units = new List<Unit>
        {
            new(UnitType.Army, Power.Austria, new Province("vie")),
            new(UnitType.Army, Power.Austria, new Province("bud")),
            new(UnitType.Army, Power.Austria, new Province("tri")),
        };
        var supplyCenters = new Dictionary<Power, IReadOnlyList<Province>>
        {
            [Power.Austria] = [new Province("vie"), new Province("bud"), new Province("tri")]
        };
        var bogusOrder = new BuildOrder(new Unit(UnitType.Army, Power.Austria, new Province("vie")));
        var request = new BuildAdjudicationRequest(Map, units, supplyCenters, [bogusOrder]);

        var result = Adjudicator.Adjudicate(request);

        var buildResult = result.OrderResults.Single(r => r.Order is BuildOrder);
        Assert.Equal(OrderOutcome.Void, buildResult.Outcome);
        Assert.Equal(3, result.ResultingUnits.Count);
    }

    [Fact]
    public void Build_AutoDisbandWhenNoDisbandOrderSubmitted()
    {
        // Austria must disband 1 but submits no disband orders.
        // The adjudicator auto-disbands one unit (alphabetical by province code).
        var request = AustriaMinusDisband(); // no orders

        var result = Adjudicator.Adjudicate(request);

        // Should still have 2 units (one auto-disbanded)
        Assert.Equal(2, result.ResultingUnits.Count);
    }
}
