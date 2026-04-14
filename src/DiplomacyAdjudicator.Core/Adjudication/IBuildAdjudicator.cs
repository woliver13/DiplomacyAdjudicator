namespace woliver13.DiplomacyAdjudicator.Core.Adjudication;

public interface IBuildAdjudicator
{
    BuildAdjudicationResult Adjudicate(BuildAdjudicationRequest request);
}
