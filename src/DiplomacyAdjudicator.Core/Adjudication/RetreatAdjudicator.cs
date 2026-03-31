using DiplomacyAdjudicator.Core.Domain;
using DiplomacyAdjudicator.Core.Map;

namespace DiplomacyAdjudicator.Core.Adjudication;

/// <summary>
/// Adjudicates the retreat phase.
///
/// Rules:
///   - A dislodged unit with no retreat order is disbanded (Failure).
///   - A retreat to a province not in RetreatOptions is void (unit disbanded).
///   - Two or more units retreating to the same province all disband (Bounced).
///   - Otherwise the retreat succeeds and the unit moves to the new province.
/// </summary>
public sealed class RetreatAdjudicator : IRetreatAdjudicator
{
    public RetreatAdjudicationResult Adjudicate(RetreatAdjudicationRequest request)
    {
        // Index retreat orders by unit province (base code)
        var orderByBase = request.RetreatOrders.ToDictionary(
            r => MapGraph.BaseCode(r.Unit.Province.Code),
            StringComparer.OrdinalIgnoreCase);

        // Detect destination conflicts: two units retreating to same province → both bounced
        var conflictedDestinations = request.RetreatOrders
            .GroupBy(r => MapGraph.BaseCode(r.Destination.Code),
                     StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() >= 2)
            .Select(g => g.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var orderResults = new List<OrderResult>();
        var survivedUnits = new List<Unit>();

        foreach (var dislodged in request.DislodgedUnits)
        {
            var unitBase = MapGraph.BaseCode(dislodged.Unit.Province.Code);

            if (!orderByBase.TryGetValue(unitBase, out var retreatOrder))
            {
                // No order submitted — auto-disband
                orderResults.Add(new OrderResult(
                    new DisbandOrder(dislodged.Unit), OrderOutcome.Failure,
                    "No retreat order submitted."));
                continue;
            }

            var destBase = MapGraph.BaseCode(retreatOrder.Destination.Code);

            // Check that destination is in valid retreat options
            bool validDestination = dislodged.RetreatOptions
                .Any(p => MapGraph.BaseCode(p.Code).Equals(
                              destBase, StringComparison.OrdinalIgnoreCase));

            if (!validDestination)
            {
                orderResults.Add(new OrderResult(retreatOrder, OrderOutcome.Void,
                    $"Province '{retreatOrder.Destination.Code}' is not a valid retreat option."));
                continue;
            }

            if (conflictedDestinations.Contains(destBase))
            {
                orderResults.Add(new OrderResult(retreatOrder, OrderOutcome.Bounced,
                    "Two units retreated to the same province — both disbanded."));
                continue;
            }

            // Success — unit retreats to new province
            orderResults.Add(new OrderResult(retreatOrder, OrderOutcome.Success));
            survivedUnits.Add(dislodged.Unit with { Province = retreatOrder.Destination });
        }

        return new RetreatAdjudicationResult(orderResults, survivedUnits);
    }
}
