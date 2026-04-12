using DiplomacyAdjudicator.Core.Domain;

namespace DiplomacyAdjudicator.Core.Parsing;

/// <summary>
/// Parses a single order string into a typed <see cref="Order"/> for the given unit.
/// </summary>
public interface IOrderParser
{
    Order Parse(Unit unit, string orderText);
}
