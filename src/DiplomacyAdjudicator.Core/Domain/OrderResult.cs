namespace woliver13.DiplomacyAdjudicator.Core.Domain;

public enum OrderOutcome
{
    Success,
    Failure,
    Bounced,
    Cut,
    Void,
    Dislodged,
    NoOrder
}

public record OrderResult(Order Order, OrderOutcome Outcome, string? Reason = null);
