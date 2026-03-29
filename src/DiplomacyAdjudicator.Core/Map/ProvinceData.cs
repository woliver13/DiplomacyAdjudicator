namespace DiplomacyAdjudicator.Core.Map;

/// <summary>
/// Deserialization model for a province entry in standard_map.json.
/// </summary>
internal sealed class ProvinceData
{
    public string Name { get; init; } = "";
    public string Type { get; init; } = "";
    public List<string>? ArmyAdjacencies { get; init; }
    public List<string>? FleetAdjacencies { get; init; }
}
