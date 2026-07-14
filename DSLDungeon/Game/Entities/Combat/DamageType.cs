namespace DSLDungeon.Game.Entities.Combat;

using DSLDungeon.Game.Entities.Stats;

/// <summary>
/// Тип урона. byte для компактности в памяти.
/// </summary>
public enum DamageType : byte
{
    Physical = 0,
    Fire = 1,
    Cold = 2,
    Lightning = 3,
    Poison = 4,
    Chaos = 5,
}

public static class DamageTypeExtensions
{
    private static readonly string[] _names = new[]
    {
        "Physical", "Fire", "Cold", "Lightning", "Poison", "Chaos"
    };

    private static readonly Dictionary<string, DamageType> _parseMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["physical"] = DamageType.Physical,
        ["fire"] = DamageType.Fire,
        ["cold"] = DamageType.Cold,
        ["lightning"] = DamageType.Lightning,
        ["poison"] = DamageType.Poison,
        ["chaos"] = DamageType.Chaos,
    };

    public static string ToDisplayString(this DamageType type) => _names[(int)type];

    public static bool TryParse(string? value, out DamageType type)
    {
        if (value != null && _parseMap.TryGetValue(value, out type))
            return true;
        type = DamageType.Physical;
        return false;
    }

    public static StatKey GetResistanceStat(this DamageType type) => type switch
    {
        DamageType.Physical => StatKey.ResistancePhysical,
        DamageType.Fire => StatKey.ResistanceFire,
        DamageType.Cold => StatKey.ResistanceCold,
        DamageType.Lightning => StatKey.ResistanceLightning,
        _ => StatKey.ResistancePhysical
    };

    public static StatKey GetDamagePctStat(this DamageType type) => type switch
    {
        DamageType.Physical => StatKey.DamagePhysicalPct,
        DamageType.Fire => StatKey.DamageFirePct,
        DamageType.Cold => StatKey.DamageColdPct,
        DamageType.Lightning => StatKey.DamageLightningPct,
        _ => StatKey.DamagePhysicalPct
    };
}
