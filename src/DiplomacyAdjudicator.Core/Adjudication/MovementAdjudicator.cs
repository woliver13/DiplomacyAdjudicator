using DiplomacyAdjudicator.Core.Domain;
using DiplomacyAdjudicator.Core.Map;

namespace DiplomacyAdjudicator.Core.Adjudication;

/// <summary>
/// Public entry point for movement adjudication.
/// Normalises orders (bicoastal destination resolution, fill-in holds),
/// delegates to MovementResolver, then maps boolean results to OrderOutcome
/// and identifies dislodged units.
/// </summary>
public sealed class MovementAdjudicator : IMovementAdjudicator
{
    private readonly MapGraph _map;

    public MovementAdjudicator(MapGraph map) => _map = map;

    public MovementAdjudicationResult Adjudicate(MovementAdjudicationRequest request)
    {
        var orders = NormalizeOrders(request.Units, request.Orders);

        var resolver = new MovementResolver(_map, request.Units, orders);
        var resolved = resolver.ResolveAll();

        // Provinces successfully entered this turn
        var successfullyEntered = resolved
            .Where(kvp => kvp.Key is MoveOrder && kvp.Value)
            .Select(kvp => MapGraph.BaseCode(((MoveOrder)kvp.Key).Destination.Code))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Units indexed by base province code for dislodged-unit lookup
        var unitsByBase = request.Units.ToDictionary(
            u => MapGraph.BaseCode(u.Province.Code),
            StringComparer.OrdinalIgnoreCase);

        // Orders indexed by unit base province for moved-out check
        var ordersByBase = orders.ToDictionary(
            o => MapGraph.BaseCode(o.Unit.Province.Code),
            StringComparer.OrdinalIgnoreCase);

        // First pass: compute base outcomes
        var resultMap = new Dictionary<string, OrderResult>(StringComparer.OrdinalIgnoreCase);
        foreach (var order in orders)
        {
            var outcome = ComputeOutcome(order, resolved);
            var baseCode = MapGraph.BaseCode(order.Unit.Province.Code);
            resultMap[baseCode] = new OrderResult(order, outcome);
        }

        // Second pass: mark dislodged units
        var dislodgedUnits = new List<DislodgedUnit>();

        foreach (var (order, success) in resolved)
        {
            if (order is not MoveOrder move || !success) continue;

            var destBase = MapGraph.BaseCode(move.Destination.Code);
            if (!unitsByBase.TryGetValue(destBase, out var displaced)) continue;

            // Check if the displaced unit successfully moved out
            bool movedOut = ordersByBase.TryGetValue(destBase, out var displacedOrder)
                && displacedOrder is MoveOrder displacedMove
                && resolved.GetValueOrDefault(displacedMove);

            if (movedOut) continue;

            // Override outcome to Dislodged for the displaced unit's order
            if (resultMap.TryGetValue(destBase, out var existing))
                resultMap[destBase] = existing with { Outcome = OrderOutcome.Dislodged };

            dislodgedUnits.Add(new DislodgedUnit(displaced, []));
        }

        var nextPhase = dislodgedUnits.Count > 0 ? PhaseType.Retreat : PhaseType.Build;

        return new MovementAdjudicationResult(
            resultMap.Values.ToList(),
            dislodgedUnits,
            nextPhase);
    }

    // -------------------------------------------------------------------------
    // Order normalisation
    // -------------------------------------------------------------------------

    private List<Order> NormalizeOrders(IReadOnlyList<Unit> units, IReadOnlyList<Order> providedOrders)
    {
        var normalized = new List<Order>();
        var orderedProvinceBases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var order in providedOrders)
        {
            orderedProvinceBases.Add(MapGraph.BaseCode(order.Unit.Province.Code));

            if (order is MoveOrder move)
            {
                // Resolve bicoastal destinations (auto-detect coast for fleets, strip coast for armies)
                var resolved = _map.ResolveDestination(move.Unit.Province, move.Destination, move.Unit.Type);
                // If resolved differs (coast detected or normalised), use the resolved province.
                // If null (ambiguous or unreachable), keep original — the resolver will fail it.
                normalized.Add(resolved is not null && resolved != move.Destination
                    ? move with { Destination = resolved }
                    : order);
            }
            else
            {
                normalized.Add(order);
            }
        }

        // Units with no explicit order default to Hold
        foreach (var unit in units)
        {
            var baseCode = MapGraph.BaseCode(unit.Province.Code);
            if (!orderedProvinceBases.Contains(baseCode))
                normalized.Add(new HoldOrder(unit));
        }

        return normalized;
    }

    // -------------------------------------------------------------------------
    // Outcome mapping
    // -------------------------------------------------------------------------

    private static OrderOutcome ComputeOutcome(Order order, IReadOnlyDictionary<Order, bool> resolved)
    {
        return order switch
        {
            HoldOrder                       => OrderOutcome.Success,
            MoveOrder                       => resolved.GetValueOrDefault(order) ? OrderOutcome.Success : OrderOutcome.Failure,
            SupportHoldOrder or SupportMoveOrder
                                            => resolved.GetValueOrDefault(order) ? OrderOutcome.Success : OrderOutcome.Failure,
            ConvoyOrder                     => resolved.GetValueOrDefault(order) ? OrderOutcome.Success : OrderOutcome.Failure,
            _                               => OrderOutcome.Success
        };
    }
}
