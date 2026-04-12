using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using DiplomacyAdjudicator.Core.Domain;

namespace DiplomacyAdjudicator.Core.Map;

/// <summary>
/// In-memory adjacency graph for the standard Diplomacy map.
/// Loaded once from the embedded standard_map.json resource.
/// </summary>
public sealed class MapGraph
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly Dictionary<string, ProvinceData> _provinces;
    private readonly HashSet<string> _supplyCenters;
    private readonly Dictionary<string, string> _homeCenterByProvince; // province code → power name

    private MapGraph(MapData data)
    {
        _provinces = data.Provinces;
        _supplyCenters = new HashSet<string>(data.SupplyCenters, StringComparer.Ordinal);
        _homeCenterByProvince = [];

        foreach (var (power, centers) in data.HomeCenters)
            foreach (var center in centers)
                _homeCenterByProvince[center] = power;
    }

    /// <summary>
    /// Loads the standard map from the embedded resource.
    /// </summary>
    public static MapGraph LoadStandard()
    {
        var assembly = Assembly.GetExecutingAssembly();
        const string resourceName = "DiplomacyAdjudicator.Core.Data.standard_map.json";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{resourceName}' not found. " +
                "Ensure standard_map.json is marked as EmbeddedResource in the .csproj.");

        var data = JsonSerializer.Deserialize<MapData>(stream, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize standard_map.json.");

        return new MapGraph(data);
    }

    /// <summary>
    /// Loads the 1971 edition map. Identical to the standard map except that
    /// armies in TUN and NAP are directly adjacent (no fleet convoy required),
    /// reflecting the adjacency printed in older editions.
    /// </summary>
    public static MapGraph Load1971()
    {
        var assembly = Assembly.GetExecutingAssembly();
        const string resourceName = "DiplomacyAdjudicator.Core.Data.standard_map.json";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{resourceName}' not found.");

        var data = JsonSerializer.Deserialize<MapData>(stream, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize standard_map.json.");

        // 1971 patch: TUN ↔ NAP are mutually army-adjacent
        data.Provinces["tun"].ArmyAdjacencies?.Add("nap");
        data.Provinces["nap"].ArmyAdjacencies?.Add("tun");

        return new MapGraph(data);
    }

    /// <summary>
    /// Returns true if the province code is known (including coast variants).
    /// </summary>
    public bool IsValidProvince(string code)
        => _provinces.ContainsKey(code.ToLowerInvariant());

    /// <summary>
    /// Returns true if a unit of the given type can move directly from <paramref name="from"/>
    /// to <paramref name="to"/> in a single turn.
    /// </summary>
    public bool IsAdjacent(Province from, Province to, UnitType unitType)
    {
        if (!_provinces.TryGetValue(from.Code, out var fromData))
            return false;

        var adjacencies = unitType == UnitType.Army
            ? fromData.ArmyAdjacencies
            : fromData.FleetAdjacencies;

        if (adjacencies is null)
            return false;

        return adjacencies.Contains(to.Code);
    }

    /// <summary>
    /// Returns true if the province is a supply center.
    /// Uses the base province code for bicoastal variants (e.g. bul_ec → bul).
    /// </summary>
    public bool IsSupplyCenter(Province province)
    {
        var code = NormalizeToBase(province.Code);
        return _supplyCenters.Contains(code);
    }

    /// <summary>
    /// Returns the power that owns this province as a home center, or null.
    /// Uses the base province code for bicoastal variants.
    /// </summary>
    public Power? GetHomeCenter(Province province)
    {
        var code = NormalizeToBase(province.Code);
        return _homeCenterByProvince.TryGetValue(code, out var power)
            ? new Power(power)
            : null;
    }

    /// <summary>
    /// Returns true if the province is a landlocked land province (no fleet access).
    /// </summary>
    public bool IsInland(Province province)
    {
        if (!_provinces.TryGetValue(province.Code, out var data))
            return false;
        return data.Type == "land";
    }

    /// <summary>
    /// Returns true if the province is a water (sea) province.
    /// </summary>
    public bool IsSea(Province province)
    {
        if (!_provinces.TryGetValue(province.Code, out var data))
            return false;
        return data.Type == "water";
    }

    /// <summary>
    /// Returns true if the province is impassable (Switzerland).
    /// </summary>
    public bool IsShut(Province province)
    {
        if (!_provinces.TryGetValue(province.Code, out var data))
            return false;
        return data.Type == "shut";
    }

    /// <summary>
    /// For a fleet moving from <paramref name="origin"/> to a bicoastal parent province
    /// (e.g. "spa"), resolves the destination to the unique reachable coast variant,
    /// or returns null if the move is ambiguous or impossible.
    ///
    /// If <paramref name="destination"/> already specifies a coast variant, returns it
    /// unchanged (or null if that exact variant is not adjacent).
    ///
    /// For armies, coast variants are ignored: the base province code is returned.
    /// </summary>
    public Province? ResolveDestination(Province origin, Province destination, UnitType unitType)
    {
        if (unitType == UnitType.Army)
        {
            // Armies ignore coast notation — normalise to base province
            var baseCode = NormalizeToBase(destination.Code);
            var baseProv = new Province(baseCode);
            return IsAdjacent(origin, baseProv, UnitType.Army) ? baseProv : null;
        }

        // Fleet: if destination is already a coast variant, just check adjacency
        if (destination.Code.Contains('_'))
        {
            return IsAdjacent(origin, destination, UnitType.Fleet) ? destination : null;
        }

        // Fleet moving to a base province code — check if it's bicoastal
        var coasts = CoastVariants(destination.Code);
        if (coasts.Count == 0)
        {
            // Not bicoastal — normal adjacency check
            return IsAdjacent(origin, destination, UnitType.Fleet) ? destination : null;
        }

        // Bicoastal: find reachable coasts
        var reachable = coasts
            .Where(c => IsAdjacent(origin, new Province(c), UnitType.Fleet))
            .ToList();

        return reachable.Count == 1 ? new Province(reachable[0]) : null;
    }

    /// <summary>
    /// Returns coast variant codes for a bicoastal parent province,
    /// e.g. "spa" → ["spa_nc", "spa_sc"].
    /// Returns empty list for non-bicoastal provinces.
    /// </summary>
    public IReadOnlyList<string> CoastVariants(string baseCode)
    {
        var results = new List<string>();
        // Bicoastal suffixes used in the standard map
        foreach (var suffix in new[] { "_nc", "_sc", "_ec" })
        {
            var variant = baseCode + suffix;
            if (_provinces.ContainsKey(variant))
                results.Add(variant);
        }
        return results;
    }

    /// <summary>
    /// Returns the base province code for a coast variant, or the code itself.
    /// "spa_nc" → "spa", "lon" → "lon"
    /// </summary>
    public static string BaseCode(string code)
    {
        var idx = code.IndexOf('_');
        return idx >= 0 ? code[..idx] : code;
    }

    /// <summary>
    /// Returns all provinces directly reachable from <paramref name="province"/>
    /// by a unit of <paramref name="unitType"/> in a single move.
    /// For fleet units at bicoastal coast variants the specific coast adjacencies are used.
    /// </summary>
    public IReadOnlyList<Province> GetNeighbors(Province province, UnitType unitType)
    {
        if (!_provinces.TryGetValue(province.Code, out var data))
            return [];

        var adjacencies = unitType == UnitType.Army
            ? data.ArmyAdjacencies
            : data.FleetAdjacencies;

        if (adjacencies is null)
            return [];

        return adjacencies.Select(c => new Province(c)).ToList();
    }

    /// <summary>
    /// Returns all home supply-centre base provinces for the given power.
    /// </summary>
    public IReadOnlyList<Province> GetHomeProvinces(Power power)
        => _homeCenterByProvince
            .Where(kvp => string.Equals(kvp.Value, power.Name, StringComparison.OrdinalIgnoreCase))
            .Select(kvp => new Province(kvp.Key))
            .ToList();

    /// <summary>
    /// Returns provinces reachable in one step from <paramref name="province"/> for
    /// civil-disorder distance BFS purposes.
    ///
    /// For armies: union of army and fleet adjacencies so that armies may "traverse"
    /// sea areas (each sea area counts as one step).
    /// For fleets: fleet adjacencies only (fleets cannot cross land).
    ///
    /// Returned codes may include coast variants (e.g. "stp_nc"); callers that want
    /// only base codes should normalise via <see cref="BaseCode"/>.
    /// </summary>
    public IReadOnlyList<Province> GetDistanceNeighbors(Province province, UnitType unitType)
    {
        if (!_provinces.TryGetValue(province.Code, out var data))
            return [];

        if (unitType == UnitType.Fleet)
        {
            var fleetAdj = data.FleetAdjacencies;
            return fleetAdj is null ? [] : fleetAdj.Select(c => new Province(c)).ToList();
        }

        // Army: union of army and fleet adjacencies
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<Province>();

        void AddAll(List<string>? list)
        {
            if (list is null) return;
            foreach (var c in list)
                if (seen.Add(c)) result.Add(new Province(c));
        }

        AddAll(data.ArmyAdjacencies);
        AddAll(data.FleetAdjacencies);
        return result;
    }

    // Strip coast suffix to get base province code: "spa_nc" → "spa", "bul_ec" → "bul"
    private static string NormalizeToBase(string code) => BaseCode(code);
}
