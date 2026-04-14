using woliver13.DiplomacyAdjudicator.Core.Domain;
using woliver13.DiplomacyAdjudicator.Core.Map;

namespace woliver13.DiplomacyAdjudicator.Core.Adjudication;

/// <summary>
/// Input to the build/disband phase adjudicator.
/// SupplyCenters reflects current ownership (post-retreat).
/// Orders may include BuildOrder, DisbandOrder, and WaiveOrder.
/// </summary>
public record BuildAdjudicationRequest(
    MapGraph Map,
    IReadOnlyList<Unit> Units,
    IReadOnlyDictionary<Power, IReadOnlyList<Province>> SupplyCenters,
    IReadOnlyList<Order> BuildOrders
);
