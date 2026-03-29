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
        _supplyCenters = new HashSet<string>(data.SupplyCenters, StringComparer.OrdinalIgnoreCase);
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

        return adjacencies.Contains(to.Code, StringComparer.OrdinalIgnoreCase);
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

    // Strip coast suffix to get base province code: "spa_nc" → "spa", "bul_ec" → "bul"
    private static string NormalizeToBase(string code)
    {
        var idx = code.IndexOf('_');
        return idx >= 0 ? code[..idx] : code;
    }
}
