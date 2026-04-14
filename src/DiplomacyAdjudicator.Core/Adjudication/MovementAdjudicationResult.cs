using woliver13.DiplomacyAdjudicator.Core.Domain;

namespace woliver13.DiplomacyAdjudicator.Core.Adjudication;

public record DislodgedUnit(Unit Unit, Province AttackedFrom, IReadOnlyList<Province> RetreatOptions);

public record MovementAdjudicationResult(
    IReadOnlyList<OrderResult> OrderResults,
    IReadOnlyList<DislodgedUnit> DislodgedUnits,
    PhaseType NextPhase
);
