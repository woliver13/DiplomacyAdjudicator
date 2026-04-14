using woliver13.DiplomacyAdjudicator.Core.Domain;

namespace woliver13.DiplomacyAdjudicator.Tests;

public class NamespaceTests
{
    [Fact]
    public void All_core_types_live_in_woliver13_namespace()
    {
        var coreAssembly = typeof(Province).Assembly;
        var badTypes = coreAssembly.GetTypes()
            .Where(t => t.IsPublic && !t.Namespace!.StartsWith("woliver13."))
            .Select(t => t.FullName)
            .ToList();
        Assert.Empty(badTypes);
    }
}
