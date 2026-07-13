using DSLDungeon.Game.Entities.Stats;

namespace DSLDungeon.Game.Entities.Items;

public static class WeaponPresets
{
    public static Weapon CreateRustyDagger()
    {
        var w = new Weapon("Ржавый кинжал", 3, 1, 1.0f);
        w.Quality = 0.8f;
        w.AddModifier(StatKeys.Dexterity, StatModifier.Added(1, ModifierSource.Equipment));
        return w;
    }

    public static Weapon CreateSwordOfJustice()
    {
        var w = new Weapon("Меч правосудия", 15, 1, 0.8f);
        w.Quality = 1.5f;
        w.AddModifier(StatKeys.Strength, StatModifier.Added(5, ModifierSource.Equipment));
        w.AddModifier(StatKeys.CritChance, StatModifier.Added(0.1f, ModifierSource.Equipment));
        w.AddModifier(StatKeys.DamageBase, StatModifier.More(1.2f, ModifierSource.Equipment, "Physical"));
        return w;
    }

    public static Weapon CreateFlamingStaff()
    {
        var w = new Weapon("Пылающий посох", 8, 2, 1.2f, "Fire", true);
        w.Quality = 1.3f;
        w.AddModifier(StatKeys.Intelligence, StatModifier.Added(8, ModifierSource.Equipment));
        w.AddModifier(StatKeys.DamageFirePct, StatModifier.Added(0.35f, ModifierSource.Equipment));
        w.AddModifier(StatKeys.CastSpeed, StatModifier.More(1.15f, ModifierSource.Equipment));
        return w;
    }

    public static Weapon CreateOrcChampionAxe()
    {
        var w = new Weapon("Топор чемпиона", 20, 1, 1.1f, "Physical");
        w.Quality = 1.4f;
        w.AddModifier(StatKeys.Strength, StatModifier.Added(8, ModifierSource.Equipment));
        w.AddModifier(StatKeys.DamageBase, StatModifier.More(1.3f, ModifierSource.Equipment, "Physical"));
        w.AddModifier(StatKeys.AttackSpeed, StatModifier.Less(0.9f, ModifierSource.Equipment));
        return w;
    }
}
