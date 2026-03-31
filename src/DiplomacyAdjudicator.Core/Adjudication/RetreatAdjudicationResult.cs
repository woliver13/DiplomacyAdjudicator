using DiplomacyAdjudicator.Core.Domain;

namespace DiplomacyAdjudicator.Core.Adjudication;

public record RetreatAdjudicationResult(
    IReadOnlyList<OrderResult> OrderResults,
    IReadOnlyList<Unit> SurvivedUnits
);
