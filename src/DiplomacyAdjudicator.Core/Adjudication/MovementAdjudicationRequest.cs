using DiplomacyAdjudicator.Core.Domain;
using DiplomacyAdjudicator.Core.Map;

namespace DiplomacyAdjudicator.Core.Adjudication;

public record MovementAdjudicationRequest(
    MapGraph Map,
    IReadOnlyList<Unit> Units,
    IReadOnlyList<Order> Orders,
    IReadOnlyDictionary<Power, IReadOnlyList<Province>> SupplyCenters
);
