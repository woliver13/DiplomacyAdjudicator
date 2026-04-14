using woliver13.DiplomacyAdjudicator.Core.Domain;

namespace woliver13.DiplomacyAdjudicator.Core.Adjudication;

/// <summary>
/// Input to the retreat phase adjudicator.
/// DislodgedUnits carry pre-computed RetreatOptions and AttackedFrom from
/// the preceding MovementAdjudicationResult.
/// </summary>
public record RetreatAdjudicationRequest(
    IReadOnlyList<DislodgedUnit> DislodgedUnits,
    IReadOnlyList<RetreatOrder> RetreatOrders
);
