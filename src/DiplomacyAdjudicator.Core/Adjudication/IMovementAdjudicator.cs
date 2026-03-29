namespace DiplomacyAdjudicator.Core.Adjudication;

public interface IMovementAdjudicator
{
    MovementAdjudicationResult Adjudicate(MovementAdjudicationRequest request);
}
