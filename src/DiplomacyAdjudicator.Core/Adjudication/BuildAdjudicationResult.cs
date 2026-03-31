using DiplomacyAdjudicator.Core.Domain;

namespace DiplomacyAdjudicator.Core.Adjudication;

public record BuildAdjudicationResult(
    IReadOnlyList<OrderResult> OrderResults,
    IReadOnlyList<Unit> ResultingUnits
);
