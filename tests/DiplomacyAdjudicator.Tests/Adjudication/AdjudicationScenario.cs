using DiplomacyAdjudicator.Core.Adjudication;
using DiplomacyAdjudicator.Core.Domain;
using DiplomacyAdjudicator.Core.Map;
using DiplomacyAdjudicator.Core.Parsing;

namespace DiplomacyAdjudicator.Tests.Adjudication;

/// <summary>
/// Fluent builder for movement adjudication test scenarios.
/// Usage:
///   new AdjudicationScenario()
///     .WithUnit("austria", "army", "vie")
///     .WithOrder("austria", "army", "vie", "move tyr")
///     .AssertResult("vie", OrderOutcome.Failure);
/// </summary>
internal sealed class AdjudicationScenario
{
    private readonly List<Unit> _units = [];
    private readonly List<(string power, string unitType, string province, string orderText)> _rawOrders = [];
    private readonly List<(string province, OrderOutcome expected)> _assertions = [];
    private readonly MapGraph _map = MapGraph.LoadStandard();

    internal AdjudicationScenario WithUnit(string power, string unitType, string province)
    {
        _units.Add(new Unit(
            ParseUnitType(unitType),
            new Power(power),
            new Province(province)));
        return this;
    }

    internal AdjudicationScenario WithOrder(
        string power, string unitType, string province, string orderText)
    {
        _rawOrders.Add((power, unitType, province, orderText));
        return this;
    }

    internal AdjudicationScenario AssertOutcome(string province, OrderOutcome expected)
    {
        _assertions.Add((province, expected));
        return this;
    }

    internal void Run()
    {
        var parser = new OrderParser(_map);
        var orders = new List<Order>();

        foreach (var (power, unitType, province, text) in _rawOrders)
        {
            var unit = new Unit(ParseUnitType(unitType), new Power(power), new Province(province));
            var order = parser.Parse(unit, text);
            orders.Add(order);
        }

        // Units with no explicit order get Hold
        foreach (var unit in _units)
        {
            if (!orders.Any(o => o.Unit.Province == unit.Province))
                orders.Add(new HoldOrder(unit));
        }

        var adjudicator = new MovementAdjudicator(_map);
        var request = new MovementAdjudicationRequest(
            _units,
            orders,
            new Dictionary<Power, IReadOnlyList<Province>>());
        var result = adjudicator.Adjudicate(request);

        foreach (var (province, expected) in _assertions)
        {
            var orderResult = result.OrderResults
                .FirstOrDefault(r => r.Order.Unit.Province.Code == province);

            if (orderResult is null)
                throw new InvalidOperationException(
                    $"No order result found for province '{province}'.");

            Xunit.Assert.Equal(
                expected,
                orderResult.Outcome,
                $"Province '{province}': expected {expected} but got {orderResult.Outcome}. Reason: {orderResult.Reason}");
        }
    }

    private static UnitType ParseUnitType(string s) => s.ToLowerInvariant() switch
    {
        "army" or "a" => UnitType.Army,
        "fleet" or "f" => UnitType.Fleet,
        _ => throw new ArgumentException($"Unknown unit type: {s}")
    };
}
