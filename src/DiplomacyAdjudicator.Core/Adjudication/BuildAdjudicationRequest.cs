using DiplomacyAdjudicator.Core.Domain;

namespace DiplomacyAdjudicator.Core.Adjudication;

/// <summary>
/// Input to the build/disband phase adjudicator.
/// SupplyCenters reflects current ownership (post-retreat).
/// Orders may include BuildOrder, DisbandOrder, and WaiveOrder.
/// </summary>
public record BuildAdjudicationRequest(
    IReadOnlyList<Unit> Units,
    IReadOnlyDictionary<Power, IReadOnlyList<Province>> SupplyCenters,
    IReadOnlyList<Order> BuildOrders
);
