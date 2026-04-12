using DiplomacyAdjudicator.Core.Domain;
using DiplomacyAdjudicator.Core.Map;
using DiplomacyAdjudicator.Core.Parsing;

namespace DiplomacyAdjudicator.Tests.Parsing;

public class IOrderParserTests
{
    private static readonly MapGraph Map = MapGraph.LoadStandard();

    [Fact]
    public void OrderParser_Implements_IOrderParser()
    {
        // IOrderParser must exist and OrderParser must be assignable to it
        IOrderParser parser = new OrderParser(Map);
        Assert.NotNull(parser);
    }

    [Fact]
    public void OrderParserFactory_Create_ReturnsIOrderParser()
    {
        IOrderParserFactory factory = new OrderParserFactory();
        IOrderParser parser = factory.Create(Map);
        Assert.NotNull(parser);
    }

    [Fact]
    public void IOrderParser_Parse_ReturnsOrder()
    {
        IOrderParser parser = new OrderParser(Map);
        var unit  = new Unit(UnitType.Army, new Power("austria"), new Province("vie"));
        var order = parser.Parse(unit, "hold");
        Assert.IsType<HoldOrder>(order);
    }
}
