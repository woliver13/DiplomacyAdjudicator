using DiplomacyAdjudicator.Core.Domain;
using DiplomacyAdjudicator.Core.Map;
using static DiplomacyAdjudicator.Core.Domain.ProvinceCode;

namespace DiplomacyAdjudicator.Core.Adjudication;

/// <summary>
/// Implements Kruijswijk's movement adjudication algorithm.
///
/// Algorithm overview:
///   1. Resolve each order via mutual recursion; use a pessimistic false assumption
///      when a cycle is detected (tracking which orders hit the cycle).
///   2. After the first pass, fix circular movements: if a set of move orders form a
///      complete cycle of legal moves, mark them all as successful.
///
/// Key strength functions:
///   HoldStrength(p)  — how hard it is to displace the unit at p
///   AttackStrength(m) — how many units support this move (with national restriction)
///   DefendStrength(m) — attack strength counting all support (used in head-on battles)
///   PreventStrength(m) — used when two units compete for the same province
/// </summary>
internal sealed class MovementResolver
{
    private readonly MapGraph _map;
    private readonly IReadOnlyDictionary<Province, Unit> _units;
    private readonly IReadOnlyDictionary<Province, Order> _orders;

    // Resolution cache
    private readonly Dictionary<Order, bool> _resolved = new();
    // Orders currently being resolved (for cycle detection)
    private readonly HashSet<Order> _inProgress = new();
    // Orders that triggered cycle detection (candidates for circular movement fix)
    private readonly HashSet<Order> _cycleEntries = new();

    internal MovementResolver(
        MapGraph map,
        IEnumerable<Unit> units,
        IEnumerable<Order> orders)
    {
        _map = map;
        _units = units.ToDictionary(u => u.Province);
        _orders = orders.ToDictionary(o => o.Unit.Province);
    }

    // -------------------------------------------------------------------------
    // Public entry point
    // -------------------------------------------------------------------------

    internal IReadOnlyDictionary<Order, bool> ResolveAll()
    {
        foreach (var order in _orders.Values)
            Resolve(order);

        FixCircularMovements();

        return _resolved;
    }

    // -------------------------------------------------------------------------
    // Core resolve / adjudicate
    // -------------------------------------------------------------------------

    internal bool Resolve(Order order)
    {
        if (_resolved.TryGetValue(order, out var cached)) return cached;

        if (_inProgress.Contains(order))
        {
            _cycleEntries.Add(order);
            return false; // pessimistic: assume failure for cycles
        }

        _inProgress.Add(order);
        var result = Adjudicate(order);
        _inProgress.Remove(order);

        _resolved[order] = result;
        return result;
    }

    private bool Adjudicate(Order order) => order switch
    {
        HoldOrder      => true,
        MoveOrder m    => AdjudicateMove(m),
        SupportHoldOrder s => AdjudicateSupportHold(s),
        SupportMoveOrder s => AdjudicateSupportMove(s),
        ConvoyOrder c  => AdjudicateConvoy(c),
        _              => true
    };

    // -------------------------------------------------------------------------
    // Move adjudication
    // -------------------------------------------------------------------------

    private bool AdjudicateMove(MoveOrder move)
    {
        // Cannot move to own province
        if (move.Unit.Province == move.Destination) return false;

        // Cannot move into Switzerland
        if (_map.IsShut(move.Destination)) return false;

        bool direct  = _map.IsAdjacent(move.Unit.Province, move.Destination, move.Unit.Type);
        bool convoy  = !direct && move.Unit.Type == UnitType.Army && HasConvoyPath(move);

        if (!direct && !convoy) return false;

        // Head-on collision: only between two DIRECT moves going to each other's province
        if (direct)
        {
            var orderAtDest = GetOrderAt(move.Destination);
            if (orderAtDest is MoveOrder counter &&
                Normalise(counter.Destination.Code) == Normalise(move.Unit.Province.Code) &&
                Normalise(counter.Unit.Province.Code) == Normalise(move.Destination.Code) &&
                _map.IsAdjacent(counter.Unit.Province, counter.Destination, counter.Unit.Type))
            {
                if (DefendStrength(move) <= DefendStrength(counter))
                    return false;
            }
        }

        // Attack must exceed hold strength of destination
        if (AttackStrength(move) <= HoldStrength(move.Destination))
            return false;

        // No competing move may have prevent strength >= our attack strength
        int atk = AttackStrength(move);
        var destBase = Normalise(move.Destination.Code);
        foreach (var other in _orders.Values.OfType<MoveOrder>())
        {
            if (other == move) continue;
            if (Normalise(other.Destination.Code) != destBase) continue;
            if (PreventStrength(other) >= atk) return false;
        }

        return true;
    }

    // -------------------------------------------------------------------------
    // Strength functions
    // -------------------------------------------------------------------------

    private int HoldStrength(Province province)
    {
        if (!HasUnitAt(province)) return 0;

        var order = GetOrderAt(province);
        // If the unit is trying to move out and succeeds, it vacates
        if (order is MoveOrder move && Resolve(move)) return 0;

        // Hold strength = 1 + successful support-holds
        var baseCode = Normalise(province.Code);
        return 1 + _orders.Values
            .OfType<SupportHoldOrder>()
            .Count(s => Normalise(s.SupportedProvince.Code) == baseCode && Resolve(s));
    }

    private int AttackStrength(MoveOrder move)
    {
        bool direct = _map.IsAdjacent(move.Unit.Province, move.Destination, move.Unit.Type);
        bool convoy = !direct && move.Unit.Type == UnitType.Army && HasConvoyPath(move);
        if (!direct && !convoy) return 0;

        var destBase = Normalise(move.Destination.Code);
        var defender = _units.Values.FirstOrDefault(u =>
            Normalise(u.Province.Code) == destBase);

        int count = 1;
        foreach (var s in _orders.Values.OfType<SupportMoveOrder>())
        {
            if (s.SupportedOrigin != move.Unit.Province) continue;
            if (s.SupportedDestination != move.Destination) continue;
            if (!Resolve(s)) continue;
            if (defender is not null && s.Unit.Power == defender.Power) continue;
            count++;
        }
        return count;
    }

    private int DefendStrength(MoveOrder move)
    {
        bool direct = _map.IsAdjacent(move.Unit.Province, move.Destination, move.Unit.Type);
        bool convoy = !direct && move.Unit.Type == UnitType.Army && HasConvoyPath(move);
        if (!direct && !convoy) return 0;

        return 1 + _orders.Values
            .OfType<SupportMoveOrder>()
            .Count(s => s.SupportedOrigin == move.Unit.Province
                     && s.SupportedDestination == move.Destination
                     && Resolve(s));
    }

    private int PreventStrength(MoveOrder move)
    {
        // A move to own province is always illegal — it contributes no prevent strength.
        if (Normalise(move.Unit.Province.Code) == Normalise(move.Destination.Code))
            return 0;

        bool direct = _map.IsAdjacent(move.Unit.Province, move.Destination, move.Unit.Type);
        bool convoy = !direct && move.Unit.Type == UnitType.Army && HasConvoyPath(move);
        if (!direct && !convoy) return 0;

        // A direct move losing a head-on gets prevent strength 0
        if (direct)
        {
            var orderAtDest = GetOrderAt(move.Destination);
            if (orderAtDest is MoveOrder counter &&
                Normalise(counter.Destination.Code) == Normalise(move.Unit.Province.Code) &&
                Normalise(counter.Unit.Province.Code) == Normalise(move.Destination.Code) &&
                _map.IsAdjacent(counter.Unit.Province, counter.Destination, counter.Unit.Type))
            {
                if (DefendStrength(move) <= DefendStrength(counter))
                    return 0;
            }
        }

        return 1 + _orders.Values
            .OfType<SupportMoveOrder>()
            .Count(s => s.SupportedOrigin == move.Unit.Province
                     && s.SupportedDestination == move.Destination
                     && Resolve(s));
    }

    // -------------------------------------------------------------------------
    // Support adjudication
    // -------------------------------------------------------------------------

    private bool AdjudicateSupportHold(SupportHoldOrder support)
    {
        // Support is void if unit doesn't exist at supported province
        if (!HasUnitAt(support.SupportedProvince)) return false;

        // Support is void if the supported unit is itself trying to move out
        if (GetOrderAt(support.SupportedProvince) is MoveOrder) return false;

        // Support is void if the supporter cannot legally reach the supported province.
        // Coast-insensitive: if supporter can reach ANY coast of the province it counts.
        if (!CanReachProvince(support.Unit.Province, support.SupportedProvince, support.Unit.Type))
            return false;

        // Support is cut if any unit attacks the supporter's province (base-code match)
        // EXCEPT from the supported province itself (can't cut support directed at you)
        var supporterBase = Normalise(support.Unit.Province.Code);
        foreach (var attack in _orders.Values.OfType<MoveOrder>())
        {
            if (Normalise(attack.Destination.Code) != supporterBase) continue;
            if (Normalise(attack.Unit.Province.Code) ==
                Normalise(support.SupportedProvince.Code)) continue;
            return false; // cut
        }

        return true;
    }

    private bool AdjudicateSupportMove(SupportMoveOrder support)
    {
        // Support is void if the supporter cannot legally reach the supported destination.
        // Coast-insensitive: if supporter can reach ANY coast of the province it counts.
        if (!CanReachProvince(support.Unit.Province, support.SupportedDestination, support.Unit.Type))
            return false;

        // Support is void if there is no matching move order at the supported origin
        var orderAtOrigin = GetOrderAt(support.SupportedOrigin);
        if (orderAtOrigin is not MoveOrder targetMove ||
            Normalise(targetMove.Destination.Code) != Normalise(support.SupportedDestination.Code))
            return false;

        // Support is cut if any unit attacks the supporter's province (base-code match)
        // EXCEPT from the move destination (can't cut support directed at you)
        var supporterBase = Normalise(support.Unit.Province.Code);
        foreach (var attack in _orders.Values.OfType<MoveOrder>())
        {
            if (Normalise(attack.Destination.Code) != supporterBase) continue;
            if (Normalise(attack.Unit.Province.Code) ==
                Normalise(support.SupportedDestination.Code)) continue;
            return false; // cut
        }

        return true;
    }

    // -------------------------------------------------------------------------
    // Convoy adjudication
    // -------------------------------------------------------------------------

    private bool AdjudicateConvoy(ConvoyOrder convoy)
    {
        // A fleet must be at sea to convoy.
        if (!_map.IsSea(convoy.Unit.Province)) return false;

        // A convoy fails if the fleet is dislodged (any attacking move succeeds).
        var fleetBase = Normalise(convoy.Unit.Province.Code);
        foreach (var attack in _orders.Values.OfType<MoveOrder>())
        {
            if (Normalise(attack.Destination.Code) != fleetBase) continue;
            if (Resolve(attack)) return false;
        }
        return true;
    }

    // -------------------------------------------------------------------------
    // Convoy path (BFS through active convoy fleets)
    // -------------------------------------------------------------------------

    private bool HasConvoyPath(MoveOrder move)
    {
        if (move.Unit.Type != UnitType.Army) return false;

        var originBase = Normalise(move.Unit.Province.Code);
        var destBase   = Normalise(move.Destination.Code);

        // Active convoy fleets that match this army's origin→destination
        var activeConvoys = _orders.Values
            .OfType<ConvoyOrder>()
            .Where(c =>
                Normalise(c.ConvoyedOrigin.Code) == originBase &&
                Normalise(c.ConvoyedDestination.Code) == destBase &&
                Resolve(c))
            .ToList();

        if (activeConvoys.Count == 0) return false;

        // BFS: start from convoy fleets adjacent to the army's origin province
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var queue   = new Queue<string>();

        foreach (var convoy in activeConvoys)
        {
            if (!_map.IsAdjacent(convoy.Unit.Province, move.Unit.Province, UnitType.Fleet))
                continue;
            var code = Normalise(convoy.Unit.Province.Code);
            if (visited.Add(code)) queue.Enqueue(code);
        }

        while (queue.Count > 0)
        {
            var cur = queue.Dequeue();

            // If this fleet province is adjacent to the destination, a path exists
            if (_map.IsAdjacent(new Province(cur), move.Destination, UnitType.Fleet))
                return true;

            // Expand to adjacent active convoy fleets not yet visited
            foreach (var convoy in activeConvoys)
            {
                var code = Normalise(convoy.Unit.Province.Code);
                if (visited.Contains(code)) continue;
                if (_map.IsAdjacent(new Province(cur), convoy.Unit.Province, UnitType.Fleet))
                {
                    visited.Add(code);
                    queue.Enqueue(code);
                }
            }
        }

        return false;
    }

    // -------------------------------------------------------------------------
    // Circular movement fix
    // -------------------------------------------------------------------------

    private void FixCircularMovements()
    {
        // Orders in _cycleEntries had their resolution cut short by cycle detection.
        // If any of them is the start of a complete cycle of legal moves, the whole
        // cycle succeeds (standard circular movement ruling).
        bool changed = true;
        while (changed)
        {
            changed = false;
            foreach (var entry in _cycleEntries.OfType<MoveOrder>().ToList())
            {
                if (_resolved.GetValueOrDefault(entry)) continue; // already fixed
                if (!IsCircularMovement(entry)) continue;

                MarkCycleAsSuccess(entry);
                changed = true;
            }
        }
    }

    private bool IsCircularMovement(MoveOrder start)
    {
        var visited = new HashSet<string>(); // base province codes
        var current = start;

        while (true)
        {
            if (!IsLegalMove(current)) return false;

            var originBase = Normalise(current.Unit.Province.Code);
            if (visited.Contains(originBase))
                // Completed a cycle — valid only if we returned to the start
                return originBase == Normalise(start.Unit.Province.Code);

            visited.Add(originBase);

            // If any external move has already successfully resolved into this province,
            // the unit here is dislodged and the circular movement is disrupted.
            foreach (var other in _orders.Values.OfType<MoveOrder>())
            {
                if (other == current) continue;
                if (Normalise(other.Destination.Code) != originBase) continue;
                if (_resolved.GetValueOrDefault(other)) return false;
            }

            var nextOrder = GetOrderAt(current.Destination);
            if (nextOrder is not MoveOrder nextMove) return false;
            current = nextMove;
        }
    }

    private bool IsLegalMove(MoveOrder move)
        => move.Unit.Province != move.Destination
        && !_map.IsShut(move.Destination)
        && (_map.IsAdjacent(move.Unit.Province, move.Destination, move.Unit.Type)
            || (move.Unit.Type == UnitType.Army && HasConvoyPath(move)));

    private void MarkCycleAsSuccess(MoveOrder start)
    {
        var current = start;
        var visited = new HashSet<string>();

        while (true)
        {
            var originBase = Normalise(current.Unit.Province.Code);
            if (visited.Contains(originBase)) break;
            visited.Add(originBase);

            _resolved[current] = true;

            var nextOrder = GetOrderAt(current.Destination);
            if (nextOrder is not MoveOrder nextMove) break;
            current = nextMove;
        }
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the order for the unit whose base province matches <paramref name="province"/>.
    /// Handles coast variants: looking up "bul_ec" finds an order keyed at "bul_sc".
    /// </summary>
    private Order? GetOrderAt(Province province)
    {
        if (_orders.TryGetValue(province, out var exact)) return exact;
        var baseCode = Normalise(province.Code);
        foreach (var o in _orders.Values)
            if (Normalise(o.Unit.Province.Code) == baseCode) return o;
        return null;
    }

    /// <summary>
    /// Returns true if a unit occupies the base province of <paramref name="province"/>.
    /// </summary>
    private bool HasUnitAt(Province province)
    {
        if (_units.ContainsKey(province)) return true;
        var baseCode = Normalise(province.Code);
        foreach (var p in _units.Keys)
            if (Normalise(p.Code) == baseCode) return true;
        return false;
    }

    /// <summary>
    /// Returns true if <paramref name="from"/> can reach the base province of
    /// <paramref name="to"/> by any coast. This implements DATC 6.B.4: a fleet that
    /// can reach Spain/SC can support a move to Spain/NC.
    /// </summary>
    private bool CanReachProvince(Province from, Province to, UnitType unitType)
    {
        if (_map.IsAdjacent(from, to, unitType)) return true;
        var targetBase = Normalise(to.Code);
        return _map.GetNeighbors(from, unitType)
            .Any(n => Normalise(n.Code) == targetBase);
    }
}
