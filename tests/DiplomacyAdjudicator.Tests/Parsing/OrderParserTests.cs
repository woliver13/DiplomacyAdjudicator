using woliver13.DiplomacyAdjudicator.Core.Domain;
using woliver13.DiplomacyAdjudicator.Core.Map;
using woliver13.DiplomacyAdjudicator.Core.Parsing;

namespace woliver13.DiplomacyAdjudicator.Tests.Parsing;

public class OrderParserTests
{
    private static readonly MapGraph Map = MapGraph.LoadStandard();
    private static readonly Unit ArmyVie = new(UnitType.Army, new Power("austria"), new Province("vie"));

    private Order Parse(string orderText) => new OrderParser(Map).Parse(ArmyVie, orderText);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Parse_EmptyString_ReturnsHoldOrder(string orderText)
    {
        Assert.IsType<HoldOrder>(Parse(orderText));
    }

    [Theory]
    [InlineData("conquer vie")]
    [InlineData("destroy lon")]
    [InlineData("zzz")]
    public void Parse_UnknownKeyword_ReturnsHoldOrder(string orderText)
    {
        Assert.IsType<HoldOrder>(Parse(orderText));
    }

    [Theory]
    [InlineData("support")]
    [InlineData("support army")]
    [InlineData("s fleet")]
    public void Parse_SupportWithOnlyUnitType_ReturnsHoldOrder(string orderText)
    {
        Assert.IsType<HoldOrder>(Parse(orderText));
    }

    [Theory]
    [InlineData("convoy")]
    [InlineData("convoy army")]
    [InlineData("c fleet lon")]
    public void Parse_ConvoyWithMissingDestination_ReturnsHoldOrder(string orderText)
    {
        Assert.IsType<HoldOrder>(Parse(orderText));
    }

    [Theory]
    [InlineData("move")]
    [InlineData("-")]
    [InlineData("m")]
    public void Parse_MoveWithNoDestination_ReturnsHoldOrder(string orderText)
    {
        Assert.IsType<HoldOrder>(Parse(orderText));
    }
}
