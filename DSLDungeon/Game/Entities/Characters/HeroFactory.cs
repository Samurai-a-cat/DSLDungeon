namespace DSLDungeon.Game.Entities.Characters;
using Core;
using Combat;
using Components;
using Stats;
using Grid;

public static class HeroFactory
{
    public static Actor CreateKnight(EntityId id, HexCoords position, WorldState world)
    {
        var hero = new Actor(id, "Рыцарь", position);
        
        var stats = hero.AddComponent(new StatsComponent());
        stats.SetupBaseStats(str: 15, dex: 10, @int: 8, con: 12);
        
        stats.Stats.AddModifier(StatKeys.DamageBase, StatModifier.Base(10, "Physical"));
        stats.Stats.AddModifier(StatKeys.Armor, StatModifier.Base(5));
        stats.Stats.AddModifier(StatKeys.BlockChance, StatModifier.Base(0.15f));
        
        var health = hero.AddComponent(new HealthComponent());
        health.Initialize(120);
        
        hero.AddComponent(new EquipmentComponent());
        hero.AddComponent(new CombatStateComponent());
        
        hero.AddComponent(new ImpulseComponent //TODO переделать на систему
        {
            BonusDamagePercent = 2f,
            DurationSeconds = 2.0f
        });
        
        return hero;
    }

    public static Actor CreateMage(EntityId id, HexCoords position, WorldState world)
    {
        var hero = new Actor(id, "Маг", position);
        
        var stats = hero.AddComponent(new StatsComponent());
        stats.SetupBaseStats(str: 6, dex: 10, @int: 18, con: 8);
        
        stats.Stats.AddModifier(StatKeys.DamageBase, StatModifier.Base(5, "Fire"));
        stats.Stats.AddModifier(StatKeys.CastSpeed, StatModifier.Base(1.3f));
        stats.Stats.AddModifier(StatKeys.ResistanceFire, StatModifier.Base(0.25f));
        
        var health = hero.AddComponent(new HealthComponent());
        health.Initialize(80);
        
        hero.AddComponent(new EquipmentComponent());
        
        hero.AddComponent(new BackgroundThreadComponent
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
        return null;
    }
}