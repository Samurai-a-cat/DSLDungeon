namespace DSLDungeon.Game.Entities.Characters;
using Core;
using Components;
using Stats;
using Grid;

public static class HeroFactory
{
    public static Actor CreateKnight(EntityId id, HexCoords position, WorldState world)
    {
        var hero = new Actor(id, "Рыцарь", position);

        hero.Stats.SetupBaseStats(str: 15, dex: 10, @int: 8, con: 12);
        hero.Stats.AddModifier(StatKeys.DamageBase, StatModifier.Base(10, "Physical"));
        hero.Stats.AddModifier(StatKeys.Armor, StatModifier.Base(5));
        hero.Stats.AddModifier(StatKeys.BlockChance, StatModifier.Base(0.15f));

        hero.Health.Initialize(120);

        hero.AddComponent(new EquipmentComponent());

        return hero;
    }

    public static Actor CreateMage(EntityId id, HexCoords position, WorldState world)
    {
        var hero = new Actor(id, "Маг", position);

        hero.Stats.SetupBaseStats(str: 6, dex: 10, @int: 18, con: 8);
        hero.Stats.AddModifier(StatKeys.DamageBase, StatModifier.Base(5, "Fire"));
        hero.Stats.AddModifier(StatKeys.CastSpeed, StatModifier.Base(1.3f));
        hero.Stats.AddModifier(StatKeys.ResistanceFire, StatModifier.Base(0.25f));

        hero.Health.Initialize(80);

        hero.AddComponent(new EquipmentComponent());

        hero.AddComponent(new BackgroundThreadData
        {
            CheckInterval = 3.0f,
            Condition = (actor, w) => FindLowAlly(actor, w) != null,
            OnTrigger = (actor, w) => {
                var ally = FindLowAlly(actor, w);
                if (ally != null)
                {
                    // TODO: Реализовать CastSpellEvent когда будет система заклинаний
                }
            }
        });

        return hero;
    }

    private static Actor? FindLowAlly(Actor actor, WorldState world)
    {
        _ = actor;
        _ = world;
        return null;
    }
}
