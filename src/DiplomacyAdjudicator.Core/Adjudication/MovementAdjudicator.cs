using woliver13.DiplomacyAdjudicator.Core.Domain;
using woliver13.DiplomacyAdjudicator.Core.Map;
using static woliver13.DiplomacyAdjudicator.Core.Domain.ProvinceCode;

namespace woliver13.DiplomacyAdjudicator.Core.Adjudication;

/// <summary>
/// Public entry point for movement adjudication.
/// Normalises orders (bicoastal destination resolution, fill-in holds),
/// delegates to MovementResolver, then maps boolean results to OrderOutcome
/// and identifies dislodged units.
/// </summary>
public sealed class MovementAdjudicator : IMovementAdjudicator
{
    public MovementAdjudicationResult Adjudicate(MovementAdjudicationRequest request)
    {
        var map    = request.Map;
        var orders = NormalizeOrders(map, request.Units, request.Orders);

        var resolver = new MovementResolver(map, request.Units, orders);
        var resolved = resolver.ResolveAll();

        // Units indexed by base province code for dislodged-unit lookup
        var unitsByBase = request.Units.ToDictionary(
            u => Normalise(u.Province.Code),
            StringComparer.Ordinal);

        // Orders indexed by unit base province for moved-out check
        var ordersByBase = orders.ToDictionary(
            o => Normalise(o.Unit.Province.Code),
            StringComparer.Ordinal);

        // Provinces occupied after movement (remove movers' origins, add destinations)
        var occupiedAfterMovement = new HashSet<string>(
            request.Units.Select(u => Normalise(u.Province.Code)),
            StringComparer.Ordinal);

        foreach (var (order, success) in resolved)
        {
            if (order is MoveOrder m && success)
            {
                occupiedAfterMovement.Remove(Normalise(m.Unit.Province.Code));
                occupiedAfterMovement.Add(Normalise(m.Destination.Code));
            }
        }

        // Standoff provinces: 2+ failed move orders targeted the same destination
        var standoffProvinces = resolved
            .Where(kvp => kvp.Key is MoveOrder && !kvp.Value)
            .GroupBy(kvp => Normalise(((MoveOrder)kvp.Key).Destination.Code),
                     StringComparer.Ordinal)
            .Where(g => g.Count() >= 2)
            .Select(g => g.Key)
            .ToHashSet(StringComparer.Ordinal);

        // First pass: compute base outcomes
        var resultMap = new Dictionary<string, OrderResult>(StringComparer.Ordinal);
        foreach (var order in orders)
        {
            var outcome = ComputeOutcome(order, resolved);
            var baseCode = Normalise(order.Unit.Province.Code);
            resultMap[baseCode] = new OrderResult(order, outcome);
        }

        // Second pass: mark dislodged units and compute retreat options
        var dislodgedUnits = new List<DislodgedUnit>();

        foreach (var (order, success) in resolved)
        {
            if (order is not MoveOrder move || !success) continue;

            var destBase = Normalise(move.Destination.Code);
            if (!unitsByBase.TryGetValue(destBase, out var displaced)) continue;

            // Check if the displaced unit successfully moved out
            bool movedOut = ordersByBase.TryGetValue(destBase, out var displacedOrder)
                && displacedOrder is MoveOrder displacedMove
                && resolved.GetValueOrDefault(displacedMove);

            if (movedOut) continue;

            // Override outcome to Dislodged for the displaced unit's order
            if (resultMap.TryGetValue(destBase, out var existing))
                resultMap[destBase] = existing with { Outcome = OrderOutcome.Dislodged };

            var attackedFrom = move.Unit.Province;
            var retreatOptions = ComputeRetreatOptions(
                map, displaced, attackedFrom, occupiedAfterMovement, standoffProvinces);

            dislodgedUnits.Add(new DislodgedUnit(displaced, attackedFrom, retreatOptions));
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

    private static List<Order> NormalizeOrders(MapGraph map, IReadOnlyList<Unit> units, IReadOnlyList<Order> providedOrders)
    {
        var normalized = new List<Order>();
        var orderedProvinceBases = new HashSet<string>(StringComparer.Ordinal);

        foreach (var order in providedOrders)
        {
            orderedProvinceBases.Add(Normalise(order.Unit.Province.Code));

            if (order is MoveOrder move)
            {
                // Resolve bicoastal destinations (auto-detect coast for fleets, strip coast for armies)
                var resolved = map.ResolveDestination(move.Unit.Province, move.Destination, move.Unit.Type);
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
            var baseCode = Normalise(unit.Province.Code);
            if (!orderedProvinceBases.Contains(baseCode))
                normalized.Add(new HoldOrder(unit));
        }

        return normalized;
    }

    // -------------------------------------------------------------------------
    // Retreat option computation
    // -------------------------------------------------------------------------

    private static List<Province> ComputeRetreatOptions(
        MapGraph map,
        Unit unit,
        Province attackedFrom,
        HashSet<string> occupiedAfterMovement,
        HashSet<string> standoffProvinces)
    {
        var attackedFromBase = Normalise(attackedFrom.Code);

        return map.GetNeighbors(unit.Province, unit.Type)
            .Where(p =>
            {
                var baseCode = Normalise(p.Code);
                return !occupiedAfterMovement.Contains(baseCode)
                    && baseCode != attackedFromBase
                    && !standoffProvinces.Contains(baseCode);
            })
            .ToList();
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
