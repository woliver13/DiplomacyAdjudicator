namespace DiplomacyAdjudicator.Core.Domain;

/// <summary>
/// Centralised province-code normalisation.
/// </summary>
public static class ProvinceCode
{
    /// <summary>
    /// Returns the canonical base code for a province string:
    /// lowercase and coast-suffix stripped.
    /// e.g. "SPA_NC" → "spa", "LON" → "lon".
    /// </summary>
    public static string Normalise(string raw)
    {
        var lower = raw.ToLowerInvariant();
        var idx   = lower.IndexOf('_');
        return idx >= 0 ? lower[..idx] : lower;
    }
}
