using DiplomacyAdjudicator.Core.Adjudication;
using DiplomacyAdjudicator.Core.Domain;
using DiplomacyAdjudicator.Core.Map;
using Xunit;

namespace DiplomacyAdjudicator.Tests.Adjudication;

/// <summary>
/// DATC Section 6.H — Retreating
/// Tests cover the RetreatAdjudicator directly (units 1–3, 6) and the
/// retreat-option computation inside MovementAdjudicator (units 4, 5, 7).
/// </summary>
public class DATC_6_H_Tests
{
    private static readonly MapGraph Map = MapGraph.LoadStandard();

    // -------------------------------------------------------------------------
    // Direct RetreatAdjudicator tests
    // -------------------------------------------------------------------------

    [Fact]
    public void DATC_6_H_1_BasicSuccessfulRetreat()
    {
        // A VIE dislodged from BOH. Retreats to TYR (valid option).
        var dislodged = new DislodgedUnit(
            new Unit(UnitType.Army, Power.Austria, new Province("vie")),
            new Province("boh"),
            [new Province("tyr"), new Province("gal")]);

        var request = new RetreatAdjudicationRequest(
            [dislodged],
            [new RetreatOrder(dislodged.Unit, new Province("tyr"))]);

        var result = new RetreatAdjudicator().Adjudicate(request);

        Assert.Equal(OrderOutcome.Success, result.OrderResults[0].Outcome);
        Assert.Single(result.SurvivedUnits);
        Assert.Equal("tyr", result.SurvivedUnits[0].Province.Code);
    }

    [Fact]
    public void DATC_6_H_2_NoRetreatOrderAutoDisband()
    {
        // No retreat order submitted — unit is disbanded.
        var dislodged = new DislodgedUnit(
            new Unit(UnitType.Army, Power.Austria, new Province("vie")),
            new Province("boh"),
            [new Province("tyr")]);

        var request = new RetreatAdjudicationRequest([dislodged], []);

        var result = new RetreatAdjudicator().Adjudicate(request);

        Assert.Equal(OrderOutcome.Failure, result.OrderResults[0].Outcome);
        Assert.Empty(result.SurvivedUnits);
    }

    [Fact]
    public void DATC_6_H_3_TwoRetreatsToSameProvinceBothDisband()
    {
        // A BOH and A MUN both dislodged, both try to retreat to TYR — both disbanded.
        var d1 = new DislodgedUnit(
            new Unit(UnitType.Army, Power.Austria, new Province("boh")),
            new Province("sil"),
            [new Province("tyr"), new Province("mun")]);
        var d2 = new DislodgedUnit(
            new Unit(UnitType.Army, Power.Germany, new Province("mun")),
            new Province("ber"),
            [new Province("tyr"), new Province("ruh")]);

        var request = new RetreatAdjudicationRequest(
            [d1, d2],
            [
                new RetreatOrder(d1.Unit, new Province("tyr")),
                new RetreatOrder(d2.Unit, new Province("tyr")),
            ]);

        var result = new RetreatAdjudicator().Adjudicate(request);

        Assert.All(result.OrderResults, r => Assert.Equal(OrderOutcome.Bounced, r.Outcome));
        Assert.Empty(result.SurvivedUnits);
    }

    [Fact]
    public void DATC_6_H_6_RetreatToProvinceNotInOptionsIsVoid()
    {
        // BUD not in options — retreat is void (unit disbanded).
        var dislodged = new DislodgedUnit(
            new Unit(UnitType.Army, Power.Austria, new Province("vie")),
            new Province("boh"),
            [new Province("tyr"), new Province("gal")]);

        var request = new RetreatAdjudicationRequest(
            [dislodged],
            [new RetreatOrder(dislodged.Unit, new Province("bud"))]);

        var result = new RetreatAdjudicator().Adjudicate(request);

        Assert.Equal(OrderOutcome.Void, result.OrderResults[0].Outcome);
        Assert.Empty(result.SurvivedUnits);
    }

    // -------------------------------------------------------------------------
    // Integration tests: MovementAdjudicator retreat-option computation
    // -------------------------------------------------------------------------

    [Fact]
    public void DATC_6_H_4_CannotRetreatToAttackerOrigin()
    {
        // A BOH→VIE (supported by BUD, which is adjacent to VIE) dislodges A VIE.
        // VIE's retreat options must NOT include BOH (the attacker's origin).
        var adjudicator = new MovementAdjudicator();

        var vie = new Unit(UnitType.Army, Power.Austria, new Province("vie"));
        var boh = new Unit(UnitType.Army, Power.Germany, new Province("boh"));
        var bud = new Unit(UnitType.Army, Power.Germany, new Province("bud"));

        var result = adjudicator.Adjudicate(new MovementAdjudicationRequest(Map,
            [vie, boh, bud],
            [
                new HoldOrder(vie),
                new MoveOrder(boh, new Province("vie")),
                new SupportMoveOrder(bud, new Province("boh"), new Province("vie")),
            ],
            new Dictionary<Power, IReadOnlyList<Province>>()));

        var dislodged = result.DislodgedUnits.Single(d => d.Unit.Province.Code == "vie");
        Assert.DoesNotContain(dislodged.RetreatOptions,
            p => MapGraph.BaseCode(p.Code) == "boh");
    }

    [Fact]
    public void DATC_6_H_5_CannotRetreatToStandoffProvince()
    {
        // A BOH→VIE (supported by BUD, adjacent to VIE) dislodges A VIE.
        // Meanwhile A WAR→GAL and A SIL→GAL both fail — standoff in GAL.
        // VIE's retreat options must NOT include GAL (standoff province).
        // (TYR is not adjacent to GAL; WAR and SIL both are.)
        var adjudicator = new MovementAdjudicator();

        var vie = new Unit(UnitType.Army, Power.Austria, new Province("vie"));
        var boh = new Unit(UnitType.Army, Power.Germany, new Province("boh"));
        var bud = new Unit(UnitType.Army, Power.Germany, new Province("bud"));
        var war = new Unit(UnitType.Army, Power.Russia, new Province("war"));
        var sil = new Unit(UnitType.Army, Power.Russia, new Province("sil"));

        var result = adjudicator.Adjudicate(new MovementAdjudicationRequest(Map,
            [vie, boh, bud, war, sil],
            [
                new HoldOrder(vie),
                new MoveOrder(boh, new Province("vie")),
                new SupportMoveOrder(bud, new Province("boh"), new Province("vie")),
                new MoveOrder(war, new Province("gal")),
                new MoveOrder(sil, new Province("gal")),
            ],
            new Dictionary<Power, IReadOnlyList<Province>>()));

        var dislodged = result.DislodgedUnits.Single(d => d.Unit.Province.Code == "vie");
        Assert.DoesNotContain(dislodged.RetreatOptions,
            p => MapGraph.BaseCode(p.Code) == "gal");
    }

    [Fact]
    public void DATC_6_H_7_CanRetreatToVacatedProvince()
    {
        // A MUN→SIL succeeds (SIL empty). A BOH is dislodged by A TYR (supported by VIE).
        // BOH CAN retreat to MUN (vacated by the successful MUN→SIL move).
        var adjudicator = new MovementAdjudicator();

        var mun = new Unit(UnitType.Army, Power.Germany, new Province("mun"));
        var boh = new Unit(UnitType.Army, Power.Germany, new Province("boh"));
        var tyr = new Unit(UnitType.Army, Power.Russia, new Province("tyr"));
        var vie = new Unit(UnitType.Army, Power.Russia, new Province("vie"));

        var result = adjudicator.Adjudicate(new MovementAdjudicationRequest(Map,
            [mun, boh, tyr, vie],
            [
                new MoveOrder(mun, new Province("sil")),
                new HoldOrder(boh),
                new MoveOrder(tyr, new Province("boh")),
                new SupportMoveOrder(vie, new Province("tyr"), new Province("boh")),
            ],
            new Dictionary<Power, IReadOnlyList<Province>>()));

        var dislodged = result.DislodgedUnits.Single(d => d.Unit.Province.Code == "boh");
        Assert.Contains(dislodged.RetreatOptions,
            p => MapGraph.BaseCode(p.Code) == "mun");
        // Cannot retreat to TYR (attacker's origin)
        Assert.DoesNotContain(dislodged.RetreatOptions,
            p => MapGraph.BaseCode(p.Code) == "tyr");
    }
}
