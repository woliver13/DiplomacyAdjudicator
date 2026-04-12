using DiplomacyAdjudicator.Core.Map;

namespace DiplomacyAdjudicator.Core.Parsing;

/// <summary>
/// Default factory — creates a standard <see cref="OrderParser"/> for the given map.
/// </summary>
public sealed class OrderParserFactory : IOrderParserFactory
{
    public IOrderParser Create(MapGraph map) => new OrderParser(map);
}
