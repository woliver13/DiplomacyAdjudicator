using woliver13.DiplomacyAdjudicator.Core.Domain;

namespace woliver13.DiplomacyAdjudicator.Core.Adjudication;

public record BuildAdjudicationResult(
    IReadOnlyList<OrderResult> OrderResults,
    IReadOnlyList<Unit> ResultingUnits
);
