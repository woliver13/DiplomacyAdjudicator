using woliver13.DiplomacyAdjudicator.Core.Domain;

namespace woliver13.DiplomacyAdjudicator.Tests.Domain;

public class ProvinceCodeTests
{
    [Theory]
    [InlineData("SPA_NC", "spa")]   // uppercase with coast suffix
    [InlineData("bul_ec", "bul")]   // lowercase with coast suffix
    [InlineData("LON",    "lon")]   // uppercase plain
    [InlineData("Vie",    "vie")]   // mixed-case plain
    [InlineData("stp_sc", "stp")]   // lowercase with south-coast suffix
    public void Normalise_LowercasesAndStripsCoastSuffix(string raw, string expected)
    {
        Assert.Equal(expected, ProvinceCode.Normalise(raw));
    }
}
