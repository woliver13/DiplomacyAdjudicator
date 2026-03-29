namespace DiplomacyAdjudicator.Core.Map;

/// <summary>
/// Deserialization model for standard_map.json.
/// </summary>
internal sealed class MapData
{
    public List<string> SupplyCenters { get; init; } = [];
    public Dictionary<string, List<string>> HomeCenters { get; init; } = [];
    public Dictionary<string, ProvinceData> Provinces { get; init; } = [];
}
