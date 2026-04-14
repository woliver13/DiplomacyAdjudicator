using woliver13.DiplomacyAdjudicator.Core.Domain;
using woliver13.DiplomacyAdjudicator.Core.Map;
using Xunit;

namespace woliver13.DiplomacyAdjudicator.Tests.Map;

/// <summary>
/// Tests for the standard Diplomacy map adjacency graph.
/// Province codes are canonical lowercase (e.g. "lon", "nth", "spa_nc").
/// </summary>
public class MapGraphTests
{
    private readonly MapGraph _map = MapGraph.LoadStandard();

    // -------------------------------------------------------------------------
    // Province validity
    // -------------------------------------------------------------------------

    [Fact]
    public void IsValidProvince_KnownProvince_ReturnsTrue()
        => Assert.True(_map.IsValidProvince("lon"));

    [Fact]
    public void IsValidProvince_UnknownProvince_ReturnsFalse()
        => Assert.False(_map.IsValidProvince("xyz"));

    [Fact]
    public void IsValidProvince_BicoastalParent_ReturnsTrue()
        => Assert.True(_map.IsValidProvince("spa"));

    [Fact]
    public void IsValidProvince_BicoastalCoast_ReturnsTrue()
        => Assert.True(_map.IsValidProvince("spa_nc"));

    // -------------------------------------------------------------------------
    // Army adjacency
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("lon", "yor")] // London → Yorkshire
    [InlineData("lon", "wal")] // London → Wales
    [InlineData("par", "bur")] // Paris → Burgundy
    [InlineData("par", "pic")] // Paris → Picardy
    [InlineData("par", "bre")] // Paris → Brest
    [InlineData("par", "gas")] // Paris → Gascony
    public void IsAdjacent_Army_ValidMove_ReturnsTrue(string from, string to)
        => Assert.True(_map.IsAdjacent(new Province(from), new Province(to), UnitType.Army));

    [Theory]
    [InlineData("lon", "par")] // London → Paris (not adjacent)
    [InlineData("lon", "nth")] // Army cannot move to sea
    [InlineData("lon", "bel")] // London → Belgium (not adjacent by land)
    public void IsAdjacent_Army_InvalidMove_ReturnsFalse(string from, string to)
        => Assert.False(_map.IsAdjacent(new Province(from), new Province(to), UnitType.Army));

    [Theory]
    [InlineData("spa", "por")] // Army in Spain → Portugal
    [InlineData("spa", "gas")] // Army in Spain → Gascony
    [InlineData("spa", "mar")] // Army in Spain → Marseilles
    public void IsAdjacent_Army_BicoastalParent_ReturnsTrue(string from, string to)
        => Assert.True(_map.IsAdjacent(new Province(from), new Province(to), UnitType.Army));

    // -------------------------------------------------------------------------
    // Fleet adjacency
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("lon", "nth")] // London → North Sea
    [InlineData("lon", "eng")] // London → English Channel
    [InlineData("lon", "wal")] // London → Wales (coastal)
    [InlineData("nth", "eng")] // North Sea → English Channel
    [InlineData("nth", "hel")] // North Sea → Helgoland Bight
    [InlineData("nth", "nwy")] // North Sea → Norway
    public void IsAdjacent_Fleet_ValidMove_ReturnsTrue(string from, string to)
        => Assert.True(_map.IsAdjacent(new Province(from), new Province(to), UnitType.Fleet));

    [Theory]
    [InlineData("par", "bur")] // Paris is inland — no fleet movement
    [InlineData("nth", "iri")] // North Sea not adjacent to Irish Sea
    public void IsAdjacent_Fleet_InvalidMove_ReturnsFalse(string from, string to)
        => Assert.False(_map.IsAdjacent(new Province(from), new Province(to), UnitType.Fleet));

    // -------------------------------------------------------------------------
    // Bicoastal fleet adjacency
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("spa_nc", "mao")] // Spain NC → Mid-Atlantic
    [InlineData("spa_nc", "gas")] // Spain NC → Gascony
    [InlineData("spa_nc", "por")] // Spain NC → Portugal
    public void IsAdjacent_Fleet_SpainNorthCoast_ReturnsTrue(string from, string to)
        => Assert.True(_map.IsAdjacent(new Province(from), new Province(to), UnitType.Fleet));

    [Theory]
    [InlineData("spa_sc", "mao")] // Spain SC → Mid-Atlantic
    [InlineData("spa_sc", "wes")] // Spain SC → Western Med
    [InlineData("spa_sc", "lyo")] // Spain SC → Gulf of Lyon
    [InlineData("spa_sc", "mar")] // Spain SC → Marseilles
    [InlineData("spa_sc", "por")] // Spain SC → Portugal
    public void IsAdjacent_Fleet_SpainSouthCoast_ReturnsTrue(string from, string to)
        => Assert.True(_map.IsAdjacent(new Province(from), new Province(to), UnitType.Fleet));

    [Fact]
    public void IsAdjacent_Fleet_SpainNorthCoast_CannotReachSouthCoastNeighbors()
        => Assert.False(_map.IsAdjacent(new Province("spa_nc"), new Province("wes"), UnitType.Fleet));

    [Fact]
    public void IsAdjacent_Fleet_SpainSouthCoast_CannotReachNorthCoastNeighbors()
        => Assert.False(_map.IsAdjacent(new Province("spa_sc"), new Province("gas"), UnitType.Fleet));

    [Theory]
    [InlineData("bul_ec", "bla")] // Bulgaria EC → Black Sea
    [InlineData("bul_ec", "rum")] // Bulgaria EC → Rumania
    [InlineData("bul_ec", "con")] // Bulgaria EC → Constantinople
    [InlineData("bul_sc", "aeg")] // Bulgaria SC → Aegean Sea
    [InlineData("bul_sc", "gre")] // Bulgaria SC → Greece
    [InlineData("bul_sc", "con")] // Bulgaria SC → Constantinople
    public void IsAdjacent_Fleet_Bulgaria_Coasts_ReturnTrue(string from, string to)
        => Assert.True(_map.IsAdjacent(new Province(from), new Province(to), UnitType.Fleet));

    [Theory]
    [InlineData("stp_nc", "bar")] // St Petersburg NC → Barents Sea
    [InlineData("stp_nc", "nwy")] // St Petersburg NC → Norway
    [InlineData("stp_sc", "bot")] // St Petersburg SC → Gulf of Bothnia
    [InlineData("stp_sc", "fin")] // St Petersburg SC → Finland
    [InlineData("stp_sc", "lvn")] // St Petersburg SC → Livonia
    public void IsAdjacent_Fleet_StPetersburg_Coasts_ReturnTrue(string from, string to)
        => Assert.True(_map.IsAdjacent(new Province(from), new Province(to), UnitType.Fleet));

    // -------------------------------------------------------------------------
    // Supply centers
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("lon")] [InlineData("par")] [InlineData("ber")]
    [InlineData("mos")] [InlineData("rom")] [InlineData("ank")]
    [InlineData("con")] [InlineData("vie")] [InlineData("war")]
    [InlineData("sev")] [InlineData("smy")] [InlineData("tri")]
    [InlineData("mun")] [InlineData("mar")] [InlineData("kie")]
    [InlineData("bre")] [InlineData("edi")] [InlineData("bud")]
    [InlineData("ser")] [InlineData("bel")] [InlineData("bul")]
    [InlineData("den")] [InlineData("gre")] [InlineData("hol")]
    [InlineData("lvp")] [InlineData("nap")] [InlineData("nwy")]
    [InlineData("por")] [InlineData("rum")] [InlineData("spa")]
    [InlineData("swe")] [InlineData("tun")] [InlineData("ven")]
    public void IsSupplyCenter_SupplyCenterProvince_ReturnsTrue(string code)
        => Assert.True(_map.IsSupplyCenter(new Province(code)));

    [Theory]
    [InlineData("yor")] [InlineData("pic")] [InlineData("nth")]
    [InlineData("eng")] [InlineData("boh")] [InlineData("tyr")]
    public void IsSupplyCenter_NonSupplyCenterProvince_ReturnsFalse(string code)
        => Assert.False(_map.IsSupplyCenter(new Province(code)));

    // -------------------------------------------------------------------------
    // Home centers
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("lon", "england")] [InlineData("lvp", "england")] [InlineData("edi", "england")]
    [InlineData("par", "france")]  [InlineData("bre", "france")]  [InlineData("mar", "france")]
    [InlineData("ber", "germany")] [InlineData("kie", "germany")] [InlineData("mun", "germany")]
    [InlineData("rom", "italy")]   [InlineData("nap", "italy")]   [InlineData("ven", "italy")]
    [InlineData("mos", "russia")]  [InlineData("sev", "russia")]  [InlineData("war", "russia")] [InlineData("stp", "russia")]
    [InlineData("ank", "turkey")]  [InlineData("con", "turkey")]  [InlineData("smy", "turkey")]
    [InlineData("vie", "austria")] [InlineData("bud", "austria")] [InlineData("tri", "austria")]
    public void GetHomeCenter_HomeCenterProvince_ReturnsPower(string code, string power)
    {
        var result = _map.GetHomeCenter(new Province(code));
        Assert.NotNull(result);
        Assert.Equal(power, result!.Name);
    }

    [Theory]
    [InlineData("yor")] [InlineData("nth")] [InlineData("bur")]
    [InlineData("bel")] [InlineData("spa")] [InlineData("nwy")]
    public void GetHomeCenter_NonHomeProvince_ReturnsNull(string code)
        => Assert.Null(_map.GetHomeCenter(new Province(code)));

    // -------------------------------------------------------------------------
    // Province type
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("boh")] [InlineData("bur")] [InlineData("gal")]
    [InlineData("mos")] [InlineData("mun")] [InlineData("par")]
    [InlineData("ruh")] [InlineData("sil")] [InlineData("tyr")]
    [InlineData("ukr")] [InlineData("war")]
    public void IsInland_InlandProvince_ReturnsTrue(string code)
        => Assert.True(_map.IsInland(new Province(code)));

    [Theory]
    [InlineData("nth")] [InlineData("eng")] [InlineData("mao")]
    [InlineData("adr")] [InlineData("aeg")] [InlineData("bal")]
    public void IsSea_SeaProvince_ReturnsTrue(string code)
        => Assert.True(_map.IsSea(new Province(code)));
}
