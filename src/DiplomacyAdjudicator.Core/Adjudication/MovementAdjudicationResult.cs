using DiplomacyAdjudicator.Core.Domain;

namespace DiplomacyAdjudicator.Core.Adjudication;

public record DislodgedUnit(Unit Unit, IReadOnlyList<Province> RetreatOptions);

public record MovementAdjudicationResult(
    IReadOnlyList<OrderResult> OrderResults,
    IReadOnlyList<DislodgedUnit> DislodgedUnits,
    PhaseType NextPhase
);
