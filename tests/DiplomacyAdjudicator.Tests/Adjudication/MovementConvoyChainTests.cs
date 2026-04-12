using DiplomacyAdjudicator.Core.Domain;
using Xunit;

namespace DiplomacyAdjudicator.Tests.Adjudication;

/// <summary>
/// Targeted tests for HasConvoyPath BFS across longer fleet chains.
/// These serve as regression guards for the convoy-graph caching refactoring (issue #15).
/// </summary>
public class MovementConvoyChainTests
{
    // 3-fleet sequential convoy chain: A LON → TUN via F ENG → F MAO → F WES.
    // LON is not adjacent to TUN; BFS must traverse ENG→MAO→WES to find the path.
    // No opposition — move succeeds.
    [Fact]
    public void ConvoyChain_ThreeFleetsInSequence_ArmySucceeds()
    {
        new AdjudicationScenario()
            .WithUnit("england", "army",  "lon")
            .WithUnit("england", "fleet", "eng")
            .WithUnit("england", "fleet", "mao")
            .WithUnit("england", "fleet", "wes")
            .WithOrder("england", "army",  "lon", "move tun")
            .WithOrder("england", "fleet", "eng", "convoy army lon move tun")
            .WithOrder("england", "fleet", "mao", "convoy army lon move tun")
            .WithOrder("england", "fleet", "wes", "convoy army lon move tun")
            .AssertOutcome("lon", OrderOutcome.Success)
            .Run();
    }

    // Same 3-fleet chain but middle fleet (MAO) is dislodged — path broken.
    // F IRI → MAO, supported by F NAO (both adjacent to MAO).
    // MAO is dislodged → no complete ENG→WES path without MAO → LON fails.
    [Fact]
    public void ConvoyChain_MiddleFleetDislodged_ArmyFails()
    {
        new AdjudicationScenario()
            .WithUnit("england", "army",  "lon")
            .WithUnit("england", "fleet", "eng")
            .WithUnit("england", "fleet", "mao")
            .WithUnit("england", "fleet", "wes")
            .WithUnit("france",  "fleet", "iri")
            .WithUnit("france",  "fleet", "nao")
            .WithOrder("england", "army",  "lon", "move tun")
            .WithOrder("england", "fleet", "eng", "convoy army lon move tun")
            .WithOrder("england", "fleet", "mao", "convoy army lon move tun")
            .WithOrder("england", "fleet", "wes", "convoy army lon move tun")
            .WithOrder("france",  "fleet", "iri", "move mao")
            .WithOrder("france",  "fleet", "nao", "support fleet iri move mao")
            .AssertOutcome("lon", OrderOutcome.Failure)
            .AssertDislodged("mao")
            .Run();
    }
}
