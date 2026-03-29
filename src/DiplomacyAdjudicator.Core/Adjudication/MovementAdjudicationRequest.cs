using DiplomacyAdjudicator.Core.Domain;

namespace DiplomacyAdjudicator.Core.Adjudication;

public record MovementAdjudicationRequest(
    IReadOnlyList<Unit> Units,
    IReadOnlyList<Order> Orders,
    IReadOnlyDictionary<Power, IReadOnlyList<Province>> SupplyCenters
);
