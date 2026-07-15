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

        float waveMult = 1 + (waveLevel - 1) * 0.2f;
        orc.Stats.SetupBaseStats(
            str: 8 * waveMult,
            dex: 6 * waveMult,
            @int: 3 * waveMult,
            con: 7 * waveMult
        );

        orc.Stats.AddModifier(StatKey.DamageBase, StatModifier.Base(3 * waveMult, "Physical"));

        orc.Health.Initialize((int)(70 * waveMult));

        orc.AddComponent(new EquipmentComponent());
        orc.AddComponent(new SimpleAIComponent());

        return orc;
    }

    public static Actor CreateChampion(EntityId id, string name, HexCoords position, WorldState world)
    {
        var orc = new Actor(id, name, position);

        orc.Stats.SetupBaseStats(str: 20, dex: 8, @int: 4, con: 18);

        orc.Stats.AddModifier(StatKey.DamageBase, StatModifier.Base(15, "Physical"));
        orc.Stats.AddModifier(StatKey.Armor, StatModifier.Base(12));
        orc.Stats.AddModifier(StatKey.CritChance, StatModifier.Added(0.08f, ModifierSource.BaseStats));
        orc.Stats.AddModifier(StatKey.ResistancePhysical, StatModifier.Base(0.15f));

        orc.Health.Initialize(180);

        orc.AddComponent(new EquipmentComponent());
        orc.AddComponent(new SimpleAIComponent());

        orc.AddComponent(new BerserkRageData
        {
            HpThresholdPercent = 0.5f,
            DamageBonusPerMissingHp = 0.02f
        });

        return orc;
    }
}
