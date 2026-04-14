using woliver13.DiplomacyAdjudicator.Core.Map;

namespace woliver13.DiplomacyAdjudicator.Core.Rulesets;

/// <summary>
/// Resolves a ruleset identifier to the corresponding MapGraph.
/// Ruleset identifiers are case-insensitive strings such as "standard_2000".
/// </summary>
public interface IRulesetRegistry
{
    /// <summary>All ruleset identifiers known to this registry.</summary>
    IReadOnlyList<string> SupportedRulesets { get; }

    bool IsKnown(string ruleset);

    /// <summary>Returns the map for the given ruleset. Throws if unknown.</summary>
    MapGraph GetMap(string ruleset);
}
