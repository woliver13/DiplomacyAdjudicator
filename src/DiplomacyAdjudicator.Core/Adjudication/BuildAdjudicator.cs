using DiplomacyAdjudicator.Core.Domain;
using DiplomacyAdjudicator.Core.Map;
using static DiplomacyAdjudicator.Core.Domain.ProvinceCode;

namespace DiplomacyAdjudicator.Core.Adjudication;

/// <summary>
/// Adjudicates the build/disband phase.
///
/// For each power:
///   adjustment = SC count − unit count
///   &gt; 0  →  may build up to that many units in unoccupied home SCs
///   &lt; 0  →  must disband exactly |adjustment| units
///   = 0  →  no action required
///
/// Build validation:
///   - Province must be this power's home supply center
///   - Province must be unoccupied (not in the starting unit list)
///   - Unit type must be valid for province type
///     (Army: not sea, not shut; Fleet: not inland, not shut)
///   - Must not exceed remaining build allowance
///
/// Disband validation:
///   - Unit must belong to the power and exist on the board
///   - Must not exceed required disband count
///
/// Auto-disband: if a power provides fewer disband orders than required, the
/// shortfall is filled by auto-disbanding remaining units in alphabetical order
/// of province code.
///
/// Waive: each waive consumes one build slot (success, no unit placed).
/// </summary>
public sealed class BuildAdjudicator : IBuildAdjudicator
{
    public BuildAdjudicationResult Adjudicate(BuildAdjudicationRequest request)
    {
        var map = request.Map;
        var orderResults = new List<OrderResult>();
        var resultingUnits = new List<Unit>(request.Units);

        // Track occupied province base codes (for build validity)
        var occupiedBases = request.Units
            .Select(u => Normalise(u.Province.Code))
            .ToHashSet(StringComparer.Ordinal);

        // Compute adjustments per power
        var scCounts = request.SupplyCenters.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Count);

        var unitCountsByPower = request.Units
            .GroupBy(u => u.Power)
            .ToDictionary(g => g.Key, g => g.Count());

        // All powers that appear in either SC or unit lists
        var allPowers = scCounts.Keys
            .Union(unitCountsByPower.Keys)
            .ToHashSet();

        // Remaining allowance trackers per power
        var remainingBuilds = new Dictionary<Power, int>();
        var remainingDisbands = new Dictionary<Power, int>();

        foreach (var power in allPowers)
        {
            scCounts.TryGetValue(power, out int scs);
            unitCountsByPower.TryGetValue(power, out int units);
            int adj = scs - units;
            if (adj > 0) remainingBuilds[power] = adj;
            else if (adj < 0) remainingDisbands[power] = -adj;
        }

        // Process submitted orders
        foreach (var order in request.BuildOrders)
        {
            switch (order)
            {
                case BuildOrder build:
                    orderResults.Add(ProcessBuild(map, build, remainingBuilds, occupiedBases, resultingUnits));
                    break;

                case DisbandOrder disband:
                    orderResults.Add(ProcessDisband(disband, remainingDisbands, resultingUnits));
                    break;

                case WaiveOrder waive:
                    orderResults.Add(ProcessWaive(waive, remainingBuilds));
                    break;

                default:
                    orderResults.Add(new OrderResult(order, OrderOutcome.Void,
                        "Order type not valid in build phase."));
                    break;
            }
        }

        // Auto-disband for powers that still have required disbands outstanding
        foreach (var (power, remaining) in remainingDisbands)
        {
            if (remaining <= 0) continue;

            var candidates = resultingUnits
                .Where(u => u.Power == power)
                .OrderBy(u => u.Province.Code, StringComparer.Ordinal)
                .Take(remaining)
                .ToList();

            foreach (var unit in candidates)
            {
                var autoDisbandOrder = new DisbandOrder(unit);
                orderResults.Add(new OrderResult(autoDisbandOrder, OrderOutcome.Success,
                    "Auto-disbanded due to insufficient disband orders."));
                resultingUnits.Remove(unit);
            }

            remainingDisbands[power] = 0;
        }

        return new BuildAdjudicationResult(orderResults, resultingUnits);
    }

    // -------------------------------------------------------------------------

    private static OrderResult ProcessBuild(
        MapGraph map,
        BuildOrder order,
        Dictionary<Power, int> remainingBuilds,
        HashSet<string> occupiedBases,
        List<Unit> resultingUnits)
    {
        var power = order.Unit.Power;
        var province = order.Unit.Province;
        var baseCode = Normalise(province.Code);

        // Must have build allowance
        if (!remainingBuilds.TryGetValue(power, out int allowance) || allowance <= 0)
            return new OrderResult(order, OrderOutcome.Void, "No build allowance remaining.");

        // Must be this power's home SC
        var homeCenter = map.GetHomeCenter(province);
        if (homeCenter is null || homeCenter != power)
            return new OrderResult(order, OrderOutcome.Void,
                $"Province '{baseCode}' is not a home supply center for {power.Name}.");

        // Must be unoccupied
        if (occupiedBases.Contains(baseCode))
            return new OrderResult(order, OrderOutcome.Void,
                $"Province '{baseCode}' is occupied.");

        // Unit type must be valid for province type
        if (order.Unit.Type == UnitType.Fleet && map.IsInland(province))
            return new OrderResult(order, OrderOutcome.Void,
                $"Fleet cannot be built in inland province '{baseCode}'.");

        if (order.Unit.Type == UnitType.Army && map.IsSea(province))
            return new OrderResult(order, OrderOutcome.Void,
                $"Army cannot be built in sea province '{baseCode}'.");

        if (map.IsShut(province))
            return new OrderResult(order, OrderOutcome.Void,
                $"Province '{baseCode}' is impassable.");

        // Valid build
        remainingBuilds[power] = allowance - 1;
        occupiedBases.Add(baseCode);
        resultingUnits.Add(order.Unit);
        return new OrderResult(order, OrderOutcome.Success);
    }

    private static OrderResult ProcessDisband(
        DisbandOrder order,
        Dictionary<Power, int> remainingDisbands,
        List<Unit> resultingUnits)
    {
        var power = order.Unit.Power;
        var baseCode = Normalise(order.Unit.Province.Code);

        // Unit must exist
        var unit = resultingUnits.FirstOrDefault(u =>
            u.Power == power &&
            Normalise(u.Province.Code) == baseCode);

        if (unit is null)
            return new OrderResult(order, OrderOutcome.Void,
                $"No unit belonging to {power.Name} at '{baseCode}'.");

        // Must have required disbands remaining
        if (!remainingDisbands.TryGetValue(power, out int required) || required <= 0)
            return new OrderResult(order, OrderOutcome.Void,
                "No disband required for this power.");

        remainingDisbands[power] = required - 1;
        resultingUnits.Remove(unit);
        return new OrderResult(order, OrderOutcome.Success);
    }

    private static OrderResult ProcessWaive(
        WaiveOrder order,
        Dictionary<Power, int> remainingBuilds)
    {
        var power = order.Unit.Power;

        if (!remainingBuilds.TryGetValue(power, out int allowance) || allowance <= 0)
            return new OrderResult(order, OrderOutcome.Void, "No build allowance to waive.");

        remainingBuilds[power] = allowance - 1;
        return new OrderResult(order, OrderOutcome.Success);
    }
}
