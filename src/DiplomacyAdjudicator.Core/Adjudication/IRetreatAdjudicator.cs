namespace woliver13.DiplomacyAdjudicator.Core.Adjudication;

public interface IRetreatAdjudicator
{
    RetreatAdjudicationResult Adjudicate(RetreatAdjudicationRequest request);
}
