namespace woliver13.DiplomacyAdjudicator.Core.Domain;

public abstract record Order(Unit Unit);

// Movement phase orders
public record HoldOrder(Unit Unit) : Order(Unit);

public record MoveOrder(Unit Unit, Province Destination) : Order(Unit);

public record SupportHoldOrder(Unit Unit, Province SupportedProvince) : Order(Unit);

public record SupportMoveOrder(Unit Unit, Province SupportedOrigin, Province SupportedDestination) : Order(Unit);

public record ConvoyOrder(Unit Unit, Province ConvoyedOrigin, Province ConvoyedDestination) : Order(Unit);

// Retreat phase orders
public record RetreatOrder(Unit Unit, Province Destination) : Order(Unit);

public record DisbandOrder(Unit Unit) : Order(Unit);

// Build phase orders
public record BuildOrder(Unit Unit) : Order(Unit);

public record WaiveOrder(Power Power) : Order(new Unit(UnitType.Army, Power, new Province("none")));
