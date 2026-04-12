using DiplomacyAdjudicator.Core.Domain;
using DiplomacyAdjudicator.Core.Map;

namespace DiplomacyAdjudicator.Core.Parsing;

/// <summary>
/// Parses order strings into typed Order objects.
///
/// Accepted formats:
///   hold | h
///   move &lt;province&gt; | - &lt;province&gt;
///   support [army|fleet] &lt;province&gt; [move|"-" &lt;province&gt;]
///   convoy [army] &lt;province&gt; move|"-" &lt;province&gt;
///   retreat &lt;province&gt;
///   disband
///   build [army|fleet] &lt;province&gt;
///   waive
///
/// Province codes are normalised to lowercase with "/" replaced by "_".
/// Unknown or malformed orders fall back to HoldOrder.
/// </summary>
public class OrderParser : IOrderParser
{
    private readonly MapGraph _map;

    public OrderParser(MapGraph map) => _map = map;

    public Order Parse(Unit unit, string orderText)
    {
        var tokens = Tokenize(orderText);
        if (tokens.Length == 0) return new HoldOrder(unit);

        return tokens[0] switch
        {
            "hold" or "h"                      => new HoldOrder(unit),
            "move" or "m" or "-" or "attacks"  => ParseMove(unit, tokens),
            "support" or "s"                   => ParseSupport(unit, tokens),
            "convoy" or "c"                    => ParseConvoy(unit, tokens),
            "retreat" or "r"                   => ParseRetreat(unit, tokens),
            "disband"                          => new DisbandOrder(unit),
            "build"                            => ParseBuild(unit, tokens),
            "waive"                            => new WaiveOrder(unit.Power),
            _                                  => new HoldOrder(unit)
        };
    }

    // -------------------------------------------------------------------------
    // Movement phase
    // -------------------------------------------------------------------------

    private static Order ParseMove(Unit unit, string[] tokens)
    {
        var dest = ExtractProvince(tokens, 1);
        if (dest is null) return new HoldOrder(unit);
        return new MoveOrder(unit, dest);
    }

    private static Order ParseSupport(Unit unit, string[] tokens)
    {
        int idx = 1;
        SkipUnitType(tokens, ref idx);
        if (idx >= tokens.Length) return new HoldOrder(unit);

        var supportedProvince = ParseProvince(tokens[idx++]);

        // Check for "move" / "-" keyword
        if (idx < tokens.Length && IsMoveKeyword(tokens[idx]))
        {
            idx++; // consume "move" or "-"
            SkipUnitType(tokens, ref idx);
            var dest = ExtractProvince(tokens, idx);
            if (dest is null) return new HoldOrder(unit);
            return new SupportMoveOrder(unit, supportedProvince, dest);
        }

        return new SupportHoldOrder(unit, supportedProvince);
    }

    private static Order ParseConvoy(Unit unit, string[] tokens)
    {
        int idx = 1;
        SkipUnitType(tokens, ref idx);
        if (idx >= tokens.Length) return new HoldOrder(unit);

        var armyOrigin = ParseProvince(tokens[idx++]);

        if (idx < tokens.Length && IsMoveKeyword(tokens[idx])) idx++;
        SkipUnitType(tokens, ref idx);

        var dest = ExtractProvince(tokens, idx);
        if (dest is null) return new HoldOrder(unit);
        return new ConvoyOrder(unit, armyOrigin, dest);
    }

    // -------------------------------------------------------------------------
    // Retreat / build phases
    // -------------------------------------------------------------------------

    private static Order ParseRetreat(Unit unit, string[] tokens)
    {
        var dest = ExtractProvince(tokens, 1);
        if (dest is null) return new DisbandOrder(unit);
        return new RetreatOrder(unit, dest);
    }

    private static Order ParseBuild(Unit unit, string[] tokens)
    {
        // "build army lon" or "build fleet bre"
        // The unit type in the order may differ from the unit passed in —
        // callers should pass the unit that will be built.
        return new BuildOrder(unit);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static string[] Tokenize(string text)
        => text.Trim()
               .ToLowerInvariant()
               .Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);

    private static Province ParseProvince(string token)
        => new(NormaliseCode(token));

    private static string NormaliseCode(string token)
        => token.ToLowerInvariant().Replace('/', '_');

    private static Province? ExtractProvince(string[] tokens, int index)
    {
        if (index >= tokens.Length) return null;
        var token = tokens[index];
        if (IsUnitType(token))
        {
            index++;
            if (index >= tokens.Length) return null;
            token = tokens[index];
        }
        return ParseProvince(token);
    }

    private static void SkipUnitType(string[] tokens, ref int idx)
    {
        if (idx < tokens.Length && IsUnitType(tokens[idx])) idx++;
    }

    private static bool IsUnitType(string token)
        => token is "army" or "fleet" or "a" or "f";

    private static bool IsMoveKeyword(string token)
        => token is "move" or "m" or "-" or "to";
}
