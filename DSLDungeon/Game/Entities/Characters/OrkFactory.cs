using DSLDungeon.Game.Core;
using DSLDungeon.Game.Entities.Components;
using DSLDungeon.Game.Entities.Stats;
using DSLDungeon.Game.Grid;

namespace DSLDungeon.Game.Entities.Characters;

public static class OrcFactory
{
    public static Actor CreateGrunt(EntityId id, string name, HexCoords position, WorldState world, int waveLevel = 1)
    {
        var orc = new Actor(id, name, position);

        var stats = orc.AddComponent(new StatsComponent());
        float waveMult = 1 + (waveLevel - 1) * 0.2f;
        stats.SetupBaseStats(
            str: 8 * waveMult,
            dex: 6 * waveMult,
            @int: 3 * waveMult,
            con: 7 * waveMult
        );

        stats.Stats.AddModifier(StatKeys.DamageBase, StatModifier.Base(3 * waveMult, "Physical"));

        var health = orc.AddComponent(new HealthComponent());
        health.Initialize((int)(70 * waveMult));

        orc.AddComponent(new EquipmentComponent());
        orc.AddComponent(new SimpleAIComponent());
        orc.AddComponent(new CombatStateComponent());
        orc.AddComponent(new AbilityCooldownComponent());
        orc.AddComponent(new PositionTrackerComponent());

        return orc;
    }

    public static Actor CreateChampion(EntityId id, string name, HexCoords position, WorldState world)
    {
        var orc = new Actor(id, name, position);

        var stats = orc.AddComponent(new StatsComponent());
        stats.SetupBaseStats(str: 20, dex: 8, @int: 4, con: 18);

        stats.Stats.AddModifier(StatKeys.DamageBase, StatModifier.Base(15, "Physical"));
        stats.Stats.AddModifier(StatKeys.Armor, StatModifier.Base(12));
        stats.Stats.AddModifier(StatKeys.CritChance, StatModifier.Added(0.08f, ModifierSource.BaseStats));
        stats.Stats.AddModifier(StatKeys.ResistancePhysical, StatModifier.Base(0.15f));

        var health = orc.AddComponent(new HealthComponent());
        health.Initialize(180);

        orc.AddComponent(new EquipmentComponent());
        orc.AddComponent(new SimpleAIComponent());
        orc.AddComponent(new CombatStateComponent());
        orc.AddComponent(new AbilityCooldownComponent());
        orc.AddComponent(new PositionTrackerComponent());

        orc.AddComponent(new BerserkRageData
        {
            HpThresholdPercent = 0.5f,
            DamageBonusPerMissingHp = 0.02f
        });

        return orc;
    }
}
