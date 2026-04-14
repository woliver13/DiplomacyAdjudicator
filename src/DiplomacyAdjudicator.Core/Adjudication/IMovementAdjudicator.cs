namespace woliver13.DiplomacyAdjudicator.Core.Adjudication;

public interface IMovementAdjudicator
{
    MovementAdjudicationResult Adjudicate(MovementAdjudicationRequest request);
}
