namespace DiplomacyAdjudicator.Core.Adjudication;

public interface IBuildAdjudicator
{
    BuildAdjudicationResult Adjudicate(BuildAdjudicationRequest request);
}
