using woliver13.DiplomacyAdjudicator.Core.Domain;

namespace woliver13.DiplomacyAdjudicator.Core.Adjudication;

public record RetreatAdjudicationResult(
    IReadOnlyList<OrderResult> OrderResults,
    IReadOnlyList<Unit> SurvivedUnits
);
