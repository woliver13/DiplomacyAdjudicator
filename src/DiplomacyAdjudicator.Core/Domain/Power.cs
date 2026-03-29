namespace DiplomacyAdjudicator.Core.Domain;

public record Power(string Name)
{
    public static readonly Power Austria = new("austria");
    public static readonly Power England = new("england");
    public static readonly Power France = new("france");
    public static readonly Power Germany = new("germany");
    public static readonly Power Italy = new("italy");
    public static readonly Power Russia = new("russia");
    public static readonly Power Turkey = new("turkey");
}
