namespace woliver13.DiplomacyAdjudicator.Core.Domain;

/// <summary>
/// A province on the Diplomacy map, identified by its canonical lowercase string code.
/// Bicoastal provinces use suffixes: spa_nc, spa_sc, bul_ec, bul_sc, stp_nc, stp_sc.
/// </summary>
public record Province(string Code)
{
    public override string ToString() => Code;
}
