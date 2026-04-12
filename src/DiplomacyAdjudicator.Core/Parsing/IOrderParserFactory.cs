using DiplomacyAdjudicator.Core.Map;

namespace DiplomacyAdjudicator.Core.Parsing;

/// <summary>
/// Creates <see cref="IOrderParser"/> instances bound to a specific map.
/// Register this in DI so controllers never depend on <see cref="OrderParser"/> directly.
/// </summary>
public interface IOrderParserFactory
{
    IOrderParser Create(MapGraph map);
}
