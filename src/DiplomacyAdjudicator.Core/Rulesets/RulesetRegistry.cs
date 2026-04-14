using woliver13.DiplomacyAdjudicator.Core.Map;

namespace woliver13.DiplomacyAdjudicator.Core.Rulesets;

/// <summary>
/// Default registry: loads each supported ruleset's map once at startup.
/// </summary>
public sealed class RulesetRegistry : IRulesetRegistry
{
    private readonly IReadOnlyDictionary<string, MapGraph> _maps;

    public RulesetRegistry()
    {
        _maps = new Dictionary<string, MapGraph>(StringComparer.OrdinalIgnoreCase)
        {
            ["standard_2000"] = MapGraph.LoadStandard(),
            ["standard_1971"] = MapGraph.Load1971(),
        };
    }

    public IReadOnlyList<string> SupportedRulesets => [.. _maps.Keys];
    public bool IsKnown(string ruleset) => _maps.ContainsKey(ruleset);
    public MapGraph GetMap(string ruleset) => _maps[ruleset];
}
