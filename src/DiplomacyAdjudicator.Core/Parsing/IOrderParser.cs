using woliver13.DiplomacyAdjudicator.Core.Domain;

namespace woliver13.DiplomacyAdjudicator.Core.Parsing;

/// <summary>
/// Parses a single order string into a typed <see cref="Order"/> for the given unit.
/// </summary>
public interface IOrderParser
{
    Order Parse(Unit unit, string orderText);
}
