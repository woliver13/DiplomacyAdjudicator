using woliver13.DiplomacyAdjudicator.Core.Adjudication;
using woliver13.DiplomacyAdjudicator.Core.Domain;
using woliver13.DiplomacyAdjudicator.Core.Map;
using woliver13.DiplomacyAdjudicator.Core.Parsing;
using woliver13.DiplomacyAdjudicator.Core.Rulesets;
using Microsoft.AspNetCore.Mvc;

namespace woliver13.DiplomacyAdjudicator.Api.Controllers;

[ApiController]
[Route("adjudicate")]
public sealed class AdjudicationController(
    IMovementAdjudicator movement,
    IRetreatAdjudicator retreat,
    IBuildAdjudicator build,
    IRulesetRegistry rulesets,
    IOrderParserFactory parserFactory) : ControllerBase
{
    // -------------------------------------------------------------------------
    // POST /adjudicate/movement
    // -------------------------------------------------------------------------

    [HttpPost("movement")]
    public IActionResult Movement([FromBody] MovementRequest req)
    {
        var ruleset = req.Ruleset ?? "standard_2000";
        if (!rulesets.IsKnown(ruleset))
            return BadRequest(new { error = $"Unknown ruleset '{ruleset}'.", supportedRulesets = rulesets.SupportedRulesets });

        var map = rulesets.GetMap(ruleset);

        var invalidTypes = req.Units.Select(u => u.Type)
            .Concat(req.Orders.Select(o => o.UnitType))
            .Where(t => !IsKnownUnitType(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (invalidTypes.Count > 0)
            return BadRequest(new { error = $"Unknown unit type(s): {string.Join(", ", invalidTypes)}" });

        var unknownProvinces = req.Units.Select(u => u.Province)
            .Concat(req.Orders.Select(o => o.Province))
            .Where(p => !map.IsValidProvince(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (unknownProvinces.Count > 0)
            return BadRequest(new { error = $"Unknown province code(s): {string.Join(", ", unknownProvinces)}" });

        var parser = parserFactory.Create(map);
        var units  = req.Units.Select(ToUnit).ToList();
        var orders = req.Orders.Select(o => parser.Parse(ToUnit(o), o.OrderText)).ToList();
        var scs    = ToSupplyCenters(req.SupplyCenters);

        var result = movement.Adjudicate(new MovementAdjudicationRequest(map, units, orders, scs));

        return Ok(new MovementResponse(
            result.OrderResults.Select(r => new OrderResultResponse(
                r.Order.Unit.Province.Code,
                r.Outcome.ToString(),
                r.Reason)).ToList(),
            result.DislodgedUnits.Select(d => new DislodgedUnitResponse(
                FromUnit(d.Unit),
                d.AttackedFrom.Code,
                d.RetreatOptions.Select(p => p.Code).ToList())).ToList(),
            result.NextPhase.ToString()));
    }

    // -------------------------------------------------------------------------
    // POST /adjudicate/retreat
    // -------------------------------------------------------------------------

    [HttpPost("retreat")]
    public IActionResult Retreat([FromBody] RetreatRequest req)
    {
        var ruleset = req.Ruleset ?? "standard_2000";
        if (!rulesets.IsKnown(ruleset))
            return BadRequest(new { error = $"Unknown ruleset '{ruleset}'.", supportedRulesets = rulesets.SupportedRulesets });

        var map = rulesets.GetMap(ruleset);

        var invalidTypes = req.DislodgedUnits.Select(d => d.Unit.Type)
            .Concat(req.RetreatOrders.Select(o => o.UnitType))
            .Where(t => !IsKnownUnitType(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (invalidTypes.Count > 0)
            return BadRequest(new { error = $"Unknown unit type(s): {string.Join(", ", invalidTypes)}" });

        var unknownProvinces = req.DislodgedUnits.Select(d => d.Unit.Province)
            .Concat(req.RetreatOrders.Select(o => o.Province))
            .Where(p => !map.IsValidProvince(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (unknownProvinces.Count > 0)
            return BadRequest(new { error = $"Unknown province code(s): {string.Join(", ", unknownProvinces)}" });

        var parser = parserFactory.Create(map);

        var dislodged = req.DislodgedUnits.Select(d => new DislodgedUnit(
            ToUnit(d.Unit),
            new Province(d.AttackedFrom.ToLowerInvariant()),
            d.RetreatOptions.Select(p => new Province(p.ToLowerInvariant())).ToList())).ToList();

        var retreatOrders = req.RetreatOrders
            .Select(o => parser.Parse(ToUnit(o), o.OrderText))
            .OfType<RetreatOrder>()
            .ToList();

        // Units with no retreat order are handled by the adjudicator as disbands.
        var result = retreat.Adjudicate(new RetreatAdjudicationRequest(dislodged, retreatOrders));

        return Ok(new RetreatResponse(
            result.OrderResults.Select(r => new OrderResultResponse(
                r.Order.Unit.Province.Code,
                r.Outcome.ToString(),
                r.Reason)).ToList(),
            result.SurvivedUnits.Select(FromUnit).ToList()));
    }

    // -------------------------------------------------------------------------
    // POST /adjudicate/build
    // -------------------------------------------------------------------------

    [HttpPost("build")]
    public IActionResult Build([FromBody] BuildRequest req)
    {
        var ruleset = req.Ruleset ?? "standard_2000";
        if (!rulesets.IsKnown(ruleset))
            return BadRequest(new { error = $"Unknown ruleset '{ruleset}'.", supportedRulesets = rulesets.SupportedRulesets });

        var map = rulesets.GetMap(ruleset);

        var invalidTypes = req.Units.Select(u => u.Type)
            .Concat(req.BuildOrders.Select(o => o.UnitType))
            .Where(t => !IsKnownUnitType(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (invalidTypes.Count > 0)
            return BadRequest(new { error = $"Unknown unit type(s): {string.Join(", ", invalidTypes)}" });

        var unknownProvinces = req.Units.Select(u => u.Province)
            .Concat(req.BuildOrders.Select(o => o.Province))
            .Where(p => !map.IsValidProvince(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (unknownProvinces.Count > 0)
            return BadRequest(new { error = $"Unknown province code(s): {string.Join(", ", unknownProvinces)}" });

        var parser = parserFactory.Create(map);
        var units  = req.Units.Select(ToUnit).ToList();
        var scs    = ToSupplyCenters(req.SupplyCenters);
        var orders = req.BuildOrders.Select(o => parser.Parse(ToUnit(o), o.OrderText)).ToList();

        var result = build.Adjudicate(new BuildAdjudicationRequest(map, units, scs, orders));

        return Ok(new BuildResponse(
            result.OrderResults.Select(r => new OrderResultResponse(
                r.Order.Unit.Province.Code,
                r.Outcome.ToString(),
                r.Reason)).ToList(),
            result.ResultingUnits.Select(FromUnit).ToList()));
    }

    // -------------------------------------------------------------------------
    // Mapping helpers
    // -------------------------------------------------------------------------

    private static Unit ToUnit(UnitRequest u)
        => new(ParseUnitType(u.Type), new Power(u.Power), new Province(u.Province.ToLowerInvariant().Replace('/', '_')));

    private static UnitResponse FromUnit(Unit u)
        => new(u.Power.Name, u.Type.ToString().ToLowerInvariant(), u.Province.Code);

    private static IReadOnlyDictionary<Power, IReadOnlyList<Province>> ToSupplyCenters(
        Dictionary<string, IReadOnlyList<string>> raw)
        => raw.ToDictionary(
            kv => new Power(kv.Key),
            kv => (IReadOnlyList<Province>)kv.Value.Select(p => new Province(p.ToLowerInvariant())).ToList());

    private static bool IsKnownUnitType(string s)
        => s.ToLowerInvariant() is "army" or "a" or "fleet" or "f";

    private static UnitType ParseUnitType(string s) => s.ToLowerInvariant() switch
    {
        "army"  or "a" => UnitType.Army,
        "fleet" or "f" => UnitType.Fleet,
        _ => throw new ArgumentException($"Unknown unit type: {s}")
    };
}

// -------------------------------------------------------------------------
// Request models
// -------------------------------------------------------------------------

public record UnitRequest(string Power, string Type, string Province);

public record OrderRequest(string Power, string UnitType, string Province, string OrderText)
{
    // Convenience: treat as UnitRequest for parsing
    public static implicit operator UnitRequest(OrderRequest o) => new(o.Power, o.UnitType, o.Province);
}

public record DislodgedUnitRequest(
    UnitRequest Unit,
    string AttackedFrom,
    IReadOnlyList<string> RetreatOptions);

public record MovementRequest(
    string? Ruleset,
    IReadOnlyList<UnitRequest> Units,
    IReadOnlyList<OrderRequest> Orders,
    Dictionary<string, IReadOnlyList<string>> SupplyCenters);

public record RetreatRequest(
    string? Ruleset,
    IReadOnlyList<DislodgedUnitRequest> DislodgedUnits,
    IReadOnlyList<OrderRequest> RetreatOrders);

public record BuildRequest(
    string? Ruleset,
    IReadOnlyList<UnitRequest> Units,
    Dictionary<string, IReadOnlyList<string>> SupplyCenters,
    IReadOnlyList<OrderRequest> BuildOrders);

// -------------------------------------------------------------------------
// Response models
// -------------------------------------------------------------------------

public record UnitResponse(string Power, string Type, string Province);
public record OrderResultResponse(string Province, string Outcome, string? Reason);
public record DislodgedUnitResponse(UnitResponse Unit, string AttackedFrom, IReadOnlyList<string> RetreatOptions);

public record MovementResponse(
    IReadOnlyList<OrderResultResponse> OrderResults,
    IReadOnlyList<DislodgedUnitResponse> DislodgedUnits,
    string NextPhase);

public record RetreatResponse(
    IReadOnlyList<OrderResultResponse> OrderResults,
    IReadOnlyList<UnitResponse> SurvivedUnits);

public record BuildResponse(
    IReadOnlyList<OrderResultResponse> OrderResults,
    IReadOnlyList<UnitResponse> ResultingUnits);
