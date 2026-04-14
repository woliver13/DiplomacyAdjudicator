using woliver13.DiplomacyAdjudicator.Core.Domain;
using woliver13.DiplomacyAdjudicator.Core.Map;

namespace woliver13.DiplomacyAdjudicator.Core.Adjudication;

public record MovementAdjudicationRequest(
    MapGraph Map,
    IReadOnlyList<Unit> Units,
    IReadOnlyList<Order> Orders,
    IReadOnlyDictionary<Power, IReadOnlyList<Province>> SupplyCenters
);
