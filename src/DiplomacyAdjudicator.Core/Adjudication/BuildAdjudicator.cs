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

        // Auto-disband for powers that still have required disbands outstanding.
        // Civil-disorder rule: remove the unit(s) farthest from any home supply centre.
        // Tie-breakers: fleet before army, then alphabetical by province code.
        foreach (var (power, remaining) in remainingDisbands)
        {
            if (remaining <= 0) continue;

            var homeProvinces = map.GetHomeProvinces(power);

            var candidates = resultingUnits
                .Where(u => u.Power == power)
                .OrderByDescending(u => CivilDisorderDistance(map, u, homeProvinces))
                .ThenBy(u => u.Type == UnitType.Fleet ? 0 : 1)   // fleets removed before armies
                .ThenBy(u => Normalise(u.Province.Code), StringComparer.Ordinal)
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

    // -------------------------------------------------------------------------
    // Civil-disorder distance helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the minimum number of province-steps from <paramref name="unit"/>'s
    /// province to the nearest home supply centre in <paramref name="homeProvinces"/>.
    ///
    /// Army distance: BFS through ALL provinces (including sea areas), so armies may
    /// "traverse" sea areas for counting purposes (each area = 1 step).
    ///
    /// Fleet distance: BFS through fleet-accessible provinces only (fleets cannot
    /// cross land).  Bicoastal home SCs are matched by base code, so both coast
    /// variants count as reaching the home SC.
    ///
    /// Returns <see cref="int.MaxValue"/> when the home SC is unreachable
    /// (e.g. an inland home SC for a fleet).
    /// </summary>
    private static int CivilDisorderDistance(
        MapGraph map,
        Unit unit,
        IReadOnlyList<Province> homeProvinces)
    {
        var homeBaseCodes = homeProvinces
            .Select(p => MapGraph.BaseCode(p.Code))
            .ToHashSet(StringComparer.Ordinal);

        return unit.Type == UnitType.Army
            ? ArmyDistance(map, unit.Province, homeBaseCodes)
            : FleetDistance(map, unit.Province, homeBaseCodes);
    }

    /// <summary>
    /// BFS using army+fleet adjacency union (armies may traverse sea areas).
    /// Visited set uses normalised base codes to avoid revisiting bicoastal variants.
    /// </summary>
    private static int ArmyDistance(
        MapGraph map,
        Province start,
        HashSet<string> homeBaseCodes)
    {
        var startBase = MapGraph.BaseCode(start.Code);
        if (homeBaseCodes.Contains(startBase)) return 0;

        var visited = new HashSet<string>(StringComparer.Ordinal) { startBase };
        var queue = new Queue<(string code, int dist)>();
        queue.Enqueue((startBase, 0));

        while (queue.Count > 0)
        {
            var (code, dist) = queue.Dequeue();
            foreach (var neighbor in map.GetDistanceNeighbors(new Province(code), UnitType.Army))
            {
                var nb = MapGraph.BaseCode(neighbor.Code);
                if (!visited.Add(nb)) continue;
                if (homeBaseCodes.Contains(nb)) return dist + 1;
                queue.Enqueue((nb, dist + 1));
            }
        }

        return int.MaxValue;
    }

    /// <summary>
    /// BFS using fleet adjacencies only (fleets cannot cross land).
    /// Visited set uses full province codes (including coast variants) so that
    /// "stp_nc" and "stp_sc" are explored independently — both resolve to the
    /// same home-SC base code when checked.
    /// </summary>
    private static int FleetDistance(
        MapGraph map,
        Province start,
        HashSet<string> homeBaseCodes)
    {
        var startBase = MapGraph.BaseCode(start.Code);
        if (homeBaseCodes.Contains(startBase)) return 0;

        var visited = new HashSet<string>(StringComparer.Ordinal) { start.Code };
        var queue = new Queue<(Province province, int dist)>();
        queue.Enqueue((start, 0));

        while (queue.Count > 0)
        {
            var (province, dist) = queue.Dequeue();
            foreach (var neighbor in map.GetDistanceNeighbors(province, UnitType.Fleet))
            {
                if (!visited.Add(neighbor.Code)) continue;
                var nb = MapGraph.BaseCode(neighbor.Code);
                if (homeBaseCodes.Contains(nb)) return dist + 1;
                queue.Enqueue((neighbor, dist + 1));
            }
        }

        return int.MaxValue;
    }
}
